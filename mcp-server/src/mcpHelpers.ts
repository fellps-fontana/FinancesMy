import { ApiError } from "./apiClient.js";

export function ok(text: string) {
  return { content: [{ type: "text" as const, text }] };
}

export function err(e: unknown) {
  const message =
    e instanceof ApiError
      ? `Erro da API (HTTP ${e.status}): ${e.message}`
      : e instanceof Error
        ? e.message
        : String(e);
  return { content: [{ type: "text" as const, text: message }], isError: true };
}
