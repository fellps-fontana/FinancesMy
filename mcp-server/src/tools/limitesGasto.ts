import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarMoeda } from "../format.js";

export interface LimiteGastoResponse {
  id: string;
  categoriaId: string;
  categoriaNome: string;
  valorLimite: number;
  periodo: string;
}

export interface GastoVsLimiteResponse {
  categoriaId: string;
  categoriaNome: string;
  valorLimite: number;
  gastoRealizado: number;
  percentualUtilizado: number;
  estourado: boolean;
}

function formatarGastoVsLimite(g: GastoVsLimiteResponse): string {
  const alerta = g.estourado ? " [ESTOURADO]" : "";
  return `${g.categoriaNome}: ${formatarMoeda(g.gastoRealizado)} de ${formatarMoeda(g.valorLimite)} (${g.percentualUtilizado.toFixed(0)}%)${alerta}`;
}

export function registerLimitesGastoTools(server: McpServer) {
  server.tool(
    "listar_limites_gasto",
    "Lista os limites de gasto mensais definidos por categoria.",
    {},
    async () => {
      try {
        const limites = await api.get<LimiteGastoResponse[]>("api/limites-gasto");
        if (limites.length === 0) return ok("Nenhum limite de gasto definido.");
        return ok(limites.map((l) => `${l.categoriaNome} [${l.categoriaId}]: limite ${formatarMoeda(l.valorLimite)}/mes`).join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "definir_limite_gasto",
    "Define (ou atualiza, se ja existir) o limite mensal de gasto de uma categoria. So funciona para categorias do tipo despesa e nao arquivadas.",
    { categoriaId: z.string().uuid(), valorLimite: z.number().positive() },
    async ({ categoriaId, valorLimite }) => {
      try {
        const limite = await api.post<LimiteGastoResponse>("api/limites-gasto", { categoriaId, valorLimite });
        return ok(`Limite definido: ${limite.categoriaNome} = ${formatarMoeda(limite.valorLimite)}/mes`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "remover_limite_gasto",
    "Remove o limite de gasto de uma categoria.",
    { categoriaId: z.string().uuid() },
    async ({ categoriaId }) => {
      try {
        await api.delete(`api/limites-gasto/${categoriaId}`);
        return ok(`Limite de gasto da categoria ${categoriaId} removido.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "gasto_vs_limite",
    "Compara o gasto realizado no mes contra o limite definido, por categoria. Sem categoriaId, retorna todas as categorias que tem limite definido.",
    {
      ano: z.number().int(),
      mes: z.number().int().min(1).max(12),
      categoriaId: z.string().uuid().optional(),
    },
    async ({ ano, mes, categoriaId }) => {
      try {
        if (categoriaId) {
          const resultado = await api.get<GastoVsLimiteResponse>(`api/limites-gasto/gasto-vs-limite/${categoriaId}`, { ano, mes });
          return ok(formatarGastoVsLimite(resultado));
        }
        const resultados = await api.get<GastoVsLimiteResponse[]>("api/limites-gasto/gasto-vs-limite", { ano, mes });
        if (resultados.length === 0) return ok("Nenhuma categoria com limite definido.");
        return ok(resultados.map(formatarGastoVsLimite).join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );
}
