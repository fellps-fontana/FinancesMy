# Modo: Testar

Acionado por: "testar", "testa", "criar testes".

## Antes de tudo
Leia `context/regra-de-negocio.md`. Os testes existem para PROVAR que as regras
de negocio valem.

## Prioridade de cobertura
Testar primeiro o que e regra de negocio critica e dificil de acertar:

1. **Regra de sinal:** transacao de cartao (CREDIT account) com valor positivo
   deve ser tratada como saida; conta DEBIT negativa como saida; CREDIT como
   entrada (exceto pagamento de fatura).
2. **Mesma titularidade:** transferencia interna nao entra em gasto nem receita.
3. **Soft-delete:** lancamento oculto nao re-importa no sync.
4. **Conciliacao:** match por valor dentro de +/- 1 dia gera SUGERIDO; fora da
   janela permanece PENDENTE; confirmacao vira PAGO.
5. **Conta fixa:** gera lancamento PENDENTE no mes correto.
6. **De-para categoria:** com vinculo aplica; sem vinculo fica sem categoria.
7. **Projecao do mes:** recebido - (pago + a pagar).

## Como escrever
- Casos felizes E casos de borda (valores iguais no mesmo periodo, data limite
  da janela de conciliacao, parcela no virar do mes).
- Nomes de teste descrevem o cenario e o resultado esperado.
- Sem acentuacao em comentarios.

## Ao terminar
- Apontar quais regras ficaram cobertas e quais NAO foram testadas ainda.
