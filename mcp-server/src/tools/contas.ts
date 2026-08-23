import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda, formatarEnum } from "../format.js";
import { paraStorageValue } from "../enums.js";

export interface ContaResponse {
  id: string;
  nome: string;
  tipo: string;
  subtipo?: string | null;
  icone?: string | null;
  cor?: string | null;
  origem: string;
  saldo: number;
  saldoManual?: number | null;
  ativa: boolean;
  diaFechamento?: number | null;
  diaVencimento?: number | null;
  pierreAccountId?: string | null;
}

export function formatarConta(c: ContaResponse): string {
  const linhas = [
    `${c.nome} [${c.id}]`,
    `  Tipo: ${formatarEnum(c.tipo)}${c.subtipo ? ` / ${formatarEnum(c.subtipo)}` : ""} | Origem: ${formatarEnum(c.origem)}`,
    `  Saldo: ${formatarMoeda(c.saldo)} | Ativa: ${c.ativa ? "sim" : "nao"}`,
  ];
  if (c.diaFechamento || c.diaVencimento) {
    linhas.push(`  Fechamento dia ${c.diaFechamento ?? "-"} | Vencimento dia ${c.diaVencimento ?? "-"}`);
  }
  return linhas.join("\n");
}

export function registerContasTools(server: McpServer) {
  server.tool(
    "listar_contas",
    "Lista contas cadastradas. Sem o parametro tipo, a API retorna apenas contas de investimento (comportamento padrao dela) - passe tipo explicitamente para ver contas banco ou cartao.",
    {
      tipo: z.enum(["banco", "cartao", "investimento"]).optional().describe("Filtra por tipo de conta"),
    },
    async ({ tipo }) => {
      try {
        const contas = await api.get<ContaResponse[]>("api/Contas", { tipo });
        if (contas.length === 0) return ok("Nenhuma conta encontrada.");
        return ok(contas.map(formatarConta).join("\n\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "criar_conta",
    "Cria uma nova conta (banco, cartao de credito ou investimento).",
    {
      nome: z.string().min(1),
      tipo: z.enum(["banco", "cartao", "investimento"]),
      subtipo: z
        .enum(["corrente", "poupanca", "dinheiro_fisico"])
        .optional()
        .describe("So tem efeito quando tipo=banco; para os demais tipos e ignorada pela API"),
      icone: z.string().optional(),
      cor: z.string().optional(),
      saldoManual: z.number().optional().describe("Saldo inicial manual (pode ser negativo). Nao se aplica a contas do tipo cartao."),
      diaFechamento: z.number().int().min(1).max(31).optional().describe("Obrigatorio para tipo=cartao"),
      diaVencimento: z.number().int().min(1).max(31).optional().describe("Obrigatorio para tipo=cartao"),
    },
    async ({ nome, tipo, subtipo, icone, cor, saldoManual, diaFechamento, diaVencimento }) => {
      try {
        const conta = await api.post<ContaResponse>("api/Contas", {
          nome,
          tipo: paraStorageValue(tipo),
          subtipo: subtipo ? paraStorageValue(subtipo) : undefined,
          icone,
          cor,
          saldoManual,
          diaFechamento,
          diaVencimento,
        });
        return ok(`Conta criada:\n${formatarConta(conta)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "saldo_conta",
    "Consulta o saldo atual de uma conta pelo id (para cartao de credito, ja considera faturas em aberto).",
    { contaId: z.string().uuid() },
    async ({ contaId }) => {
      try {
        const resp = await api.get<{ contaId: string; saldo: number }>(`api/Contas/${contaId}/saldo`);
        return ok(`Saldo: ${formatarMoeda(resp.saldo)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "atualizar_saldo_conta",
    "Atualiza o saldo manual de uma conta (nao funciona para contas do tipo cartao, cujo saldo e calculado pelas faturas).",
    { contaId: z.string().uuid(), novoSaldo: z.number() },
    async ({ contaId, novoSaldo }) => {
      try {
        await api.patch(`api/Contas/${contaId}/saldo`, { novoSaldo });
        return ok(`Saldo da conta ${contaId} atualizado para ${formatarMoeda(novoSaldo)}.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "desativar_conta",
    "Desativa uma conta (nao exclui, apenas marca como inativa).",
    { contaId: z.string().uuid() },
    async ({ contaId }) => {
      try {
        await api.patch(`api/Contas/${contaId}/desativar`);
        return ok(`Conta ${contaId} desativada.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "total_investido",
    "Retorna o total investido somando todas as contas do tipo investimento.",
    {},
    async () => {
      try {
        const resp = await api.get<{ totalInvestido: number }>("api/Contas/investimentos/total");
        return ok(`Total investido: ${formatarMoeda(resp.totalInvestido)}`);
      } catch (e) {
        return err(e);
      }
    }
  );
}
