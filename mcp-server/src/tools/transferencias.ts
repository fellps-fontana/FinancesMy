import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda, formatarData } from "../format.js";

export function registerTransferenciasTools(server: McpServer) {
  server.tool(
    "criar_transferencia",
    "Cria uma transferencia entre duas contas manuais (ambas precisam estar ativas). Nao serve para pagar fatura de cartao (use pagar_fatura) nem para registrar emprestimo a pessoa (use registrar_emprestimo).",
    {
      contaOrigemId: z.string().uuid(),
      contaDestinoId: z.string().uuid(),
      valor: z.number().positive(),
      data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
      descricao: z.string().optional(),
    },
    async ({ contaOrigemId, contaDestinoId, valor, data, descricao }) => {
      try {
        const transferencia = await api.post<{ id: string; valor: number; data: string }>("api/transferencias", {
          contaOrigemId,
          contaDestinoId,
          valor,
          data,
          descricao,
        });
        return ok(`Transferencia de ${formatarMoeda(transferencia.valor)} registrada em ${formatarData(transferencia.data)}.`);
      } catch (e) {
        return err(e);
      }
    }
  );
}
