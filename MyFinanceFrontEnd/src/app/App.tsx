import { QueryClient, QueryClientProvider } from "@tanstack/react-query"
import { BrowserRouter } from "react-router-dom"
import { AuthProvider } from "@/features/auth/AuthContext"
import { AppRoutes } from "@/app/routes"

// refetchInterval padrao de 5 minutos: o backend sincroniza com o Pierre via
// polling agendado (ver stack.md item 11 - sugestao de 6h para o sync em si),
// 5min e so o intervalo de refresh da UI para refletir o que ja foi
// sincronizado. Valor ajustavel, ainda nao confirmado com o usuario.
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchInterval: 5 * 60 * 1000,
    },
  },
})

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </AuthProvider>
    </QueryClientProvider>
  )
}

export default App
