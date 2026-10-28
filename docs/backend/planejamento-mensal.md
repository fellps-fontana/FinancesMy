# Planejamento mensal (sublimite por categoria)

Plano de um mes calendario: parte da renda prevista, desconta as despesas
fixas (conta fixa e assinatura de cartao) e o investimento, e distribui a
sobra em sublimites percentuais por categoria de despesa. So calcula, informa
e alerta: nunca bloqueia lancamento nem gera lancamento, transferencia ou
aporte. O "limite de gasto" de uma categoria e o sublimite dela no plano do
mes; nao existe mais limite avulso (tabela `limite_gasto` dropada).

## Endpoints

- `GET /api/planejamento-mensal/{ano}/{mes}` -> 200 plano calculado: renda,
  fixas, investimento planejado/calculado, flags `investimentoReduzido` e
  `planoNaoFecha`, sobra, `naoAlocado`, `gastoSemSublimite`, `sublimites[]`
  (percentual, valor, gasto, fracao, estourado, categoria arquivada) e
  `gastosPorCategoria[]` (gasto variavel do mes de toda categoria DESPESA,
  com ou sem sublimite). 404 mes passado sem plano; 400 ano/mes invalido ou
  alem do horizonte. Mes corrente/futuro sem plano e gerado na consulta.
- `PUT /api/planejamento-mensal/{ano}/{mes}` com `rendaPrevista`,
  `percentualInvestimento` e `sublimites[]` (`categoriaId`, `percentual`),
  todos obrigatorios; o conjunto substitui o atual (`[]` limpa). 200 | 400
  validacao | 404 categoria | 409 mes passado | 422 regra de sublimite.
- `GET /api/relatorios/gasto-por-categoria/detalhe`: o bloco `limite` le o
  sublimite do plano do mes (ver `docs/backend/relatorio-categoria.md`).
- MCP (`mcp-server/src/tools/planejamentoMensal.ts`):
  - `obter_planejamento_mensal(ano, mes)`: plano formatado + gasto variavel
    por categoria com hierarquia "Pai > Sub" (inclui arquivadas, marcadas);
  - `salvar_planejamento_mensal(ano, mes, rendaPrevista,
    percentualInvestimento | valorInvestimento, sublimites[{categoriaId,
    percentual | valor}])`: aceita % ou R$ (exatamente um por campo) e
    converte R$ -> % antes do PUT; categoria duplicada e erro;
  - `copiar_planejamento_mensal(anoOrigem, mesOrigem, anoDestino, mesDestino,
    copiarRenda?)`: copia % de investimento e sublimites ativos da origem.

## Regras aplicadas

Referencias em `.claude/context/regra-de-negocio.md`:

- Item 20 (critico): formula de fixas, investimento, sobra, sublimites,
  `nao_alocado` e `gasto_sem_sublimite`; renda prevista inicial pelos
  recebiveis recorrentes ativos; geracao sob demanda copiando o mes
  anterior; horizonte de mes corrente + 12; mes passado somente leitura;
  validacoes do salvamento; tela unificada com edicao inline R$ <-> %;
  "Copiar de outro mes".
- Item 14: sublimite so para DESPESA nao arquivada; hierarquia (sub exige
  pai; zerar o pai zera as subcategorias ativas); gasto do sublimite so
  variavel; sublimite de categoria arquivada continua no plano e nos
  calculos, reenvio dele e recusado, e no rascunho da tela ele e intocavel
  ate o clique em "Remover" (decisao de 2026-10-26).
- Item 19: barra do detalhe do relatorio compara sublimite x gasto variavel.

## Decisoes tecnicas vigentes

- Valores em R$ nunca sao gravados: recalculados a cada consulta, em decimal
  sem arredondamento intermediario.
- Dominio puro: `PlanejamentoMensalCalculator` (um metodo por termo, incluindo
  `CalcularGastosPorCategoria`), `PlanoMensalValidator` (ordem de checagem
  documentada) e `PlanoMensalInicialFactory` (renda + copia).
  `PlanejamentoMensalService` so orquestra.
- `PlanoMensalRepository.AdicionarOuObterExistente` resolve a corrida da
  geracao sob demanda pelo indice unico (`ano`, `mes`).
- Front (`features/planejamento-mensal/`): a previa em tempo real da tela
  reimplementa o calculo do item 20 em `lib/rascunho.ts` (funcoes puras),
  coberta por `lib/rascunho.test.ts` (vitest, 78 testes). O back continua
  sendo a fonte da verdade; salvar envia so percentuais. O teto da renda e
  validado no cliente pelo texto (16 digitos inteiros) porque o valor exato
  nao cabe em double; o back revalida.
- O MCP repete a formula de sobra/investimento so para converter R$ -> %
  antes do PUT; o back recalcula tudo.

## Lacunas conhecidas

- O GET do plano (e o detalhe do relatorio) grava quando gera o plano sob
  demanda. Permitido pelo item 20.
- Dois salvamentos concorrentes do mesmo plano podem estourar o indice
  (plano, categoria) -> 500. Single-user: so registrado.
- A previa do front e o back podem divergir se a formula mudar so de um
  lado; o teste do front so cobre o lado do front.
- mcp-server nao tem teste automatizado.
- "Remover" num pai arquivado tambem remove os sublimites das subcategorias
  dele no rascunho; a regra nao decide esse caso (aguardando o usuario).
- Fora da v1 (regra): conferir se o investimento foi realizado; diluir conta
  fixa ANUAL; sublimite em valor fixo.
- Dado sem `usuario_id`: o plano e global, como o resto do dominio.
