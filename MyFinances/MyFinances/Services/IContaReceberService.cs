using MyFinances.Domain;

namespace MyFinances.Services;

public interface IContaReceberService
{
    Task<ContaReceber> RegistrarRecebivel(
        string descricao,
        decimal valorTotal,
        DateOnly dataRegistro,
        DateOnly? dataPrevista,
        Guid? categoriaId);

    Task<ContaReceber> RegistrarEmprestimo(
        string descricao,
        string pessoa,
        decimal valorTotal,
        Guid contaOrigemId,
        DateOnly dataRegistro,
        DateOnly? dataPrevista,
        Guid? categoriaId);

    Task<Lancamento> RegistrarRecebimento(
        Guid contaReceberId,
        Guid contaDestinoId,
        decimal valor,
        DateOnly data,
        Guid? categoriaId);

    Task<ContaReceber> ObterPorId(Guid contaReceberId);

    Task<IEnumerable<ContaReceber>> Listar(StatusContaReceber? statusFiltro = null);

    Task<decimal> CalcularTotalAReceberEsperadoNoMes(int ano, int mes);

    Task Excluir(Guid contaReceberId);

    // regra-de-negocio.md itens 9, 13 e 15.
    // Lanca: ContaReceberNaoEncontradaException, ContaReceberRecorrenteNaoEditavelException,
    // CampoObrigatorioException (descricao), ValorInvalidoException (valor_total <= 0),
    // PessoaObrigatoriaParaEmprestimoException, ValorTotalNaoEditavelException.
    Task<ContaReceber> EditarAsync(
        Guid id,
        string descricao,
        string? pessoa,
        DateOnly? dataPrevista,
        decimal? valorTotal);
}

