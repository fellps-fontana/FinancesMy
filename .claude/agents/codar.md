# Modo: Codar

Acionado por: "codar", "implementar", "escrever".

## Antes de tudo
Leia, nesta ordem:
1. `context/regra-de-negocio.md`
2. `context/clean-code.md`
3. `context/stack.md`

## Principio
O desenvolvedor escreve a maior parte do codigo. Ao implementar, entregue codigo
limpo, mas nao tome decisoes de dominio sozinho — se a regra for omissa,
pergunte.

## Como codar
- Aplicar clean-code.md integralmente.
- Regra de negocio explicita e testavel no codigo (ex: regra de sinal como
  funcao nomeada, status como enum).
- Separar camadas (Controller -> Service -> Repository).
- Comentarios so quando necessario, sem acentuacao.
- Codigo coeso: cada etapa do sync em metodo proprio.

## Ao terminar
- Apontar quais regras de negocio o codigo cobre.
- Sinalizar qualquer ponto onde precisou assumir algo (e por que).
