# Modulo de Investimentos

## Visao geral

Permite ao usuario acompanhar contas de investimento (cofrinho, corretora,
carteira de acoes) dentro do patrimonio. Uma conta de investimento pode
funcionar de dois jeitos: saldo simples digitado a mao (cofrinho, XP sem
detalhe), ou carteira com ativos individuais (ticker, quantidade, preco medio,
preco atual), com compra, venda e grafico de historico de cotacao sob
demanda. Integracao real com corretora/cotacao em tempo real fica para v2.

## Regras de negocio implementadas

Ver `context/regra-de-negocio.md` secao 8 (e subsecoes 8.1-8.4) e secao 10
para o texto completo. Resumo:

- Conta tipo INVESTIMENTO, origem sempre MANUAL.
- Conta sem ativos: saldo = `saldo_manual`, editado pelo usuario.
- Conta com ativos: saldo = soma(`quantidade x preco_atual`) dos ativos
  ativos. Uma vez que a conta ganha seu primeiro ativo, fica permanentemente
  nesse modo (nao volta a `saldo_manual` mesmo zerando tudo).
- `preco_atual`: informado manualmente pelo usuario a cada compra, vale para
  toda a posicao (nao so a leva comprada).
- `preco_medio`: custo medio ponderado, recalculado SO em compra usando a
  quantidade ATUAL (pos-venda) como peso — venda nunca altera.
- Venda: reduz quantidade ou desativa o ativo (venda total); NAO gera
  lancamento em outra conta (fora de escopo v1).
- Grafico de cotacao: API externa (Brapi), consultado sob demanda, sem
  persistencia nem polling.

## Modelo de dados

- `conta` (compartilhada com outros tipos): `tipo=INVESTIMENTO`,
  `origem=MANUAL`, `saldo_manual` (null quando a conta tem ativos).
- `ativo`: `conta_id`, `ticker`, `nome`, `quantidade`, `preco_medio`,
  `preco_atual`, `ativa`.
- `movimentacao_ativo`: historico de compra/venda por ativo (`tipo`,
  `quantidade`, `preco_unitario`, `data`) — usado para auditoria e para o
  recalculo de preco medio funcionar corretamente apos venda parcial.

## Backend — entregue

- CRUD de conta de investimento (`ContasController`): criar, listar,
  atualizar saldo manual, desativar, total investido.
- `AtivoService`/`AtivoRepository`: `RegistrarCompra` (cria ou incrementa
  ativo, recalcula preco medio), `RegistrarVenda` (reduz quantidade/desativa),
  `ListarAtivosPorConta`.
- Excecoes de dominio: `ContaNaoEncontradaException`,
  `SaldoManualNaoPermitidoException`, `ContaNaoEhInvestimentoException`,
  `AtivoNaoEncontradoException`, `QuantidadeVendaInvalidaException`,
  `ValorInvalidoException`.
- 104 testes de integracao/unidade cobrindo o CRUD de conta (antes da
  expansao de escopo pra ativos).

## Backend — em andamento / pendente

- `AtivosController` (endpoints REST de compra/venda/listar) — TASK-015.
- Testes do `AtivoService` e do `AtivosController` — TASK-014/016.
- Extensao do saldo/total investido para somar ativos — TASK-017/018.
- Proxy de cotacao historica (Brapi) — TASK-019.

## Frontend — entregue

- `MyFinanceFrontEnd/src/features/investimentos/`: hooks React Query, tela
  de lista de contas com total investido, formulario de criar/editar/
  desativar conta simples.

## Frontend — pendente

- Camada de dados de ativos (hooks) — TASK-020.
- UI de lista de ativos, compra, venda, grafico — TASK-021 a 024.

## Decisoes e suposicoes relevantes

- Preco medio usa custo medio ponderado sobre a quantidade ATUAL (padrao
  B3/Receita Federal), nao soma simples das compras historicas — formula
  ingenua quebra apos qualquer venda parcial. Ver `regra-de-negocio.md` 8.2.
- Venda e operacao interna da carteira; nao gera lancamento em outra conta
  (decisao explicita de escopo minimo, nao pergunta em aberto).
- Cotacao historica via proxy no backend (nao chamada direta do front) —
  evita expor API key, centraliza tratamento de erro.
- Grafico usa Recharts (registrado em `stack.md`).

## Pendencias em aberto (nao decidido)

- Usuario pode atualizar `preco_atual` sem registrar compra nova
  ("marcar a mercado")? Nao implementado, fora de v1 ate confirmar.
- "Patrimonio total" do app (somando Open Finance + manual) depende de
  modulos que nao existem ainda (conta corrente, cartao, lancamento).

## Notas operacionais

- O backend deste modulo foi construido do zero (greenfield) numa branch
  isolada; ao integrar com o modulo de usuario (JWT auth, `AppDbContext`) via
  merge, foi necessario reconciliar manualmente `Program.cs`/`csproj`/config
  (dois DbContexts, dois WebApplicationFactory de teste precisando registrar
  ambos os contextos em InMemory). Ver historico de commits da branch
  `worktree-tasks-investimentos` para o detalhe da reconciliacao.
- `Dtos/` foi renomeado para `DTOs/` para alinhar com a convencao do modulo
  de usuario e evitar conflito de case-folding no Windows.
