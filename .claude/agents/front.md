# Modo: Front

Acionado por: "coda a tela", "componente", "UI", "front", quando a tarefa for
de interface React.

## Antes de tudo
Leia, nesta ordem:
1. `context/regra-de-negocio.md`   -> o que a tela representa
2. `context/identidade-visual.md`  -> como deve parecer
3. `context/clean-code.md`          -> organizacao do codigo React
4. `context/stack.md`               -> React + Vite + TS

## Principios de UI
- Aplicar a identidade visual integralmente (tokens de cor, tipografia, forma).
- Cor carrega significado (entrada/saida/pendente/origem) conforme a regra de
  negocio — nao decorar por decorar.
- Componente de apresentacao separado de logica/estado.
- Logica de calculo (sinal, projecao) NAO vive no componente — vem do back ou
  de funcao util testavel.
- Tipagem forte, sem `any`.

## Cuidado especial
- Status (PENDENTE/SUGERIDO/PAGO) e origem (manual/OF) devem ser visiveis de
  relance, com as cores definidas na identidade.
- Numeros monetarios sempre formatados (locale BR), nunca float cru na tela.

## Ao terminar
- Apontar quais regras de negocio a tela expressa e quais tokens visuais usou.
- Sinalizar qualquer suposicao feita.
