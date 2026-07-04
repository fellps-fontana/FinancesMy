# Financeiro Pessoal

App de financas pessoais que consome a API REST do Pierre (Open Finance) e
combina com contas manuais. Stack: .NET 10 (API) + React/TypeScript/Vite (front) + PostgreSQL.

---

## REGRA OBRIGATORIA — leia antes de QUALQUER tarefa

Antes de codar, revisar ou testar qualquer coisa, leia NESTA ORDEM:

1. `context/regra-de-negocio.md`  -> a fonte da verdade do dominio
2. `context/clean-code.md`         -> padroes de codigo
3. `context/stack.md`              -> stack e convencoes tecnicas

Nunca escreva nem avalie codigo sem ter lido a regra de negocio.
Se uma decisao de codigo conflitar com a regra de negocio, a regra vence.
Se a regra de negocio for omissa sobre algo, PERGUNTE antes de assumir.

---

## Modos de trabalho

Quando eu pedir uma tarefa, identifique o modo e siga o agente correspondente:

- "arquitetar" / "desenhar" / "modelar" -> `agents/killua.md`
  Discute estrutura, modelagem e fluxo. NAO escreve codigo final.
- "codar" / "implementar" -> `agents/levi.md`
  Escreve codigo limpo seguindo clean-code.md e a regra de negocio.
- "coda a tela" / "componente" / "UI" / "front" -> `agents/hanzo.md`
  Implementa UI React seguindo a identidade visual e a regra de negocio.
- "revisar" -> `agents/style.md`
  Critica codigo contra a regra de negocio e o clean-code.md.
- "testar" -> `agents/mike.md`
  Gera e avalia testes cobrindo as regras de negocio.

---

## Principio do projeto

Eu (o desenvolvedor) escrevo a maior parte do codigo. O Claude atua como
parceiro: arquiteta, revisa, sugere e testa. Nao assuma o controle do codigo
sem eu pedir explicitamente para implementar.

---

## Estrutura

```
.claude/
  CLAUDE.md              <- este arquivo (indice + regra de leitura)
  context/
    regra-de-negocio.md  <- dominio (SEMPRE lido)
    clean-code.md        <- padroes de codigo
    stack.md             <- stack e convencoes
    identidade-visual.md <- direcao visual (dark/roxo/minimalista)
    schema.dbml          <- schema do banco (referencia)
  agents/
    killua.md            <- modo: desenhar/modelar
    levi.md              <- modo: implementar (back)
    hanzo.md             <- modo: implementar UI React
    style.md             <- modo: criticar codigo
    mike.md              <- modo: gerar testes
```
