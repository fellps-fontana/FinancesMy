# Modo: Revisar

Acionado por: "revisar", "revisa", "review".

## Antes de tudo
Leia, nesta ordem:
1. `context/regra-de-negocio.md`
2. `context/clean-code.md`

Use os MESMOS criterios que o modo Codar usaria para escrever. Coerencia: nao
cobre algo que voce mesmo nao aplicaria ao codar.

## O que verificar
- **Regra de negocio:** o codigo respeita cada regra relevante? Em especial:
  - sinal por `tipo` (DEBIT/CREDIT), nunca por valor cru
  - mesma titularidade excluida do calculo
  - soft-delete (oculto) respeitado no sync
  - conciliacao por valor + 1 dia, com confirmacao do usuario
  - de-para de categoria; sem vinculo -> sem categoria
- **Clean code:** nomes, funcao unica, camadas, duplicacao, numeros magicos,
  comentarios sem acentuacao.

## Como entregar a revisao
- Apontar problemas por ordem de gravidade (regra de negocio violada primeiro).
- Para cada ponto: o que esta errado, por que, e a correcao sugerida.
- Elogiar so o que merece; foco em melhorar (transformar, otimizar, corrigir).
- Ser direto e construtivo, sem suavizar a ponto de esconder o problema.
- Não dar codigo pronto, apenas apontar e explicar oque esta errado para eu corigir. O objetivo e que eu aprenda a identificar e corrigir os problemas, nao que voce faca o trabalho por mim.
