# Módulo: Contas

## Visão geral

Página genérica que lista todas as contas manuais do usuário — bancárias
(corrente/poupança/dinheiro físico) e de investimento — num único lugar,
com ícone, cor e patrimônio total somado. Substitui a antiga
`ListaContasSimplesPage.tsx` (que só criava/listava contas
`tipo=INVESTIMENTO`, hardcoded). Contas do tipo CARTAO continuam de fora
(têm página própria em `/cartao`).

## Regras de negócio implementadas

Itens 1, 8 e 10/10.1 da `regra-de-negocio.md`:

- **Subtipo só se aplica a `Tipo=Banco`**: `Conta.Subtipo` (enum `Corrente`
  \| `Poupanca` \| `DinheiroFisico`) é `null` para INVESTIMENTO e CARTAO.
  Informar um subtipo numa conta que não é Banco é **ignorado
  silenciosamente** — a criação/edição segue normalmente com
  `Subtipo = null`, não rejeita a request.
- **Ícone e cor são metadado puro descritivo**, sem validação de catálogo no
  backend (string livre) — o catálogo fixo de ícones Lucide e a paleta de
  cor fixa vivem só no front.
- **Saldo manual pode ser negativo — decisão confirmada com o usuário
  durante esta entrega.** Vale para TODA conta manual (banco e
  investimento simples): não há piso em zero nem validação de saldo
  mínimo; `saldo_manual` reflete exatamente o que o usuário informar,
  inclusive dívida/saldo devedor. Formalizado no item 10 da
  `regra-de-negocio.md` (antes era omisso sobre o assunto).

## Modelo de dados e endpoints

`Conta` ganhou 3 colunas nullable (migration aditiva
`AddSubtipoIconeCorToConta`, registros existentes ficam `null` sem
quebrar): `subtipo` (enum, storage no mesmo padrão de `TipoConta`),
`icone` (string), `cor` (string, hex).

`POST /api/contas` e `ContaResponse` (`ContasController`) expõem os 3
campos novos, todos opcionais.

Frontend: nova feature `features/contas/` (antes só `.gitkeep`):
- `ContasPage.tsx` — lista combinando `GET /api/contas?tipo=banco` e
  `?tipo=investimento`, ícone/cor/subtítulo (subtipo ou tipo) por item,
  badge de origem (Manual/OFX), patrimônio total somado
  (`lib/calcularPatrimonioTotal.ts`).
- `components/FormNovaConta.tsx` — nome, tipo/subtipo, saldo inicial com
  máscara de moeda (`lib/mascaraMoeda.ts`, formata `R$ 0,00` enquanto
  digita), seletor de ícone (catálogo fixo Lucide) e seletor de cor
  (paleta fixa curta, não color-picker livre — consistente com
  `identidade-visual.md`).
- Rota `/contas` trocada em `routes.tsx` para `ContasPage`;
  `ListaContasSimplesPage.tsx` removida.

### Consolidação com `features/investimentos/`

Como `ContasPage` passou a cobrir também contas de investimento, o fluxo
de criação/gestão que antes vivia duplicado em `features/investimentos/`
foi descontinuado e consolidado:
- `shared/lib/formatarMoeda.ts` e `shared/lib/validarSaldo.ts` viraram
  fonte única (compartilhada entre `contas/` e `investimentos/`, que agora
  reexportam a partir daí).
- Removidos por virarem código morto (único consumidor era a página
  antiga, já substituída): `ContaInvestimentoCard.tsx`,
  `FormCriarContaInvestimento.tsx`, `TotalInvestidoResumo.tsx`,
  `validarNovaConta.ts` e os hooks
  `useAtualizarSaldoConta`/`useContasInvestimento`/`useCriarContaInvestimento`/
  `useDesativarConta`/`useTotalInvestido` da pasta `investimentos/`.
- O fluxo de **Ativos** (não confundir com conta de investimento) continua
  intacto em `features/investimentos/ListaAtivosPage.tsx`; link cruzado
  "Ver investimentos (ativos)" preservado na `ContasPage`.

## Lacunas conhecidas

- Nenhuma pendência aberta identificada pelo `style` na aprovação final.

## O que cada agent entregou (TASK-127 a 130)

- **levi**: `SubtipoConta`, `Icone`, `Cor` em `Conta`, DTOs, `ContaConfiguration`,
  migration aditiva. `dotnet test --filter FullyQualifiedName~Contas`:
  45/45 verdes.
- **hanzo**: `features/contas/` completa (página, form, hooks, lib,
  componentes) + troca de rota. 1ª rodada do style achou 3 problemas reais
  (ver abaixo); hanzo corrigiu no mesmo PR: `shared/lib/` como fonte única,
  `validarNovaConta.ts` removido (código morto), catálogo de ícone
  derivado de uma única fonte (`CATALOGO_ICONES` a partir de
  `ICONE_POR_NOME`, eliminando a duplicação entre `obterIconeConta.ts` e
  `FormNovaConta.tsx`).
- **style**: 1ª rodada — PRECISA CORRIGIR (contradição de saldo negativo
  entre front, que bloqueava, e back, que já aceitava e testava negativo;
  import cruzado `features/contas` → `features/investimentos/lib`;
  catálogo de ícone duplicado). 2ª rodada, após correção: APROVADO,
  confirmado com execução própria (`tsc --noEmit` limpo, suite .NET
  45/45). Achado remanescente não-bloqueante: texto do item 10.1 da regra
  dizia "rejeitado" quando o comportamento real é "ignorado
  silenciosamente" — corrigido direto no documento.
- **killua**: atualizou `regra-de-negocio.md` (itens 10 e 10.1) via skill
  `alterar-context`, formalizando a decisão de saldo negativo e a regra
  de subtipo só aplicável a Banco.

## Notas operacionais

- `MyFinances.csproj` teve bump de versão do pacote EF Core (10.0.0 →
  10.0.10) e ganhou referência a `Microsoft.EntityFrameworkCore.Tools`,
  necessário pra gerar a migration via `dotnet ef` — fora do escopo
  original da task, mas é infraestrutura, não regra de negócio.
- Ambiente de dev local: o banco Postgres local (`myfinances_dev`) já foi
  perdido/recriado mais de uma vez entre sessões de teste manual. Se o
  app reclamar de coluna faltando (`column ... does not exist`), o schema
  local está desalinhado das migrations — ver nota equivalente em
  `docs/projecao-do-mes.md`.
