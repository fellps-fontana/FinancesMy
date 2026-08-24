import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda, formatarData, formatarEnum } from "../format.js";

export interface CompraResponse {
  id: string;
  contaId: string;
  categoriaId?: string | null;
  descricao?: string | null;
  valor: number;
  tipo: string;
  data: string;
  status: string;
  faturaId?: string | null;
  compraParceladaId?: string | null;
  parcelaNumero?: number | null;
}

export interface FaturaResponse {
  id: string;
  contaId: string;
  dataFechamento: string;
  dataVencimento: string;
  status: string;
  valorTotal: number;
  valorPago: number;
  valorPendente: number;
}

function formatarCompra(c: CompraResponse): string {
  const parcela = c.parcelaNumero ? ` (parcela ${c.parcelaNumero})` : "";
  return `${c.descricao ?? "(sem descricao)"}${parcela} [${c.id}] | ${formatarMoeda(c.valor)} | ${formatarData(c.data)} | ${formatarEnum(c.status)}`;
}

export function formatarFatura(f: FaturaResponse): string {
  return `Fatura [${f.id}] | conta ${f.contaId} | fecha ${formatarData(f.dataFechamento)} vence ${formatarData(f.dataVencimento)} | ${formatarEnum(f.status)} | total ${formatarMoeda(f.valorTotal)} | pago ${formatarMoeda(f.valorPago)} | pendente ${formatarMoeda(f.valorPendente)}`;
}

export function registerCartaoTools(server: McpServer) {
  server.tool(
    "criar_compra_cartao",
    "Registra uma compra a vista no cartao de credito. Entra por competencia na fatura em aberto, nao aparece no fluxo de caixa da conta.",
    {
      contaId: z.string().uuid().describe("Id da conta do tipo cartao"),
      descricao: z.string().min(1),
      valor: z.number().positive(),
      categoriaId: z.string().uuid().optional(),
      data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
    },
    async ({ contaId, descricao, valor, categoriaId, data }) => {
      try {
        const compra = await api.post<CompraResponse>(`api/contas/${contaId}/compras`, { descricao, valor, categoriaId, data });
        return ok(`Compra registrada:\n${formatarCompra(compra)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "editar_compra_cartao",
    "Edita uma compra de cartao a vista existente.",
    {
      contaId: z.string().uuid(),
      compraId: z.string().uuid(),
      descricao: z.string().min(1),
      valor: z.number().positive(),
      categoriaId: z.string().uuid().optional(),
      data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
    },
    async ({ contaId, compraId, descricao, valor, categoriaId, data }) => {
      try {
        const compra = await api.put<CompraResponse>(`api/contas/${contaId}/compras/${compraId}`, {
          descricao,
          valor,
          categoriaId,
          data,
        });
        return ok(`Compra atualizada:\n${formatarCompra(compra)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "criar_compra_parcelada",
    "Registra uma compra parcelada no cartao de credito, gerando uma parcela por fatura futura.",
    {
      contaId: z.string().uuid(),
      descricao: z.string().min(1),
      valorTotal: z.number().positive(),
      quantidadeParcelas: z.number().int().min(1),
      categoriaId: z.string().uuid().optional(),
      dataCompra: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
    },
    async ({ contaId, descricao, valorTotal, quantidadeParcelas, categoriaId, dataCompra }) => {
      try {
        const compraParcelada = await api.post<{
          id: string;
          descricao: string;
          valorTotal: number;
          quantidadeParcelas: number;
          parcelas: CompraResponse[];
        }>(`api/contas/${contaId}/compras-parceladas`, {
          descricao,
          valorTotal,
          quantidadeParcelas,
          categoriaId,
          dataCompra,
        });
        const parcelas = compraParcelada.parcelas.map(formatarCompra).join("\n  ");
        return ok(
          `Compra parcelada criada: ${compraParcelada.descricao} [${compraParcelada.id}] em ${compraParcelada.quantidadeParcelas}x de ${formatarMoeda(compraParcelada.valorTotal / compraParcelada.quantidadeParcelas)}\n  ${parcelas}`
        );
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "estornar_compra_parcelada",
    "Estorna uma compra parcelada. Parcelas em faturas ainda nao pagas sao canceladas; parcelas em faturas ja pagas recebem um lancamento de estorno retroativo.",
    {
      contaId: z.string().uuid(),
      compraParceladaId: z.string().uuid(),
      motivo: z.string().min(1),
      data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
    },
    async ({ contaId, compraParceladaId, motivo, data }) => {
      try {
        const resultado = await api.post<{ parcelasCanceladas: CompraResponse[]; estornosRetroativos: unknown[] }>(
          `api/contas/${contaId}/compras-parceladas/${compraParceladaId}/estornos`,
          { motivo, data }
        );
        return ok(
          `Estorno realizado. Parcelas canceladas: ${resultado.parcelasCanceladas.length} | Estornos retroativos (faturas ja pagas): ${resultado.estornosRetroativos.length}`
        );
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "listar_faturas",
    "Lista as faturas de uma conta de cartao de credito (abertas, fechadas e pagas).",
    { contaId: z.string().uuid() },
    async ({ contaId }) => {
      try {
        const faturas = await api.get<FaturaResponse[]>(`api/contas/${contaId}/faturas`);
        if (faturas.length === 0) return ok("Nenhuma fatura encontrada para essa conta.");
        return ok(faturas.map(formatarFatura).join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "obter_fatura",
    "Consulta uma fatura especifica de cartao de credito.",
    { contaId: z.string().uuid(), faturaId: z.string().uuid() },
    async ({ contaId, faturaId }) => {
      try {
        const fatura = await api.get<FaturaResponse>(`api/contas/${contaId}/faturas/${faturaId}`);
        return ok(formatarFatura(fatura));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "pagar_fatura",
    "Registra um pagamento (total ou parcial, inclusive antecipado) de uma fatura de cartao, debitando a conta de origem informada. Nao ha 'marcar como paga' - a fatura vira paga automaticamente quando os pagamentos somam o valor total.",
    {
      contaId: z.string().uuid().describe("Id da conta do tipo cartao"),
      faturaId: z.string().uuid(),
      valor: z.number().positive(),
      data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
      contaOrigemId: z.string().uuid().describe("Conta banco/investimento de onde sai o dinheiro"),
    },
    async ({ contaId, faturaId, valor, data, contaOrigemId }) => {
      try {
        const pagamento = await api.post<{ id: string; valor: number; data: string }>(
          `api/contas/${contaId}/faturas/${faturaId}/pagamentos`,
          { valor, data, contaOrigemId }
        );
        return ok(`Pagamento de ${formatarMoeda(pagamento.valor)} registrado em ${formatarData(pagamento.data)}.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "criar_estorno_fatura",
    "Estorna uma compra a vista de cartao (nao parcelada) ja lancada em fatura.",
    {
      contaId: z.string().uuid(),
      compraId: z.string().uuid(),
      motivo: z.string().min(1),
      data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
    },
    async ({ contaId, compraId, motivo, data }) => {
      try {
        const estorno = await api.post<{ id: string }>(`api/contas/${contaId}/faturas/estornos`, {
          compraId,
          motivo,
          data,
        });
        return ok(`Estorno registrado [${estorno.id}].`);
      } catch (e) {
        return err(e);
      }
    }
  );
}
