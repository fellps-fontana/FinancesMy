import type { ReactElement } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { ContaCartaoPage } from './features/cartao/ContaCartaoPage';
import { PlaceholderPage } from './shared/components/PlaceholderPage';

/**
 * Rotas de topo do app. Cada uma comeca como placeholder; a feature dona do
 * modulo substitui pelo componente real quando chegar sua task
 * (Cartao: proximas tasks deste modulo, ver features/cartao).
 */
export function App(): ReactElement {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/inicio" replace />} />
      <Route path="/inicio" element={<PlaceholderPage title="Inicio" />} />
      <Route path="/lancamentos" element={<PlaceholderPage title="Lancamentos" />} />
      <Route path="/cartao" element={<ContaCartaoPage />} />
      <Route path="/contas" element={<PlaceholderPage title="Contas" />} />
    </Routes>
  );
}

export default App;
