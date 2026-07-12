using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IFaturaRepository
{
    Task Adicionar(Fatura fatura);
    Task<Fatura?> ObterPorId(Guid id);
    Task<IEnumerable<Fatura>> ListarPorConta(Guid contaId);
    Task<Fatura?> ObterFaturaAbertaPorConta(Guid contaId);
    Task Atualizar(Fatura fatura);
    Task Salvar();
}
