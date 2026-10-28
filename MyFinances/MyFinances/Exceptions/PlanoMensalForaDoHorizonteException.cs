namespace MyFinances.Exceptions;

public class PlanoMensalForaDoHorizonteException : Exception
{
    public int Ano { get; }
    public int Mes { get; }
    public int MesesFuturosPermitidos { get; }

    public PlanoMensalForaDoHorizonteException(int ano, int mes, int mesesFuturosPermitidos)
        : base($"O plano de {mes:00}/{ano} esta alem do horizonte de {mesesFuturosPermitidos} meses a frente do mes corrente.")
    {
        Ano = ano;
        Mes = mes;
        MesesFuturosPermitidos = mesesFuturosPermitidos;
    }
}
