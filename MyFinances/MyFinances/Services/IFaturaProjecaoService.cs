namespace MyFinances.Services;

public record FaturaProjecaoMes(decimal TotalPago, decimal TotalNaoPago);

public interface IFaturaProjecaoService
{
    Task<FaturaProjecaoMes> CalcularProjecaoCartaoDoMes(int ano, int mes);
}
