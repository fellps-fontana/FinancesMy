import "dotenv/config";

function required(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(
      `Variavel de ambiente ${name} nao definida. Copie env.example para .env e preencha os valores.`
    );
  }
  return value;
}

export const config = {
  apiUrl: (process.env.MYFINANCES_API_URL ?? "http://localhost:5146").replace(/\/+$/, ""),
  get username() {
    return required("MYFINANCES_USERNAME");
  },
  get password() {
    return required("MYFINANCES_PASSWORD");
  },
};
