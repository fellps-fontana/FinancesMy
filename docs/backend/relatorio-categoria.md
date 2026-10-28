# Relatorio de gasto por categoria

Mostra quanto foi gasto no mes em cada categoria e a variacao contra o mes
anterior, com uma tela de detalhe por categoria (lancamentos e barra do
sublimite do planejamento mensal, se houver).

## Endpoints

- `GET /api/relatorios/gasto-por-categoria?ano=&mes=`: linhas por categoria
  (inclui "Sem categoria") com total, percentual (0-100) e variacao.
- Detalhe por categoria e por "sem-categoria": lancamentos do mes, percentual
  sobre o total do mes e bloco `limite` com o sublimite da categoria no
  plano do mes (`valorLimite` = valor do sublimite, `gastoRealizado` = gasto
  variavel, `estourado` calculado no back). `limite` e `null` sem plano, sem
  sublimite da categoria, alem do horizonte ou para "Sem categoria".
- Telas `/relatorio`, `/relatorio/:categoriaId` e `/relatorio/sem-categoria`.

## Regras aplicadas

Referencias em `.claude/context/regra-de-negocio.md`:

- Item 19: so lancamentos DEBIT; variacao `null` quando o mes anterior e
  zero; linha "Sem categoria"; no detalhe o percentual e sobre o total do
  mes (decisao de 2026-10-14).
- Itens 14 e 20: a barra do detalhe compara o sublimite com o gasto variavel,
  que pode ser menor que o total da linha (fixas ficam fora do sublimite).

## Decisoes tecnicas vigentes

- Percentuais, variacao e estouro vem prontos do back; o front nao recalcula.
- `RelatorioCategoriaService` le o sublimite via
  `IPlanejamentoMensalService` (consultar o detalhe pode gerar o plano do mes
  sob demanda, como o GET do plano).
- Donut em SVG (`lib/fatiasRelatorio.ts`).

## Lacunas conhecidas

- O seletor de mes usa setas, nao o dropdown do mockup.
- A paleta tem 6 tons de token que se repetem ciclicamente.
