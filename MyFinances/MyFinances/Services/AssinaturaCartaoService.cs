using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class AssinaturaCartaoService : IAssinaturaCartaoService
{
    private readonly IAssinaturaCartaoRepository _assinaturaCartaoRepository;
    private readonly IContaRepository _contaRepository;

    public AssinaturaCartaoService(
        IAssinaturaCartaoRepository assinaturaCartaoRepository,
        IContaRepository contaRepository)
    {
        _assinaturaCartaoRepository = assinaturaCartaoRepository;
        _contaRepository = contaRepository;
    }

    public Task<AssinaturaCartao> CriarAsync(
        Guid contaId,
        string descricao,
        decimal valor,
        int diaReferencia,
        Guid? categoriaId)
    {
        // Esqueleto compilavel (Killua - TASK-166). Logica real sera implementada por Levi via TDD com Mike.
        throw new NotImplementedException();
    }

    public Task<AssinaturaCartao> EditarAsync(
        Guid id,
        string descricao,
        decimal valor,
        int diaReferencia,
        Guid? categoriaId)
    {
        // Esqueleto compilavel (Killua - TASK-166). Logica real sera implementada por Levi via TDD com Mike.
        throw new NotImplementedException();
    }

    public Task DesativarAsync(Guid id)
    {
        // Esqueleto compilavel (Killua - TASK-166). Logica real sera implementada por Levi via TDD com Mike.
        throw new NotImplementedException();
    }

    public Task ReativarAsync(Guid id)
    {
        // Esqueleto compilavel (Killua - TASK-166). Logica real sera implementada por Levi via TDD com Mike.
        throw new NotImplementedException();
    }

    public Task<AssinaturaCartao> ObterPorIdAsync(Guid id)
    {
        // Esqueleto compilavel (Killua - TASK-166). Logica real sera implementada por Levi via TDD com Mike.
        throw new NotImplementedException();
    }

    public Task<IEnumerable<AssinaturaCartao>> ListarAsync(Guid? contaId = null, bool? ativaFiltro = null)
    {
        // Esqueleto compilavel (Killua - TASK-166). Logica real sera implementada por Levi via TDD com Mike.
        throw new NotImplementedException();
    }
}
