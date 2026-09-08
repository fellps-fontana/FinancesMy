using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IAssinaturaCartaoRepository
{
    Task Adicionar(AssinaturaCartao assinatura);
    Task<AssinaturaCartao?> ObterPorId(Guid id);
    Task<IEnumerable<AssinaturaCartao>> Listar(Guid? contaId = null, bool? ativaFiltro = null);
    Task<IEnumerable<AssinaturaCartao>> ListarAtivasPorConta(Guid contaId);
    Task Atualizar(AssinaturaCartao assinatura);

    // Idempotencia (item 16): existe compra gerada para esta assinatura neste mes/ano de referencia?
    Task<bool> ExisteCompraGerada(Guid assinaturaCartaoId, int ano, int mes);

    Task Salvar();
}
