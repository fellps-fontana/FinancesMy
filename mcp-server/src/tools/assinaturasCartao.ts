import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda } from "../format.js";

export interface AssinaturaCartaoResponse {
  id: string;
  contaId: string;
  categoriaId?: string | null;
  descricao: string;
  valor: number;
  diaReferencia: number;
  ativa: boolean;
}

export function formatarAssinaturaCartao(a: AssinaturaCartaoResponse): string {
  return `${a.descricao} [${a.id}] | ${formatarMoeda(a.valor)} | cobra dia ${a.diaReferencia} | conta ${a.contaId} | ${a.ativa ? "ativa" : "inativa"}`;
}

export function registerAssinaturasCartaoTools(server: McpServer) {
  server.tool(
    "criar_assinatura_cartao",
    "Cadastra uma assinatura recorrente no cartao de credito (ex: Netflix, Spotify). Ao criar, gera automaticamente a compra na fatura do ciclo corrente.",
    {
      contaId: z.string().uuid().describe("Id da conta do tipo cartao"),
      descricao: z.string().min(1),
      valor: z.number().positive(),
      diaReferencia: z.number().int().min(1).max(31).describe("Dia de cobranca no mes (1-31)"),
      categoriaId: z.string().uuid().optional(),
    },
    async ({ contaId, descricao, valor, diaReferencia, categoriaId }) => {
      try {
        const assinatura = await api.post<AssinaturaCartaoResponse>("api/assinaturas-cartao", {
          contaId,
          descricao,
          valor,
          diaReferencia,
          categoriaId,
        });
        return ok(`Assinatura de cartao criada:\n${formatarAssinaturaCartao(assinatura)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "listar_assinaturas_cartao",
    "Lista assinaturas recorrentes de cartao de credito, permitindo filtrar por conta e/ou status ativa.",
    {
      contaId: z.string().uuid().optional(),
      ativa: z.boolean().optional(),
    },
    async ({ contaId, ativa }) => {
      try {
        const assinaturas = await api.get<AssinaturaCartaoResponse[]>("api/assinaturas-cartao", {
          contaId,
          ativa,
        });
        if (assinaturas.length === 0) return ok("Nenhuma assinatura de cartao encontrada.");
        return ok(assinaturas.map(formatarAssinaturaCartao).join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "editar_assinatura_cartao",
    "Edita valor, descricao, dia de referencia e/ou categoria de uma assinatura de cartao. Propaga para a compra da fatura corrente se a fatura estiver aberta.",
    {
      id: z.string().uuid().describe("Id da assinatura"),
      descricao: z.string().min(1),
      valor: z.number().positive(),
      diaReferencia: z.number().int().min(1).max(31),
      categoriaId: z.string().uuid().optional(),
    },
    async ({ id, descricao, valor, diaReferencia, categoriaId }) => {
      try {
        const assinatura = await api.put<AssinaturaCartaoResponse>(`api/assinaturas-cartao/${id}`, {
          descricao,
          valor,
          diaReferencia,
          categoriaId,
        });
        return ok(`Assinatura de cartao atualizada:\n${formatarAssinaturaCartao(assinatura)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "desativar_assinatura_cartao",
    "Desativa uma assinatura de cartao. Novas compras deixam de ser geradas; compras historicas ja geradas permanecem intocadas.",
    {
      id: z.string().uuid().describe("Id da assinatura"),
    },
    async ({ id }) => {
      try {
        await api.post(`api/assinaturas-cartao/${id}/desativar`);
        return ok(`Assinatura de cartao ${id} desativada.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "reativar_assinatura_cartao",
    "Reativa uma assinatura de cartao previamente desativada, voltando a gerar compras a partir do ciclo corrente.",
    {
      id: z.string().uuid().describe("Id da assinatura"),
    },
    async ({ id }) => {
      try {
        await api.post(`api/assinaturas-cartao/${id}/reativar`);
        return ok(`Assinatura de cartao ${id} reativada.`);
      } catch (e) {
        return err(e);
      }
    }
  );
}
