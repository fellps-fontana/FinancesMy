import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda, formatarData, formatarEnum } from "../format.js";
import { paraStorageValue } from "../enums.js";

export interface ContaReceberResponse {
  id: string;
  tipo: string;
  descricao: string;
  pessoa?: string | null;
  valorTotal: number;
  saldoPendente: number;
  status: string;
  dataRegistro: string;
  dataPrevista?: string | null;
}

export function formatarContaReceber(c: ContaReceberResponse): string {
  const pessoa = c.pessoa ? ` | de/para: ${c.pessoa}` : "";
  const prevista = c.dataPrevista ? ` | previsto para ${formatarData(c.dataPrevista)}` : "";
  return `${c.descricao} [${c.id}] (${formatarEnum(c.tipo)}) | saldo pendente ${formatarMoeda(c.saldoPendente)} de ${formatarMoeda(c.valorTotal)} | ${formatarEnum(c.status)}${pessoa}${prevista}`;
}

export function registerContasReceberTools(server: McpServer) {
  server.tool(
    "listar_contas_receber",
    "Lista contas a receber (recebiveis avulsos ou dinheiro emprestado a alguem). Nao ha endpoint para 'marcar como recebido': o status vira RECEBIDO automaticamente quando os recebimentos somam o valor total (use registrar_recebimento).",
    { status: z.enum(["pendente", "parcial", "recebido"]).optional() },
    async ({ status }) => {
      try {
        const contas = await api.get<ContaReceberResponse[]>("api/contas-receber", {
          status: status ? paraStorageValue(status) : undefined,
        });
        if (contas.length === 0) return ok("Nenhuma conta a receber encontrada.");
        return ok(contas.map(formatarContaReceber).join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "obter_conta_receber",
    "Consulta uma conta a receber especifica pelo id, incluindo o saldo pendente atual.",
    { contaReceberId: z.string().uuid() },
    async ({ contaReceberId }) => {
      try {
        const conta = await api.get<ContaReceberResponse>(`api/contas-receber/${contaReceberId}`);
        return ok(formatarContaReceber(conta));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "registrar_recebivel",
    "Registra uma conta a receber avulsa (ex: reembolso esperado, venda a prazo).",
    {
      descricao: z.string().min(1),
      valorTotal: z.number().positive(),
      dataRegistro: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
      dataPrevista: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional(),
      categoriaId: z.string().uuid().optional(),
    },
    async ({ descricao, valorTotal, dataRegistro, dataPrevista, categoriaId }) => {
      try {
        const conta = await api.post<ContaReceberResponse>("api/contas-receber/recebiveis", {
          descricao,
          valorTotal,
          dataRegistro,
          dataPrevista,
          categoriaId,
        });
        return ok(`Recebivel registrado:\n${formatarContaReceber(conta)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "registrar_emprestimo",
    "Registra dinheiro emprestado a uma pessoa, debitando a conta de origem.",
    {
      descricao: z.string().min(1),
      pessoa: z.string().min(1),
      valorTotal: z.number().positive(),
      contaOrigemId: z.string().uuid(),
      dataRegistro: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
      dataPrevista: z.string().regex(/^\d{4}-\d{2}-\d{2}$/).optional(),
      categoriaId: z.string().uuid().optional(),
    },
    async ({ descricao, pessoa, valorTotal, contaOrigemId, dataRegistro, dataPrevista, categoriaId }) => {
      try {
        const conta = await api.post<ContaReceberResponse>("api/contas-receber/emprestimos", {
          descricao,
          pessoa,
          valorTotal,
          contaOrigemId,
          dataRegistro,
          dataPrevista,
          categoriaId,
        });
        return ok(`Emprestimo registrado:\n${formatarContaReceber(conta)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "registrar_recebimento",
    "Registra o recebimento (total ou parcial) de uma conta a receber, creditando a conta de destino. Quando o acumulado atinge o valor total, o status muda para recebido automaticamente. Nao aceita valor maior que o saldo pendente.",
    {
      contaReceberId: z.string().uuid(),
      valor: z.number().positive(),
      data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
      contaDestinoId: z.string().uuid(),
      categoriaId: z.string().uuid().optional(),
    },
    async ({ contaReceberId, valor, data, contaDestinoId, categoriaId }) => {
      try {
        const recebimento = await api.post<{ id: string; valor: number; data: string }>(
          `api/contas-receber/${contaReceberId}/recebimentos`,
          { valor, data, contaDestinoId, categoriaId }
        );
        return ok(`Recebimento de ${formatarMoeda(recebimento.valor)} registrado em ${formatarData(recebimento.data)}.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "total_esperado_mes",
    "Soma o saldo pendente de todas as contas a receber previstas para o mes/ano informado (inclui parciais de qualquer mes, ja que contam ate serem totalmente recebidas).",
    { ano: z.number().int(), mes: z.number().int().min(1).max(12) },
    async ({ ano, mes }) => {
      try {
        const resp = await api.get<{ totalAReceberEsperadoNoMes: number }>("api/contas-receber/total-esperado-mes", {
          ano,
          mes,
        });
        return ok(`Total esperado a receber em ${mes}/${ano}: ${formatarMoeda(resp.totalAReceberEsperadoNoMes)}`);
      } catch (e) {
        return err(e);
      }
    }
  );
}
