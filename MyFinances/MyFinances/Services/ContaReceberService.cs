using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class ContaReceberService : IContaReceberService
{
    private readonly IContaReceberRepository _contaReceberRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IContaRepository _contaRepository;

    public ContaReceberService(
        IContaReceberRepository contaReceberRepository,
        ITransferenciaRepository transferenciaRepository,
        ILancamentoRepository lancamentoRepository,
        IContaRepository contaRepository)
    {
        _contaReceberRepository = contaReceberRepository;
        _transferenciaRepository = transferenciaRepository;
        _lancamentoRepository = lancamentoRepository;
        _contaRepository = contaRepository;
    }

    public Task<ContaReceber> RegistrarRecebivel(
        string descricao,
        decimal valorTotal,
        DateOnly dataRegistro,
        DateOnly? dataPrevista,
        Guid? categoriaId)
    {
        throw new NotImplementedException();
    }

    public Task<ContaReceber> RegistrarEmprestimo(
        string descricao,
        string pessoa,
        decimal valorTotal,
        Guid contaOrigemId,
        DateOnly dataRegistro,
        DateOnly? dataPrevista,
        Guid? categoriaId)
    {
        throw new NotImplementedException();
    }

    public Task<Lancamento> RegistrarRecebimento(
        Guid contaReceberId,
        Guid contaDestinoId,
        decimal valor,
        DateOnly data,
        Guid? categoriaId)
    {
        throw new NotImplementedException();
    }

    public Task<ContaReceber> ObterPorId(Guid contaReceberId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ContaReceber>> Listar(StatusContaReceber? statusFiltro = null)
    {
        throw new NotImplementedException();
    }
}
