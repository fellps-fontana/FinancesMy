using MyFinances.Domain;

namespace MyFinances.Services;

public interface IContaFixaService
{
    Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> CriarAsync(
        Guid contaId, string descricao, decimal valor, int diaVencimento, Guid? categoriaId,
        string? periodicidade = null, int? mesReferencia = null);

    Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> EditarAsync(
        Guid contaFixaId, decimal valor, int diaVencimento, Guid? categoriaId,
        string? periodicidade = null, int? mesReferencia = null);

    Task<(bool Sucesso, string? Erro)> DesativarAsync(Guid contaFixaId);

    Task<(bool Sucesso, string? Erro)> ReativarAsync(Guid contaFixaId);

    Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> ObterPorId(Guid contaFixaId);

    Task<(bool Sucesso, IEnumerable<ContaFixa>? ContasFixas, string? Erro)> Listar(bool? ativaFiltro);
}
