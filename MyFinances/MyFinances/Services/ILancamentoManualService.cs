using MyFinances.DTOs;

namespace MyFinances.Services;

public interface ILancamentoManualService
{
    Task<(bool Sucesso, LancamentoResponseDto? Lancamento, string? Erro)> CriarAsync(
        Guid contaId,
        CriarLancamentoRequest request);

    Task<(bool Sucesso, LancamentoResponseDto? Lancamento, string? Erro)> EditarAsync(
        Guid contaId,
        Guid lancamentoId,
        EditarLancamentoRequest request);

    Task<(bool Sucesso, string? Erro)> MarcarComoPagoAsync(
        Guid contaId,
        Guid lancamentoId);

    Task<(bool Sucesso, string? Erro)> RemoverAsync(
        Guid contaId,
        Guid lancamentoId);
}
