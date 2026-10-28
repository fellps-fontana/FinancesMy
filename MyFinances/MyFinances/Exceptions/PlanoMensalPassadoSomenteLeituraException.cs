namespace MyFinances.Exceptions;

public class PlanoMensalPassadoSomenteLeituraException : Exception
{
    public int Ano { get; }
    public int Mes { get; }

    public PlanoMensalPassadoSomenteLeituraException(int ano, int mes)
        : base($"O plano de {mes:00}/{ano} e de mes passado e nao pode ser editado.")
    {
        Ano = ano;
        Mes = mes;
    }
}
