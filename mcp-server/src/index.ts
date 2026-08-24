import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { registerContasTools } from "./tools/contas.js";
import { registerLancamentosTools } from "./tools/lancamentos.js";
import { registerCategoriasTools } from "./tools/categorias.js";
import { registerContasFixasTools } from "./tools/contasFixas.js";
import { registerContasReceberTools } from "./tools/contasReceber.js";
import { registerCartaoTools } from "./tools/cartao.js";
import { registerLimitesGastoTools } from "./tools/limitesGasto.js";
import { registerDashboardTools } from "./tools/dashboard.js";
import { registerAtivosTools } from "./tools/ativos.js";
import { registerTransferenciasTools } from "./tools/transferencias.js";
import { registerContasAPagarTools } from "./tools/contasAPagar.js";

const server = new McpServer({
  name: "myfinances",
  version: "1.0.0",
});

registerContasAPagarTools(server);
registerContasTools(server);
registerLancamentosTools(server);
registerCategoriasTools(server);
registerContasFixasTools(server);
registerContasReceberTools(server);
registerCartaoTools(server);
registerLimitesGastoTools(server);
registerDashboardTools(server);
registerAtivosTools(server);
registerTransferenciasTools(server);

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("MyFinances MCP server rodando via stdio.");
}

main().catch((e) => {
  console.error("Falha ao iniciar o servidor MCP:", e);
  process.exit(1);
});
