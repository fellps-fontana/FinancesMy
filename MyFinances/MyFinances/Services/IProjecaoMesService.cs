namespace MyFinances.Services;

public record ProjecaoMesResultado(
    int Ano,
    int Mes,
    decimal TotalRecebidoNoMes,
    decimal TotalAReceberEsperadoNoMes,
    decimal TotalPagoNoMes,
    decimal TotalAPagarNoMes,
    decimal SaldoProjetado);

public interface IProjecaoMesService
{
    Task<ProjecaoMesResultado> CalcularProjecaoDoMes(int ano, int mes);
}
