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

    // Exclusao definitiva (regra-de-negocio.md item 6). Ordem obrigatoria:
    // 1) RemoverOcorrenciasNaoConsumadasAsync (banco: Pendente; cartao: Fatura != Paga);
    // 2) DesvincularOcorrenciasConsumadasAsync (ContaFixaId = null; valor/data/categoria intactos);
    // 3) remove o molde ContaFixa.
    // Inexistente -> (false, "Conta fixa nao encontrada"). Aceita molde ja desativada.
    Task<(bool Sucesso, string? Erro)> ApagarAsync(Guid contaFixaId);

    Task<(bool Sucesso, string? Erro)> ReativarAsync(Guid contaFixaId);

    Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> ObterPorId(Guid contaFixaId);

    Task<(bool Sucesso, IEnumerable<ContaFixa>? ContasFixas, string? Erro)> Listar(bool? ativaFiltro);
}
