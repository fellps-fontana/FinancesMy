using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class ContaFixaService : IContaFixaService
{
    private readonly IContaFixaRepository _contaFixaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;

    public ContaFixaService(
        IContaFixaRepository contaFixaRepository,
        IContaRepository contaRepository,
        ILancamentoRepository lancamentoRepository)
    {
        _contaFixaRepository = contaFixaRepository;
        _contaRepository = contaRepository;
        _lancamentoRepository = lancamentoRepository;
    }

    public Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> CriarAsync(
        Guid contaId, string descricao, decimal valor, int diaVencimento, Guid? categoriaId)
        => throw new NotImplementedException();

    public Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> EditarAsync(
        Guid contaFixaId, decimal valor, int diaVencimento, Guid? categoriaId)
        => throw new NotImplementedException();

    public Task<(bool Sucesso, string? Erro)> DesativarAsync(Guid contaFixaId)
        => throw new NotImplementedException();

    public Task<(bool Sucesso, string? Erro)> ReativarAsync(Guid contaFixaId)
        => throw new NotImplementedException();

    public Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> ObterPorId(Guid contaFixaId)
        => throw new NotImplementedException();

    public Task<(bool Sucesso, IEnumerable<ContaFixa>? ContasFixas, string? Erro)> Listar(bool? ativaFiltro)
        => throw new NotImplementedException();

    public Task<(bool Sucesso, int LancamentosGerados, string? Erro)> GerarLancamentosPendentes(
        Guid contaFixaId, DateOnly dataReferencia)
        => throw new NotImplementedException();
}
