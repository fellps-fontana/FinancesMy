namespace MyFinances.Exceptions;

public class PessoaObrigatoriaParaEmprestimoException : Exception
{
    public PessoaObrigatoriaParaEmprestimoException()
        : base("Campo 'pessoa' e obrigatorio para ContaReceber do tipo Emprestimo.")
    {
    }
}
