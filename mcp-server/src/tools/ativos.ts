import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda, formatarData, formatarEnum } from "../format.js";
import { paraPascalCase } from "../enums.js";

export interface AtivoResponse {
  id: string;
  nome: string;
  tipo: string;
  instituicao: string;
  quantidade: number;
  valorInvestido: number;
  valorAtual: number;
  precoMedio: number;
  evolucaoPercentual: number;
  dataCompra: string;
  ativa: boolean;
}

function formatarAtivo(a: AtivoResponse): string {
  return `${a.nome} [${a.id}] (${formatarEnum(a.tipo)}) | ${a.instituicao} | investido ${formatarMoeda(a.valorInvestido)} -> atual ${formatarMoeda(a.valorAtual)} (${a.evolucaoPercentual >= 0 ? "+" : ""}${a.evolucaoPercentual.toFixed(2)}%)`;
}

export function registerAtivosTools(server: McpServer) {
  server.tool(
    "listar_ativos",
    "Lista os ativos de investimento ativos (renda fixa/variavel), com valor investido, valor atual e evolucao percentual.",
    {},
    async () => {
      try {
        const ativos = await api.get<AtivoResponse[]>("api/ativos");
        if (ativos.length === 0) return ok("Nenhum ativo encontrado.");
        return ok(ativos.map(formatarAtivo).join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "criar_ativo",
    "Cria um novo ativo de investimento, registrando o primeiro aporte (quantidade x preco unitario).",
    {
      nome: z.string().min(1),
      tipo: z.enum(["renda_fixa", "renda_variavel"]),
      instituicao: z.string().min(1),
      quantidade: z.number().positive(),
      precoUnitario: z.number().positive(),
      dataCompra: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
    },
    async ({ nome, tipo, instituicao, quantidade, precoUnitario, dataCompra }) => {
      try {
        const ativo = await api.post<AtivoResponse>("api/ativos", {
          nome,
          tipo: paraPascalCase(tipo),
          instituicao,
          quantidade,
          precoUnitario,
          dataCompra,
        });
        return ok(`Ativo criado:\n${formatarAtivo(ativo)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "atualizar_valor_ativo",
    "Atualiza manualmente o valor de mercado atual de um ativo (nao ha integracao automatica de cotacao). A diferenca gera um rendimento do tipo valorizacao automaticamente.",
    { ativoId: z.string().uuid(), novoValorAtual: z.number().nonnegative() },
    async ({ ativoId, novoValorAtual }) => {
      try {
        await api.patch(`api/ativos/${ativoId}/valor-atual`, { novoValorAtual });
        return ok(`Valor atual do ativo ${ativoId} atualizado para ${formatarMoeda(novoValorAtual)}.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "desativar_ativo",
    "Desativa (soft-delete) um ativo de investimento.",
    { ativoId: z.string().uuid() },
    async ({ ativoId }) => {
      try {
        await api.patch(`api/ativos/${ativoId}/desativar`);
        return ok(`Ativo ${ativoId} desativado.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "resumo_ativos",
    "Resumo da carteira de investimentos: total investido, total atual e distribuicao percentual por tipo.",
    {},
    async () => {
      try {
        const resumo = await api.get<{
          totalInvestido: number;
          totalAtual: number;
          porTipo: { tipo: string; valorAtual: number; percentualDaCarteira: number }[];
        }>("api/ativos/resumo");
        const linhas = [
          `Total investido: ${formatarMoeda(resumo.totalInvestido)}`,
          `Total atual: ${formatarMoeda(resumo.totalAtual)}`,
          ...resumo.porTipo.map((t) => `  ${formatarEnum(t.tipo)}: ${formatarMoeda(t.valorAtual)} (${t.percentualDaCarteira.toFixed(1)}%)`),
        ];
        return ok(linhas.join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "registrar_dividendo",
    "Registra o recebimento de um dividendo/rendimento manual de um ativo. Rendimentos sao informativos e nao entram no saldo das contas nem na projecao do mes.",
    { ativoId: z.string().uuid(), valor: z.number().positive(), data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/) },
    async ({ ativoId, valor, data }) => {
      try {
        await api.post(`api/ativos/${ativoId}/rendimentos`, { valor, data });
        return ok(`Dividendo de ${formatarMoeda(valor)} registrado em ${formatarData(data)}.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "historico_rendimentos",
    "Lista o historico de rendimentos (dividendos manuais e valorizacoes automaticas) de um ativo.",
    { ativoId: z.string().uuid() },
    async ({ ativoId }) => {
      try {
        const rendimentos = await api.get<{ id: string; tipo: string; origem: string; valor: number; data: string }[]>(
          `api/ativos/${ativoId}/rendimentos`
        );
        if (rendimentos.length === 0) return ok("Nenhum rendimento registrado para esse ativo.");
        return ok(
          rendimentos
            .map((r) => `${formatarData(r.data)} | ${formatarEnum(r.tipo)} (${formatarEnum(r.origem)}) | ${formatarMoeda(r.valor)}`)
            .join("\n")
        );
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "resumo_rendimentos",
    "Resumo geral de rendimentos da carteira: total em dividendos, total em valorizacao e o historico completo.",
    {},
    async () => {
      try {
        const resumo = await api.get<{
          totalDividendos: number;
          totalValorizacao: number;
          historico: { id: string; tipo: string; origem: string; valor: number; data: string }[];
        }>("api/ativos/rendimentos-resumo");
        const linhas = [
          `Total em dividendos: ${formatarMoeda(resumo.totalDividendos)}`,
          `Total em valorizacao: ${formatarMoeda(resumo.totalValorizacao)}`,
          `Registros no historico: ${resumo.historico.length}`,
        ];
        return ok(linhas.join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "registrar_aporte",
    "Registra um novo aporte (compra adicional) em um ativo existente, atualizando quantidade e preco medio.",
    {
      ativoId: z.string().uuid(),
      quantidade: z.number().positive(),
      precoUnitario: z.number().positive(),
      data: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
    },
    async ({ ativoId, quantidade, precoUnitario, data }) => {
      try {
        const aporte = await api.post<{ id: string; valorTotal: number; data: string }>(`api/ativos/${ativoId}/aportes`, {
          quantidade,
          precoUnitario,
          data,
        });
        return ok(`Aporte de ${formatarMoeda(aporte.valorTotal)} registrado em ${formatarData(aporte.data)}.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "listar_aportes",
    "Lista o historico de aportes (compras) de um ativo.",
    { ativoId: z.string().uuid() },
    async ({ ativoId }) => {
      try {
        const aportes = await api.get<{ id: string; data: string; quantidade: number; precoUnitario: number; valorTotal: number }[]>(
          `api/ativos/${ativoId}/aportes`
        );
        if (aportes.length === 0) return ok("Nenhum aporte registrado para esse ativo.");
        return ok(
          aportes
            .map((a) => `${formatarData(a.data)} | ${a.quantidade} x ${formatarMoeda(a.precoUnitario)} = ${formatarMoeda(a.valorTotal)}`)
            .join("\n")
        );
      } catch (e) {
        return err(e);
      }
    }
  );
}
