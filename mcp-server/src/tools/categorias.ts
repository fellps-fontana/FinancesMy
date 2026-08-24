import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { api } from "../apiClient.js";
import { ok, err } from "../mcpHelpers.js";
import { formatarEnum } from "../format.js";
import { paraPascalCase } from "../enums.js";

export interface CategoriaResponse {
  id: string;
  nome: string;
  tipo: string;
  parentId?: string | null;
  subcategorias: CategoriaResponse[];
  arquivada: boolean;
  icone?: string | null;
}

export function formatarCategoria(c: CategoriaResponse, indent = ""): string {
  const linha = `${indent}${c.nome} [${c.id}] (${formatarEnum(c.tipo)}${c.arquivada ? ", arquivada" : ""})`;
  const filhas = c.subcategorias?.map((sub) => formatarCategoria(sub, indent + "  ")).join("\n") ?? "";
  return filhas ? `${linha}\n${filhas}` : linha;
}

export function registerCategoriasTools(server: McpServer) {
  server.tool(
    "listar_categorias",
    "Lista categorias (despesa/receita), com hierarquia de ate 1 nivel de subcategorias.",
    {
      tipo: z.enum(["despesa", "receita"]).optional(),
      arquivada: z.boolean().optional(),
      parentId: z.string().uuid().optional(),
    },
    async ({ tipo, arquivada, parentId }) => {
      try {
        const categorias = await api.get<CategoriaResponse[]>("api/Categorias", {
          tipo: tipo ? paraPascalCase(tipo) : undefined,
          arquivada,
          parentId,
        });
        if (categorias.length === 0) return ok("Nenhuma categoria encontrada.");
        return ok(categorias.map((c) => formatarCategoria(c)).join("\n"));
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "criar_categoria",
    "Cria uma categoria ou subcategoria (max 1 nivel de profundidade; a subcategoria herda o tipo do pai).",
    {
      nome: z.string().min(1),
      tipo: z.enum(["despesa", "receita"]),
      parentId: z.string().uuid().optional(),
      icone: z.string().optional(),
    },
    async ({ nome, tipo, parentId, icone }) => {
      try {
        const categoria = await api.post<CategoriaResponse>("api/Categorias", {
          nome,
          tipo: paraPascalCase(tipo),
          parentId,
          icone,
        });
        return ok(`Categoria criada:\n${formatarCategoria(categoria)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "editar_categoria",
    "Edita uma categoria (substitui nome/parentId/icone por completo - icone omitido e removido).",
    {
      categoriaId: z.string().uuid(),
      nome: z.string().min(1),
      parentId: z.string().uuid().optional(),
      icone: z.string().optional(),
    },
    async ({ categoriaId, nome, parentId, icone }) => {
      try {
        const categoria = await api.put<CategoriaResponse>(`api/Categorias/${categoriaId}`, { nome, parentId, icone });
        return ok(`Categoria atualizada:\n${formatarCategoria(categoria)}`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "arquivar_categoria",
    "Arquiva uma categoria (e suas subcategorias em cascata). E a unica forma de 'excluir' uma categoria.",
    { categoriaId: z.string().uuid() },
    async ({ categoriaId }) => {
      try {
        await api.patch(`api/Categorias/${categoriaId}/arquivar`);
        return ok(`Categoria ${categoriaId} arquivada.`);
      } catch (e) {
        return err(e);
      }
    }
  );

  server.tool(
    "reativar_categoria",
    "Reativa uma categoria previamente arquivada.",
    { categoriaId: z.string().uuid() },
    async ({ categoriaId }) => {
      try {
        await api.post(`api/Categorias/${categoriaId}/reativar`);
        return ok(`Categoria ${categoriaId} reativada.`);
      } catch (e) {
        return err(e);
      }
    }
  );
}
