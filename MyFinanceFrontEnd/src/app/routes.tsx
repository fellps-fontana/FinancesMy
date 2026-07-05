import { Navigate, Route, Routes } from "react-router-dom"
import { LoginPage } from "@/features/auth/LoginPage"
import { Home } from "@/app/Home"
import { ProtectedRoute } from "@/app/ProtectedRoute"

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <Home />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
