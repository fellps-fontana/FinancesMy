using MyFinances.Domain;

namespace MyFinances.Services;

public interface IAssinaturaCartaoService
{
    Task<AssinaturaCartao> CriarAsync(
        Guid contaId,
        string descricao,
        decimal valor,
        int diaReferencia,
        Guid? categoriaId);

    Task<AssinaturaCartao> EditarAsync(
        Guid id,
        string descricao,
        decimal valor,
        int diaReferencia,
        Guid? categoriaId);

    Task DesativarAsync(Guid id);

    Task ReativarAsync(Guid id);

    Task<AssinaturaCartao> ObterPorIdAsync(Guid id);

    Task<IEnumerable<AssinaturaCartao>> ListarAsync(Guid? contaId = null, bool? ativaFiltro = null);
}
