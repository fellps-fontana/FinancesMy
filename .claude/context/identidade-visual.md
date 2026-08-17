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

## Cores — tema claro

| Token              | Hex       | Uso                                    |
|--------------------|-----------|-----------------------------------------|
| bg-base            | #F5F3FA   | fundo da pagina (leve tom lavanda)     |
| bg-surface         | #FFFFFF   | cards, listas, superficies             |
| bg-surface-alt     | #EAE6F3   | divisorias, badges neutros, trilhos    |
| border-subtle      | #DAD4E8   | bordas 0.5px discretas                 |

## Cores — texto

| Token         | Hex       | Uso                          |
|---------------|-----------|------------------------------|
| text-primary  | #f2f0f7   | titulos, valores principais  |
| text-body     | #e8e6ef   | texto comum                  |
| text-muted    | #8b8794   | labels, secundario           |
| text-faint    | #6f6b7a   | metadados, hints             |

## Cores — texto (tema claro)

| Token         | Hex       | Uso                          |
|---------------|-----------|-------------------------------|
| text-primary  | #16141C   | titulos, valores principais  |
| text-body     | #322F3D   | texto comum                  |
| text-muted    | #6B6775   | labels, secundario           |
| text-faint    | #8A8696   | metadados, hints              |

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

Cores de acento e semantica (accent, accent-deep, accent-soft, positivo,
negativo, alerta) NAO mudam entre temas — sao as mesmas hex acima em ambos
os temas. Cor com significado e uma decisao de dominio, nao de tema.

ATENCAO (risco de contraste): os hex de positivo (#5DCAA5), negativo
(#E0807A) e alerta (#BA7517) foram calibrados para contraste contra
bg-base ESCURO. Contra bg-surface branco do tema claro, o contraste como
texto corrido cai abaixo de WCAG aceitavel (~1.8:1 a 2.6:1). Uso deve ser
sempre em badge/chip com fundo (nunca como texto nu direto sobre fundo
claro).

## Mecanismo de alternancia (tema claro/escuro)

- Classe no elemento raiz (`<html>`): ausencia de classe (ou `.light`
  explicito) = tema claro; `.dark` = tema escuro. `:root` carrega os
  valores do tema claro como base; `.dark` sobrescreve com os valores de
  "Cores — base (dark)".
- Default: preferencia do SO via `window.matchMedia('(prefers-color-scheme:
  dark)')`, aplicado ANTES do primeiro paint (script inline sincrono no
  `<head>` do `index.html`, nao em useEffect — useEffect roda depois do
  paint e causa flash de tema errado).
- Persistencia: escolha manual do usuario (toggle) sobrescreve a
  preferencia do SO e vai pra localStorage (chave `theme`, valores
  `"light" | "dark" | "system"`). Em toda carga, o script inline le
  localStorage; se ausente ou "system", cai pro matchMedia.
- Estado da UI (toggle ligado/desligado, valor atual): Context API no
  shell da app (`ThemeContext`, mesmo padrao ja usado em
  `features/auth/AuthContext.tsx`).

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
