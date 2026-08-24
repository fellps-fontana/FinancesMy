import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda, formatarEnum } from "../format.js";
import { paraStorageValue } from "../enums.js";

export interface ContaFixaResponse {
  id: string;
  contaId: string;
  categoriaId?: string | null;
  descricao: string;
  valor: number;
  diaVencimento: number;
  periodicidade: string;
  ativa: boolean;
}

export function formatarContaFixa(c: ContaFixaResponse): string {
  return `${c.descricao} [${c.id}] | ${formatarMoeda(c.valor)} | vence dia ${c.diaVencimento} (${formatarEnum(c.periodicidade)}) | conta ${c.contaId} | ${c.ativa ? "ativa" : "inativa"}`;
}

export function registerContasFixasTools(server: McpServer) {
  server.tool(
    "listar_contas_fixas",
    "Lista contas fixas (moldes recorrentes, ex: aluguel, assinatura). Note que a conta fixa em si nao tem status pago/pendente - isso vive nos lancamentos que ela gera; use listar_lancamentos ou contas_a_pagar para ver as ocorrencias pendentes.",
    { ativa: z.boolean().optional() },
    async ({ ativa }) => {
      try {
        const contasFixas = await api.get<ContaFixaResponse[]>("api/contas-fixas", { ativa });
        if (contasFixas.length === 0) return ok("Nenhuma conta fixa encontrada.");
        return ok(contasFixas.map(formatarContaFixa).join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "criar_conta_fixa",
    "Cria uma conta fixa recorrente (ex: aluguel, assinatura). Ao criar, a API ja gera automaticamente os lancamentos pendentes da ocorrencia atual e da proxima.",
    {
      contaId: z.string().uuid(),
      descricao: z.string().min(1),
      valor: z.number(),
      diaVencimento: z.number().int().min(1).max(31),
      categoriaId: z.string().uuid().optional(),
      periodicidade: z.enum(["mensal", "anual"]).optional().describe("Padrao mensal"),
    },
    async ({ contaId, descricao, valor, diaVencimento, categoriaId, periodicidade }) => {
      try {
        const contaFixa = await api.post<ContaFixaResponse>("api/contas-fixas", {
          contaId,
          descricao,
          valor,
          diaVencimento,
          categoriaId,
          periodicidade: periodicidade ? paraStorageValue(periodicidade) : undefined,
        });
        return ok(`Conta fixa criada:\n${formatarContaFixa(contaFixa)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "editar_conta_fixa",
    "Edita valor/dia de vencimento/categoria/periodicidade de uma conta fixa. So propaga para lancamentos ainda pendentes (os ja pagos ficam com o valor historico); mudanca de periodicidade nao afeta lancamentos ja gerados.",
    {
      contaFixaId: z.string().uuid(),
      valor: z.number(),
      diaVencimento: z.number().int().min(1).max(31),
      categoriaId: z.string().uuid().optional(),
      periodicidade: z.enum(["mensal", "anual"]).optional(),
    },
    async ({ contaFixaId, valor, diaVencimento, categoriaId, periodicidade }) => {
      try {
        const contaFixa = await api.put<ContaFixaResponse>(`api/contas-fixas/${contaFixaId}`, {
          valor,
          diaVencimento,
          categoriaId,
          periodicidade: periodicidade ? paraStorageValue(periodicidade) : undefined,
        });
        return ok(`Conta fixa atualizada:\n${formatarContaFixa(contaFixa)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "desativar_conta_fixa",
    "Desativa uma conta fixa e remove os lancamentos ainda pendentes gerados por ela (os ja pagos permanecem no historico).",
    { contaFixaId: z.string().uuid() },
    async ({ contaFixaId }) => {
      try {
        await api.post(`api/contas-fixas/${contaFixaId}/desativar`);
        return ok(`Conta fixa ${contaFixaId} desativada.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "reativar_conta_fixa",
    "Reativa uma conta fixa, regerando os lancamentos pendentes da ocorrencia atual e da proxima.",
    { contaFixaId: z.string().uuid() },
    async ({ contaFixaId }) => {
      try {
        await api.post(`api/contas-fixas/${contaFixaId}/reativar`);
        return ok(`Conta fixa ${contaFixaId} reativada.`);
      } catch (e) {
        return err(e);
      }
    }
  );
}
