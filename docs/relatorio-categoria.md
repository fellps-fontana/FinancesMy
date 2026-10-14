# Relatorio de gasto por categoria

## Visao geral
O relatorio mostra quanto foi gasto no mes em cada categoria e a variacao contra o mes anterior. Ha uma tela de
detalhe por categoria com os lancamentos e o limite de gasto, se houver. Regras: itens 14 e 19 de
`regra-de-negocio.md`.

## Regras de negocio implementadas
- Conta so lancamentos DEBIT (item 19).
- Percentuais (0-100) e variacao vem prontos do backend; o front nao recalcula.
- A variacao e `null` quando o mes anterior e zero.
- Existe a linha "Sem categoria".
- No detalhe, o percentual e sobre o total do mes (confirmado pelo usuario em 2026-10-14).

## Endpoints e telas
- `GET /api/relatorios/gasto-por-categoria?ano=&mes=`
- O detalhe por categoria e por "sem-categoria".
- Telas `/relatorio`, `/relatorio/:categoriaId` e `/relatorio/sem-categoria`:
  - donut em SVG (`lib/fatiasRelatorio.ts`);
  - linhas com barra proporcional;
  - no detalhe, tabela de lancamentos e bloco de limite.

## Lacunas conhecidas
- O seletor de mes usa setas, nao o dropdown do mockup.
- A paleta tem 6 tons de token que se repetem ciclicamente.

## O que cada agent entregou (PR #73, 2026-10-14)
- levi: endpoints.
- hanzo: telas.
- style: 1 rodada de correcao (literal "sem-categoria", `lerMesDaQuery` e a regra "estourou" movidos para lib,
  formatador duplicado), depois APROVADO.
