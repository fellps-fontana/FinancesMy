namespace MyFinances.Exceptions;

public class CampoObrigatorioException : Exception
{
    public string NomeCampo { get; }

    public CampoObrigatorioException(string nomeCampo)
        : base($"{nomeCampo} e obrigatorio e nao pode estar vazio ou conter apenas espacos.")
    {
        NomeCampo = nomeCampo;
    }
}
