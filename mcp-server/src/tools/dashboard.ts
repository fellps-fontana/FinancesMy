import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda } from "../format.js";

export interface ProjecaoMesResponse {
  ano: number;
  mes: number;
  totalRecebidoNoMes: number;
  totalAReceberEsperadoNoMes: number;
  totalPagoNoMes: number;
  totalAPagarNoMes: number;
  saldoProjetado: number;
}

export function registerDashboardTools(server: McpServer) {
  server.tool(
    "projecao_mes",
    "Retorna a projecao financeira do mes: total ja recebido/pago, total esperado a receber/pagar (incluindo fatura de cartao fracionada pelo vencimento) e o saldo projetado do mes.",
    { ano: z.number().int(), mes: z.number().int().min(1).max(12) },
    async ({ ano, mes }) => {
      try {
        const p = await api.get<ProjecaoMesResponse>("api/dashboard/projecao-mes", { ano, mes });
        return ok(
          [
            `Projecao ${p.mes}/${p.ano}`,
            `  Recebido: ${formatarMoeda(p.totalRecebidoNoMes)} | A receber: ${formatarMoeda(p.totalAReceberEsperadoNoMes)}`,
            `  Pago: ${formatarMoeda(p.totalPagoNoMes)} | A pagar: ${formatarMoeda(p.totalAPagarNoMes)}`,
            `  Saldo projetado: ${formatarMoeda(p.saldoProjetado)}`,
          ].join("\n")
        );
      } catch (e) {
        return err(e);
      }
    }
  );
}
