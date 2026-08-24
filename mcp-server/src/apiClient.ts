import { config } from "./config.js";

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = "ApiError";
  }
}

interface LoginResponseRaw {
  token?: string;
  Token?: string;
}

async function parseBody(res: Response): Promise<any> {
  const text = await res.text();
  if (!text) return undefined;
  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}

function buildUrl(path: string, query?: Record<string, unknown>): string {
  const cleanPath = path.replace(/^\/+/, "");
  const url = new URL(`${config.apiUrl}/${cleanPath}`);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value === undefined || value === null || value === "") continue;
      url.searchParams.set(key, String(value));
    }
  }
  return url.toString();
}

function extractErrorMessage(status: number, body: any): string {
  if (body && typeof body.erro === "string") return body.erro;
  if (body && typeof body.title === "string") return body.title;
  return `Erro HTTP ${status} sem detalhe retornado pela API`;
}

class ApiClient {
  private token: string | null = null;
  private loginPromise: Promise<void> | null = null;

  private async login(): Promise<void> {
    const res = await fetch(buildUrl("api/Auth/login"), {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        usernameOrEmail: config.username,
        senha: config.password,
      }),
    });
    const body = await parseBody(res);
    if (!res.ok) {
      throw new ApiError(res.status, `Falha ao autenticar na API do MyFinances: ${extractErrorMessage(res.status, body)}`);
    }
    const token = (body as LoginResponseRaw)?.token ?? (body as LoginResponseRaw)?.Token;
    if (!token) {
      throw new ApiError(res.status, "Login na API retornou 200 mas sem token no corpo da resposta.");
    }
    this.token = token;
  }

  private async ensureToken(): Promise<void> {
    if (this.token) return;
    if (!this.loginPromise) {
      this.loginPromise = this.login().finally(() => {
        this.loginPromise = null;
      });
    }
    await this.loginPromise;
  }

  private async request<T>(
    method: string,
    path: string,
    options: { body?: unknown; query?: Record<string, unknown> } = {}
  ): Promise<T> {
    await this.ensureToken();

    const doFetch = () =>
      fetch(buildUrl(path, options.query), {
        method,
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${this.token}`,
        },
        body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
      });

    let res = await doFetch();

    if (res.status === 401) {
      this.token = null;
      await this.ensureToken();
      res = await doFetch();
    }

    const body = await parseBody(res);

    if (!res.ok) {
      throw new ApiError(res.status, extractErrorMessage(res.status, body));
    }

    return body as T;
  }

  get<T>(path: string, query?: Record<string, unknown>): Promise<T> {
    return this.request<T>("GET", path, { query });
  }

  post<T>(path: string, body?: unknown, query?: Record<string, unknown>): Promise<T> {
    return this.request<T>("POST", path, { body, query });
  }

  put<T>(path: string, body?: unknown): Promise<T> {
    return this.request<T>("PUT", path, { body });
  }

  patch<T>(path: string, body?: unknown): Promise<T> {
    return this.request<T>("PATCH", path, { body });
  }

  delete<T>(path: string): Promise<T> {
    return this.request<T>("DELETE", path);
  }
}

export const api = new ApiClient();
