import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { buildServer } from "./server.js";

async function main() {
  const server = buildServer();
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("MyFinances MCP server rodando via stdio.");
}

main().catch((e) => {
  console.error("Falha ao iniciar o servidor MCP:", e);
  process.exit(1);
});
