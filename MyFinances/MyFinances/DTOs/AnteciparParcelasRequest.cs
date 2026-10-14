namespace MyFinances.DTOs;

public class AnteciparParcelasRequest
{
    // Quantas parcelas antecipar (>= 1). O usuario escolhe QUANTAS, nunca QUAIS (item 12).
    // A data da antecipacao e sempre a data de hoje no servidor (cliente nao escolhe).
    public required int Quantidade { get; set; }
}
