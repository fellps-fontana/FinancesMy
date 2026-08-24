import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda, formatarData, diasEntre } from "../format.js";
import type { ContaResponse } from "./contas.js";
import type { LancamentoResponseDto } from "./lancamentos.js";
import type { FaturaResponse } from "./cartao.js";
import type { ContaReceberResponse } from "./contasReceber.js";

interface ItemPendente {
  descricao: string;
  valor: number;
  vencimento: string;
  diasParaVencer: number;
  origem: string;
}

function formatarItem(item: ItemPendente): string {
  const status =
    item.diasParaVencer < 0
      ? `ATRASADO ha ${Math.abs(item.diasParaVencer)} dia(s)`
      : item.diasParaVencer === 0
        ? "VENCE HOJE"
        : `vence em ${item.diasParaVencer} dia(s)`;
  return `[${status}] ${formatarData(item.vencimento)} | ${formatarMoeda(item.valor)} | ${item.descricao} (${item.origem})`;
}

export function registerContasAPagarTools(server: McpServer) {
  server.tool(
    "contas_a_pagar",
    "Resumo do que precisa ser pago: lancamentos pendentes em contas banco (inclui as ocorrencias geradas por contas fixas), faturas de cartao em aberto/fechadas ainda nao quitadas, e opcionalmente contas a receber previstas. Ordena por urgencia (vencidos primeiro, depois por data). Use para perguntas como 'tenho algo pra pagar essa semana?'.",
    {
      dias: z.number().int().min(0).max(365).optional().describe("Janela de dias a frente para considerar 'proximo do vencimento'. Padrao 7. Itens vencidos entram sempre, independente da janela."),
      incluirContasReceber: z.boolean().optional().describe("Se true, inclui tambem contas a receber (recebiveis/emprestimos) previstas para o mesmo periodo. Padrao false."),
    },
    async ({ dias, incluirContasReceber }) => {
      const janela = dias ?? 7;
      const incluirReceber = incluirContasReceber ?? false;

      try {
        const [contasBanco, contasCartao] = await Promise.all([
          api.get<ContaResponse[]>("api/Contas", { tipo: "banco" }),
          api.get<ContaResponse[]>("api/Contas", { tipo: "cartao" }),
        ]);

        const contasBancoAtivas = contasBanco.filter((c) => c.ativa);
        const contasCartaoAtivas = contasCartao.filter((c) => c.ativa);

        const lancamentosPorConta = await Promise.all(
          contasBancoAtivas.map((c) => api.get<LancamentoResponseDto[]>(`api/contas/${c.id}/lancamentos/fluxo-caixa`))
        );
        const faturasPorConta = await Promise.all(
          contasCartaoAtivas.map((c) => api.get<FaturaResponse[]>(`api/contas/${c.id}/faturas`))
        );

        const itens: ItemPendente[] = [];

        for (let i = 0; i < contasBancoAtivas.length; i++) {
          const conta = contasBancoAtivas[i];
          for (const l of lancamentosPorConta[i]) {
            if (l.tipo?.toUpperCase() !== "DEBIT" || l.status?.toUpperCase() !== "PENDENTE") continue;
            const dias0 = diasEntre(l.data);
            if (dias0 > janela) continue;
            itens.push({
              descricao: l.descricao ?? "(sem descricao)",
              valor: l.valor,
              vencimento: l.data,
              diasParaVencer: dias0,
              origem: `lancamento em ${conta.nome}`,
            });
          }
        }

        for (let i = 0; i < contasCartaoAtivas.length; i++) {
          const conta = contasCartaoAtivas[i];
          for (const f of faturasPorConta[i]) {
            if (f.status?.toUpperCase() === "PAGA" || f.valorPendente <= 0) continue;
            const dias0 = diasEntre(f.dataVencimento);
            if (dias0 > janela) continue;
            itens.push({
              descricao: `Fatura ${conta.nome}`,
              valor: f.valorPendente,
              vencimento: f.dataVencimento,
              diasParaVencer: dias0,
              origem: `fatura ${conta.nome}`,
            });
          }
        }

        if (incluirReceber) {
          const [pendentes, parciais] = await Promise.all([
            api.get<ContaReceberResponse[]>("api/contas-receber", { status: "PENDENTE" }),
            api.get<ContaReceberResponse[]>("api/contas-receber", { status: "PARCIAL" }),
          ]);
          for (const c of [...pendentes, ...parciais]) {
            if (!c.dataPrevista) continue;
            const dias0 = diasEntre(c.dataPrevista);
            if (dias0 > janela) continue;
            itens.push({
              descricao: `A receber: ${c.descricao}`,
              valor: c.saldoPendente,
              vencimento: c.dataPrevista,
              diasParaVencer: dias0,
              origem: c.pessoa ? `de ${c.pessoa}` : "conta a receber",
            });
          }
        }

        if (itens.length === 0) {
          return ok(`Nada pendente de pagamento${incluirReceber ? " ou recebimento" : ""} nos proximos ${janela} dia(s) (nem atrasado).`);
        }

        itens.sort((a, b) => a.diasParaVencer - b.diasParaVencer || b.valor - a.valor);

        const totalAPagar = itens.filter((i) => !i.descricao.startsWith("A receber:")).reduce((soma, i) => soma + i.valor, 0);
        const cabecalho = `Total a pagar (vencido + proximos ${janela} dias): ${formatarMoeda(totalAPagar)}\n`;

        return ok(cabecalho + itens.map(formatarItem).join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );
}
