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
}
