using MyFinances.Services;

namespace MyFinances.DTOs;

public class ProjecaoMesResponse
{
    public int Ano { get; set; }

    public int Mes { get; set; }

    public decimal TotalRecebidoNoMes { get; set; }

    public decimal TotalAReceberEsperadoNoMes { get; set; }

    public decimal TotalPagoNoMes { get; set; }

    public decimal TotalAPagarNoMes { get; set; }

    public decimal SaldoProjetado { get; set; }

    public static ProjecaoMesResponse FromResultado(ProjecaoMesResultado resultado)
    {
        return new()
        {
            Ano = resultado.Ano,
            Mes = resultado.Mes,
            TotalRecebidoNoMes = resultado.TotalRecebidoNoMes,
            TotalAReceberEsperadoNoMes = resultado.TotalAReceberEsperadoNoMes,
            TotalPagoNoMes = resultado.TotalPagoNoMes,
            TotalAPagarNoMes = resultado.TotalAPagarNoMes,
            SaldoProjetado = resultado.SaldoProjetado
        };
    }
}
