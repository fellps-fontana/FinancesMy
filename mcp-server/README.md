# MyFinances MCP Server

Servidor MCP (Model Context Protocol) que expoe a API REST do MyFinances como
tools para o Claude, para consultar e editar dados financeiros e receber
alertas de contas a pagar direto na conversa, sem abrir o frontend.

Este servidor **nao acessa o Postgres diretamente** - ele e um cliente HTTP da
API `.NET` existente (`MyFinances/MyFinances`), respeitando toda a regra de
negocio (calculo de fatura, saldo, projecao do mes etc).

## Como rodar

```bash
cd mcp-server
npm install
cp env.example .env   # preencha com as credenciais do seu usuario na API
npm run build
```

Variaveis de ambiente (`.env`):

| Variavel | Obrigatoria | Padrao | Descricao |
|---|---|---|---|
| `MYFINANCES_API_URL` | nao | `http://localhost:5146` | URL base da API .NET |
| `MYFINANCES_USERNAME` | sim | - | Usuario/email para login no `AuthController` |
| `MYFINANCES_PASSWORD` | sim | - | Senha do usuario |
| `MCP_HTTP_PORT` | nao | `3939` | Porta do modo HTTP (`npm run start:http`), ver secao abaixo |

O servidor faz login automaticamente no primeiro request que precisar de
autenticacao (lazy), guarda o token JWT em memoria, e refaz login sozinho se
algum request voltar `401`.

Para testar em modo desenvolvimento sem compilar (`tsx` direto no TypeScript):

```bash
npm run dev
```

## Registrar no Claude Desktop / Claude Code

Adicione ao `claude_desktop_config.json` (Claude Desktop) ou configure como
MCP server no Claude Code:

```json
{
  "mcpServers": {
    "myfinances": {
      "command": "node",
      "args": ["D:/Estudos/MyFinances/mcp-server/dist/index.js"],
      "env": {
        "MYFINANCES_API_URL": "http://localhost:5146",
        "MYFINANCES_USERNAME": "teste",
        "MYFINANCES_PASSWORD": "Teste123!"
      }
    }
  }
}
```

Ajuste o caminho de `args` para onde o repo estiver na sua maquina. A API
`.NET` (`dotnet run` em `MyFinances/MyFinances`) precisa estar rodando para as
tools funcionarem.

## Modo HTTP (acesso de outros computadores via rede/VPN)

O modo acima (`command`/`args`, transporte stdio) roda um processo local por
maquina - nao da pra "compartilhar" com outro computador. Pra acessar de
qualquer dispositivo da sua rede/VPN so colando uma URL no Claude, suba o
servidor no modo HTTP em vez do stdio:

```bash
MCP_HTTP_PORT=3939 npm run start:http
# ou, em desenvolvimento: MCP_HTTP_PORT=3939 npm run dev:http
```

Isso sobe um servidor Express escutando em `0.0.0.0:3939` (todas as
interfaces de rede, nao so localhost), implementando o transporte
Streamable HTTP do MCP no endpoint `/mcp`. Rode isso numa unica maquina (a
mesma que tem a API `.NET` acessivel, local ou na rede) - os outros
dispositivos so precisam da URL, sem instalar nada.

Nos outros computadores, registre como servidor remoto (sem `command`/`args`,
so a URL):

```json
{
  "mcpServers": {
    "myfinances": {
      "type": "http",
      "url": "http://<ip-da-maquina-que-roda-o-servidor>:3939/mcp"
    }
  }
}
```

No Claude Code tambem da pra registrar via CLI:

```bash
claude mcp add --transport http myfinances http://<ip>:3939/mcp
```

**Sem autenticacao no endpoint HTTP em si** - decisao deliberada, assumindo
que o acesso e restrito a uma rede/VPN de confianca (nao exponha essa porta
pra internet). Qualquer dispositivo que alcance `http://<ip>:3939/mcp`
consegue chamar todas as tools, inclusive as de escrita
(criar/editar/excluir lancamento, pagar fatura etc), usando as credenciais
configuradas no `.env` de quem sobe o servidor.

## Tools disponiveis

### Alerta (a mais importante)

- **`contas_a_pagar`** - resumo do que precisa ser pago: lancamentos
  pendentes em contas banco (inclui as ocorrencias geradas por contas fixas),
  faturas de cartao ainda nao quitadas, e opcionalmente contas a receber
  previstas. Ordena por urgencia (vencidos primeiro). Parametros: `dias`
  (janela em dias a frente, padrao 7 - itens vencidos sempre entram) e
  `incluirContasReceber` (padrao false).

### Contas

- `listar_contas` (`tipo?`), `criar_conta`, `saldo_conta`,
  `atualizar_saldo_conta`, `desativar_conta`, `total_investido`

### Lancamentos

- `listar_lancamentos` (`contaId?`, `categoriaId?`, `dataInicio?`, `dataFim?`,
  `status?`, `tipo?`), `criar_lancamento`, `editar_lancamento`,
  `marcar_lancamento_pago`, `remover_lancamento`

### Categorias

- `listar_categorias`, `criar_categoria`, `editar_categoria`,
  `arquivar_categoria`, `reativar_categoria`

### Contas fixas (recorrentes)

- `listar_contas_fixas`, `criar_conta_fixa`, `editar_conta_fixa`,
  `desativar_conta_fixa`, `reativar_conta_fixa`

### Contas a receber

- `listar_contas_receber`, `obter_conta_receber`, `registrar_recebivel`,
  `registrar_emprestimo`, `registrar_recebimento`, `total_esperado_mes`

### Cartao de credito

