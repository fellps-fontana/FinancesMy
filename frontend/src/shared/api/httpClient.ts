import axios from 'axios';

/**
 * Instancia base do cliente HTTP, apontando para a API .NET.
 * Toda chamada de rede do app passa por aqui (nunca fetch solto no componente).
 * URL configuravel via VITE_API_BASE_URL (.env), com fallback para o
 * ambiente de desenvolvimento local do backend.
 */
export const httpClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7224/api',
  headers: {
    'Content-Type': 'application/json',
  },
});
