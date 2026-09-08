# Módulo: Assinatura de Cartão (Recorrência em Cartão)

## Visão Geral

O módulo de Assinatura de Cartão introduz a entidade `AssinaturaCartao`, um molde de despesa recorrente vinculada exclusivamente a contas do tipo `CARTAO`. Diferente de contas fixas de fluxo de caixa (que geram lançamentos pendentes em conta bancária) ou compras parceladas (que possuem fim determinado e dividem um valor total), a assinatura materializa automaticamente uma Compra (regime de competência, `Status = Pago`, vinculada a `Fatura`) em cada ciclo de fatura do cartão.

Entrada na regra de negócio: **Item 16 da regra-de-negocio.md** (decisões registradas em 2026-09-08).

---

## Decisões de Produto (PO)

1. **Separação de Entidade:** Não sobrecarregar `ContaFixa`. Assinaturas de cartão possuem ciclo de vida, faturamento e semântica histórica totalmente distintos de contas a pagar bancárias.
2. **Propagação de Edição:** Ao editar valor, descrição ou categoria, a compra vinculada na fatura corrente é atualizada **se e somente se** a fatura ainda estiver `ABERTA`. Faturas `FECHADA` ou `PAGA` são fatos consumados e nunca são alteradas.
3. **Escopo v1:**
   - Periodicidade estritamente **MENSAL** ancorada em `dia_referencia` (1-31).
   - Sem prazo de término pré-fixado (contínua até desativação explícita).
   - Geração sob demanda ao consultar/listar faturas via `GarantirAssinaturasDoCicloAsync`.
4. **Desativação:** Desativar (`ativa = false`) mantém todas as compras já geradas intocadas. Cessa apenas novas gerações futuras. Reativar volta a gerar a partir do ciclo corrente com proteção de idempotência.

---

## Entidade `AssinaturaCartao`

| Campo | Tipo | Descrição |
| --- | --- | --- |
| `id` | uuid (PK) | Identificador único |
| `conta_id` | uuid (FK) | Conta do tipo `CARTAO` obrigatória |
| `categoria_id` | uuid (FK opcional) | Categoria sugerida de despesa |
| `descricao` | varchar | Descrição (ex: "Netflix", "Spotify") |
| `valor` | decimal | Valor cobrado em cada ciclo |
| `dia_referencia` | int (1-31) | Dia base para a data da compra |
| `ativa` | boolean | Status de ativação (default: true) |

---

## Endpoints da API

- `POST /api/assinaturas-cartao` — Cadastra nova assinatura e gera compra no ciclo atual
- `PUT /api/assinaturas-cartao/{id}` — Atualiza dados e propaga se fatura atual estiver aberta
- `POST /api/assinaturas-cartao/{id}/desativar` — Desativa assinatura (compras históricas preservadas)
- `POST /api/assinaturas-cartao/{id}/reativar` — Reativa assinatura e gera ocorrência corrente
- `GET /api/assinaturas-cartao/{id}` — Detalhe da assinatura
- `GET /api/assinaturas-cartao?contaId=&ativa=` — Listagem com filtros

---

## Ferramentas MCP Planejadas

- `criar_assinatura_cartao`
- `listar_assinaturas_cartao`
- `editar_assinatura_cartao`
- `desativar_assinatura_cartao`
- `reativar_assinatura_cartao`