- `criar_compra_cartao`, `editar_compra_cartao`, `criar_compra_parcelada`,
  `estornar_compra_parcelada`, `listar_faturas`, `obter_fatura`,
  `pagar_fatura`, `criar_estorno_fatura`

### Limites de gasto

- `listar_limites_gasto`, `definir_limite_gasto`, `remover_limite_gasto`,
  `gasto_vs_limite`

### Dashboard

- `projecao_mes`

### Investimentos

- `listar_ativos`, `criar_ativo`, `atualizar_valor_ativo`, `desativar_ativo`,
  `resumo_ativos`, `registrar_dividendo`, `historico_rendimentos`,
  `resumo_rendimentos`, `registrar_aporte`, `listar_aportes`

### Transferencias

- `criar_transferencia`

## Decisoes de modelagem e limitacoes conhecidas da API

- **Sem "marcar conta fixa como paga" dedicado**: a API nao tem esse
  endpoint. Uma conta fixa gera automaticamente lancamentos `PENDENTE`; para
  pagar, use `listar_lancamentos` pra achar o id e depois
  `marcar_lancamento_pago`. Nao existe campo que ligue o lancamento de volta
  a conta fixa que o gerou.
- **Sem "marcar conta a receber como recebida"**: o status vira `RECEBIDO`
  automaticamente quando os `registrar_recebimento` somam o valor total.
- **Sem "marcar fatura como paga"**: idem, `pagar_fatura` aceita pagamento
  parcial/antecipado e a fatura vira `PAGA` sozinha quando quitada.
- **Sem endpoint para listar as compras dentro de uma fatura** - a API so
  expoe o agregado (`FaturaResponse`), nao a lista de compras que compoem
  ela. Nao existe tool pra isso porque o endpoint nao existe.
- **`listar_lancamentos` sem filtro nativo na API**: a API so expoe
  `fluxo-caixa` por conta, sem filtro de periodo/categoria. A tool busca em
  todas as contas (ou na informada) e filtra no lado do MCP.
- **Tools de `DeParaCategorias`** (mapeamento de categoria importada do Pierre
  -> categoria do usuario) **nao foram implementadas** de proposito: segundo
  a documentacao do modulo, esse recurso ainda nao esta conectado a nenhum
  fluxo de importacao, entao nao ha utilidade pratica em expor via chat ainda.
- **Case de enum inconsistente entre request/response da propria API**:
  alguns campos (ex: `Conta.Tipo`) voltam em PascalCase (`"Banco"`) mas
  precisam ser enviados em maiusculas com underscore (`"BANCO"`); outros
  campos (ex: `Lancamento.Status`) usam maiusculas com underscore nos dois
  sentidos. O servidor MCP normaliza isso internamente - os schemas das
  tools sempre aceitam valores em portugues minusculo (`banco`, `pendente`,
  `renda_fixa` etc).

## Teste manual

Validado nesta sessao contra a API `.NET` real rodando em
`http://localhost:5146` (Postgres via container `myfinances-postgres`, banco
`myfinances_dev`, usuario dev `teste`/`Teste123!`):

- **Build limpo** (`tsc --noEmit` e `npm run build`, sem erros de tipo).
- **Handshake MCP completo**: `initialize` + `tools/list` via stdio retornam
  as 52 tools com `inputSchema` valido (JSON Schema gerado corretamente a
  partir dos schemas zod).
- **`listar_contas`** (`tipo=banco` e `tipo=cartao`) - retornou a conta
  Nubank banco (saldo R$ 20,00) e a conta Nubank cartao (fechamento dia 10,
  vencimento dia 18).
- **`listar_lancamentos`** - retornou o lancamento pendente real "Aluguekl",
  R$ 20,00, vencendo 12/09/2026.
- **`contas_a_pagar`** - com a janela padrao (7 dias) retornou corretamente
  "nada pendente" (o lancamento acima estava a 20 dias); com `dias=30`
  encontrou o mesmo lancamento e formatou "vence em 20 dia(s)". Confirma que
  o filtro de urgencia e o calculo de dias estao corretos.
- **`projecao_mes`** (8/2026) - retornou totais coerentes com os dados
  (recebido R$ 0, a receber R$ 123, pago R$ 123, a pagar R$ 0).
- **Round-trip de escrita**: `criar_lancamento` (R$ 1,23, descricao "TESTE
  MCP - pode apagar") seguido de `remover_lancamento` no id retornado -
  criou e removeu com sucesso, sem deixar dado de teste no banco.
- **Modo HTTP**: `npm run start:http` (porta 3939), `initialize` via `curl`
  retornou `mcp-session-id` no header e a resposta em SSE; reusando esse
  session id, `notifications/initialized` (202) seguido de `tools/call`
  `listar_contas` retornou os mesmos dados reais da conta Nubank.

Para rodar voce mesmo:

```bash
# 1. Suba o Postgres (ajuste conforme seu setup local) e garanta que o banco
#    myfinances_dev existe e as migrations rodaram.

# 2. Suba a API
cd MyFinances/MyFinances
dotnet run
# API sobe em http://localhost:5146, com o usuario dev "teste"/"Teste123!"
# semeado automaticamente (Data/DevUserSeeder.cs) se nao existir.

# 3. Em outro terminal, valide o build e o handshake do MCP
cd mcp-server
npm run build
MYFINANCES_USERNAME=teste MYFINANCES_PASSWORD='Teste123!' npm start
# o processo fica esperando input JSON-RPC via stdin - use um cliente MCP
# (Claude Desktop/Code) ou um script de teste manual.
```
