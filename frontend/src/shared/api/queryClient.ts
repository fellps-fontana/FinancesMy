import { QueryClient } from '@tanstack/react-query';

/**
 * Instancia unica do React Query — camada de estado de servidor do app.
 * Cada feature define seus proprios hooks (useQuery/useMutation) consumindo
 * o httpClient; nenhum fetch deve viver dentro de um componente.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60 * 1000,
      retry: 1,
    },
  },
});
