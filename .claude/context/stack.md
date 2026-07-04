# Stack e convencoes

## Stack

- **Backend:** .NET 10, API REST. (ambiente de dev tem apenas SDK .NET 10 instalado; EF Core 8.0.4 e Npgsql 8.0.3 confirmados compativeis com o TFM net10.0)
- **ORM:** Entity Framework Core + Npgsql.
- **Banco:** PostgreSQL.
- **Frontend:** React (TypeScript) + Vite.
- **Fonte externa:** API REST do Pierre (Open Finance) via Bearer token.
- **Cotacao (v2):** Brapi ou similar para acoes BR.

## Integracao Pierre

- Autenticacao: header `Authorization: Bearer sk-...`.
- Base: `https://www.pierre.finance/tools/api`.
- Endpoints de leitura usados: get-transactions, get-accounts, get-balance,
  get-bills, manual-update.
- Sync via polling agendado (BackgroundService), nao tempo real.
- Dedup pela chave `pierre_txn_id`.

## Pontos de atencao do dado Pierre (ver regra-de-negocio.md)

- Sinal do `valor` nao confiavel em cartao -> usar `tipo` (DEBIT/CREDIT).
- Transacoes de mesma titularidade vem duplicadas -> excluir do calculo.
- Campo `manual_transaction` distingue origem manual de OF.
- Conciliacao por valor + janela de 1 dia.

## Convencoes

- Migrations versionadas pelo EF.
- IDs como uuid.
- Datas em UTC no banco; conversao na borda (UI).
- Idioma do dominio em portugues (nomes de tabela/campo conforme schema.dbml);
  codigo de infraestrutura pode usar ingles.

## Frontend (React)

- Vite como build. TypeScript estrito.
- Estado de servidor via camada dedicada (React Query ou hook proprio), nao
  fetch solto no componente.
- Componentes pequenos e por feature, espelhando os modulos do app.
- Identidade visual: ver `context/identidade-visual.md` quando existir.
