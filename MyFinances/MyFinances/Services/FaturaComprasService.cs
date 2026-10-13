using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

// Lancamentos que compoem uma fatura (compras, parcelas, antecipadas e estornos),
// ordenados por data. Leitura pura: nao altera saldo nem status.
public class FaturaComprasService : IFaturaComprasService
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;

    public FaturaComprasService(IFaturaRepository faturaRepository, ILancamentoRepository lancamentoRepository)
    {
        _faturaRepository = faturaRepository;
        _lancamentoRepository = lancamentoRepository;
    }

    // Devolve null se a fatura nao existe ou nao pertence a contaId.
    public async Task<IReadOnlyList<Lancamento>?> ListarComprasAsync(Guid contaId, Guid faturaId)
    {
        var fatura = await _faturaRepository.ObterPorId(faturaId);
        if (fatura == null || fatura.ContaId != contaId)
        {
            return null;
        }

        var lancamentos = await _lancamentoRepository.ListarPorFatura(faturaId);

        return lancamentos
            .OrderBy(l => l.Data)
            .ToList()
            .AsReadOnly();
    }
}
