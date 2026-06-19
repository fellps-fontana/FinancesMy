# Identidade Visual — Financeiro Pessoal

Direcao: escuro, roxo, minimalista. Legibilidade e prazer de uso acima de tudo.
Sem brilho/neon/gradiente chamativo (cansa em app de uso diario). Cor carrega
significado, nao e enfeite.

Este documento alimenta o modo Front. Ao codar UI, usar estes tokens.

---

## Cores — base (dark)

| Token              | Hex       | Uso                                    |
|--------------------|-----------|----------------------------------------|
| bg-base            | #0e0d13   | fundo da pagina (leve tom arroxeado)   |
| bg-surface         | #17151f   | cards, listas, superficies             |
| bg-surface-alt     | #221f2d   | divisorias, badges neutros, trilhos    |
| border-subtle      | #2a2636   | bordas 0.5px discretas                 |

## Cores — texto

| Token         | Hex       | Uso                          |
|---------------|-----------|------------------------------|
| text-primary  | #f2f0f7   | titulos, valores principais  |
| text-body     | #e8e6ef   | texto comum                  |
| text-muted    | #8b8794   | labels, secundario           |
| text-faint    | #6f6b7a   | metadados, hints             |

## Cores — acento e semantica

| Token          | Hex       | Uso                                       |
|----------------|-----------|-------------------------------------------|
| accent (roxo)  | #7F77DD   | acao, botao primario, destaque, categoria |
| accent-deep    | #26215C   | fundo de icone/avatar roxo                |
| accent-soft    | #AFA9EC   | icone sobre fundo roxo                     |
| positivo       | #5DCAA5   | entrada/recebimento/pago                   |
| negativo       | #E0807A   | saida/gasto                               |
| alerta         | #BA7517   | pendente, atencao                         |

Status (badges): pago -> positivo; pendente -> alerta; manual -> neutro
(text-muted sobre bg-surface-alt); sugerido -> accent.

---

## Tipografia

- Fonte: **Inter** (limpa, moderna, facil de obter via Google Fonts).
  Alternativas equivalentes: Geist, Satoshi.
- Pesos: 400 (regular) e 500 (medium). Evitar 600/700.
- Escala: valor grande 28px; titulo 19px; corpo 14px; label 13px; meta 12px.
- Sentence case sempre. Nunca caixa alta decorativa.

## Forma e espaco

- Raio: cards 12-16px; elementos menores 8-10px; badges 5px.
- Bordas: 0.5px, discretas (border-subtle). Nunca borda grossa.
- Densidade: equilibrada — nem apertado nem vazio demais.
- Icone de status/categoria: quadrado arredondado 34px com icone Tabler dentro.

## Principios

- Cor com significado: roxo = acao/categoria; verde = entrada; coral = saida;
  ambar = pendente. Nao usar cor so por enfeite.
- Os badges de origem (manual/OF) e status (pendente/pago/sugerido) refletem
  diretamente as regras de negocio — o usuario bate o olho e entende.
- Minimalismo: remover o que nao informa. Espaco em branco e parte do design.
