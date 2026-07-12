# Modulo de Investimentos

**Status: completo (24/24 tasks concluidas).**

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
- Conta INVESTIMENTO com ativo ativo (quantidade > 0) NAO pode ser desativada
  — bloqueio no backend, evita esconder patrimonio real do usuario.
- Grafico de cotacao: API externa (Brapi), consultado sob demanda, sem
  persistencia nem polling (query com `refetchInterval` explicitamente
  desativado, ja que o app tem um default global de 5min pra outras telas).

## Modelo de dados

- `conta` (compartilhada com outros tipos): `tipo=INVESTIMENTO`,
  `origem=MANUAL`, `saldo_manual` (null quando a conta tem ativos).
- `ativo`: `conta_id`, `ticker`, `nome`, `quantidade`, `preco_medio`,
  `preco_atual`, `ativa`.
- `movimentacao_ativo`: historico de compra/venda por ativo (`tipo`,
  `quantidade`, `preco_unitario`, `data`) — usado para auditoria e para o
  recalculo de preco medio funcionar corretamente apos venda parcial.

## Backend — entregue (TASK-001 a 019)

- CRUD de conta de investimento (`ContasController`): criar, listar,
  atualizar saldo manual, desativar (com bloqueio se houver ativo ativo),
  total investido.
- `AtivoService`/`AtivoRepository`/`AtivosController`: `RegistrarCompra`
  (cria ou incrementa ativo, recalcula preco medio), `RegistrarVenda`
  (reduz quantidade/desativa, valida posse do ativo pela conta), listar
  ativos por conta.
- `CotacaoExternaService`/`CotacaoController`: proxy pra Brapi, sem
  persistencia, com tratamento de erro (404/502) sem vazar detalhe tecnico.
- Saldo/total investido estendidos pra somar ativos quando a conta esta em
  "modo carteira" (`ObterSaldosComModoContasInvestimento`).
- Excecoes de dominio: `ContaNaoEncontradaException`,
  `SaldoManualNaoPermitidoException`, `ContaNaoEhInvestimentoException`,
  `AtivoNaoEncontradoException`, `QuantidadeVendaInvalidaException`,
  `ValorInvalidoException`, `ContaComAtivosNaoPodeSerDesativadaException`,
  `TickerNaoEncontradoException`, `CotacaoExternaIndisponibilException`.
- 148 testes (unitarios + integracao HTTP) cobrindo CRUD de conta, compra/
  venda/recalculo de preco medio, saldo calculado, bloqueio de desativacao.

## Frontend — entregue (TASK-020 a 024)

- `MyFinanceFrontEnd/src/features/investimentos/`: hooks React Query pra
  conta, ativo e cotacao; tela de lista de contas com total investido;
  formulario de criar/editar/desativar conta simples; lista de ativos com
  saldo calculado; formularios de compra e venda de ativo; grafico de
  historico de cotacao (Recharts), aberto sob demanda por ativo.

## Decisoes e suposicoes relevantes

- Preco medio usa custo medio ponderado sobre a quantidade ATUAL (padrao
  B3/Receita Federal), nao soma simples das compras historicas — formula
  ingenua quebra apos qualquer venda parcial. Ver `regra-de-negocio.md` 8.2.
- Venda e operacao interna da carteira; nao gera lancamento em outra conta
  (decisao explicita de escopo minimo, nao pergunta em aberto).
- Conta com ativos fica permanentemente em modo calculado, mesmo apos vender
  tudo (confirmado pelo usuario) — e nao pode ser desativada enquanto tiver
  ativo com quantidade > 0 (achado do style durante revisao de UI, corrigido
  no backend).
- Cotacao historica via proxy no backend (nao chamada direta do front) —
  evita expor API key, centraliza tratamento de erro.
- Grafico usa Recharts (registrado em `stack.md`). Query de cotacao
  sobrescreve o `refetchInterval` global do `QueryClient` (5min, usado por
  outras telas pra refletir sync do Pierre) porque cotacao e sob demanda,
  nunca polling.

## Pendencias em aberto (nao decidido)

- Usuario pode atualizar `preco_atual` sem registrar compra nova
  ("marcar a mercado")? Nao implementado, fora de v1 ate confirmar.
- "Patrimonio total" do app (somando Open Finance + manual) depende de
  modulos que nao existem ainda (conta corrente, cartao, lancamento).

## Sintese do que cada agent entregou

- **killua**: modelou `Ativo`/`MovimentacaoAtivo`, decompos 24 tasks,
  formalizou a mudanca de escopo v1 (ativo entra, tempo real continua v2) em
  `regra-de-negocio.md` apos pedido do usuario.
- **levi**: implementou todo o backend. Cometeu 2 incidentes de processo
  (commit sem autorizacao numa task, bug de DI no HttpClient da Brapi
  corrigido em 1 rodada) — codigo em si sempre corrigido apos apontado.
- **style**: gate obrigatorio em toda regra critica. Achados relevantes:
  formula de preco medio validada matematicamente na mao; bug de seguranca
  (venda ignorando `contaId` da rota, permitia vender ativo de qualquer
  conta); gap de regra (desativar conta escondia patrimonio com ativo ativo);
  bug de DI (HttpClient da Brapi nunca configurado, endpoint sempre falhava);
  polling herdado indevidamente no grafico de cotacao. A maioria das tasks
  levou 1-2 rodadas de correcao antes de aprovar.
- **mike**: 148 testes no total. Um incidente: diagnosticou mal um erro de
  compilacao (duplicacao de metodos) como "limitacao do xUnit" e chegou a
  apagar um teste ja aprovado no processo — corrigido revertendo pro estado
  limpo e mantendo a cobertura em arquivo separado.
- **hanzo**: toda a UI. Corrigiu proativamente um bug de saldo zerado
  (fallback `saldoManual ?? 0` que nao valia mais com o campo `saldo`
  calculado) durante a TASK-021, sem esperar apontamento.

## Notas operacionais

- O backend deste modulo foi construido do zero (greenfield) numa branch
  isolada; ao integrar com o modulo de usuario (JWT auth, `AppDbContext`) via
  merge, foi necessario reconciliar manualmente `Program.cs`/`csproj`/config
  (dois DbContexts, dois WebApplicationFactory de teste precisando registrar
  ambos os contextos em InMemory).
- `Dtos/` foi renomeado para `DTOs/` para alinhar com a convencao do modulo
  de usuario e evitar conflito de case-folding no Windows.
- Recharts adicionado como dependencia nova do frontend (nao existia antes
  desta task).
