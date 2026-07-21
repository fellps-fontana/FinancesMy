using MyFinances.Domain;

namespace MyFinances.Services;

public interface IContaFixaService
{
    Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> CriarAsync(
        Guid contaId, string descricao, decimal valor, int diaVencimento, Guid? categoriaId);

    Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> EditarAsync(
        Guid contaFixaId, decimal valor, int diaVencimento, Guid? categoriaId);

    Task<(bool Sucesso, string? Erro)> DesativarAsync(Guid contaFixaId);

    Task<(bool Sucesso, string? Erro)> ReativarAsync(Guid contaFixaId);

    Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> ObterPorId(Guid contaFixaId);

    Task<(bool Sucesso, IEnumerable<ContaFixa>? ContasFixas, string? Erro)> Listar(bool? ativaFiltro);

    Task<(bool Sucesso, int LancamentosGerados, string? Erro)> GerarLancamentosPendentes(
        Guid contaFixaId, DateOnly dataReferencia);
}
