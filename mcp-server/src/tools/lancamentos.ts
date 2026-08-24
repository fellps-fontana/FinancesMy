import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda, formatarData, formatarEnum } from "../format.js";
import { paraStorageValue } from "../enums.js";
import type { ContaResponse } from "./contas.js";

export interface LancamentoResponseDto {
  id: string;
  contaId: string;
  categoriaId?: string | null;
  descricao?: string | null;
  valor: number;
  tipo: string;
  classificacao: string;
  data: string;
  status: string;
  manual: boolean;
  oculto: boolean;
}

export function formatarLancamento(l: LancamentoResponseDto): string {
  return `${formatarData(l.data)} | ${formatarMoeda(l.valor)} | ${formatarEnum(l.classificacao)} (${formatarEnum(l.status)}) | ${l.descricao ?? "(sem descricao)"} | conta ${l.contaId} | id ${l.id}`;
}

async function idsDeContas(contaId: string | undefined): Promise<string[]> {
  if (contaId) return [contaId];
  const tipos = ["banco", "cartao", "investimento"] as const;
  const listas = await Promise.all(tipos.map((tipo) => api.get<ContaResponse[]>("api/Contas", { tipo })));
  return listas.flat().map((c) => c.id);
}

export function registerLancamentosTools(server: McpServer) {
  server.tool(
    "listar_lancamentos",
    "Lista lancamentos (fluxo de caixa) com filtros opcionais por conta, categoria, periodo e status. Sem contaId, busca em todas as contas cadastradas. Compras de cartao de credito nao aparecem aqui (elas entram por competencia na fatura, use listar_faturas).",
    {
      contaId: z.string().uuid().optional(),
      categoriaId: z.string().uuid().optional(),
      dataInicio: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional().describe("Formato yyyy-MM-dd"),
      dataFim: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional().describe("Formato yyyy-MM-dd"),
      status: z.enum(["pendente", "sugerido", "pago"]).optional(),
      tipo: z.enum(["debit", "credit"]).optional(),
    },
    async ({ contaId, categoriaId, dataInicio, dataFim, status, tipo }) => {
      try {
        const contaIds = await idsDeContas(contaId);
        const listas = await Promise.all(
          contaIds.map((id) => api.get<LancamentoResponseDto[]>(`api/contas/${id}/lancamentos/fluxo-caixa`))
        );
        let lancamentos = listas.flat();
        if (categoriaId) lancamentos = lancamentos.filter((l) => l.categoriaId === categoriaId);
        if (status) lancamentos = lancamentos.filter((l) => l.status?.toUpperCase() === paraStorageValue(status));
        if (tipo) lancamentos = lancamentos.filter((l) => l.tipo?.toUpperCase() === paraStorageValue(tipo));
        if (dataInicio) lancamentos = lancamentos.filter((l) => l.data >= dataInicio);
        if (dataFim) lancamentos = lancamentos.filter((l) => l.data <= dataFim);
        lancamentos.sort((a, b) => a.data.localeCompare(b.data));
        if (lancamentos.length === 0) return ok("Nenhum lancamento encontrado para os filtros informados.");
        return ok(lancamentos.map(formatarLancamento).join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "criar_lancamento",
    "Cria um lancamento manual em uma conta.",
    {
      contaId: z.string().uuid(),
      descricao: z.string().min(1),
      valor: z.number(),
      categoriaId: z.string().uuid().optional(),
      tipo: z.enum(["debit", "credit"]).describe("debit = saida de dinheiro, credit = entrada"),
      data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).describe("Formato yyyy-MM-dd"),
      status: z.enum(["pendente", "pago"]),
    },
    async ({ contaId, descricao, valor, categoriaId, tipo, data, status }) => {
      try {
        const lancamento = await api.post<LancamentoResponseDto>(`api/contas/${contaId}/lancamentos`, {
          descricao,
          valor,
          categoriaId,
          tipo: paraStorageValue(tipo),
          data,
          status: paraStorageValue(status),
        });
        return ok(`Lancamento criado:\n${formatarLancamento(lancamento)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "editar_lancamento",
    "Edita um lancamento existente (substitui os campos informados).",
    {
      contaId: z.string().uuid(),
      lancamentoId: z.string().uuid(),
      descricao: z.string().min(1),
      valor: z.number(),
      categoriaId: z.string().uuid().optional(),
      tipo: z.enum(["debit", "credit"]),
      data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
      status: z.enum(["pendente", "pago"]).optional(),
    },
    async ({ contaId, lancamentoId, descricao, valor, categoriaId, tipo, data, status }) => {
      try {
        const lancamento = await api.put<LancamentoResponseDto>(`api/contas/${contaId}/lancamentos/${lancamentoId}`, {
          descricao,
          valor,
          categoriaId,
          tipo: paraStorageValue(tipo),
          data,
          status: status ? paraStorageValue(status) : undefined,
        });
        return ok(`Lancamento atualizado:\n${formatarLancamento(lancamento)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "marcar_lancamento_pago",
    "Marca um lancamento pendente como pago (usado tambem para pagar a ocorrencia gerada por uma conta fixa - obtenha o id do lancamento via listar_lancamentos).",
    { contaId: z.string().uuid(), lancamentoId: z.string().uuid() },
    async ({ contaId, lancamentoId }) => {
      try {
        await api.post(`api/contas/${contaId}/lancamentos/${lancamentoId}/pagamentos`);
        return ok(`Lancamento ${lancamentoId} marcado como pago.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "remover_lancamento",
    "Remove (exclui definitivamente) um lancamento. Falha se o lancamento pertencer a uma transferencia, fatura ou estiver conciliado.",
    { contaId: z.string().uuid(), lancamentoId: z.string().uuid() },
    async ({ contaId, lancamentoId }) => {
      try {
        await api.delete(`api/contas/${contaId}/lancamentos/${lancamentoId}`);
        return ok(`Lancamento ${lancamentoId} removido.`);
      } catch (e) {
        return err(e);
      }
    }
  );
}
