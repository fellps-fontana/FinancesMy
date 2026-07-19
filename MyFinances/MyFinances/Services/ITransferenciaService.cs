using MyFinances.DTOs;
using MyFinances.Domain;

namespace MyFinances.Services;

public interface ITransferenciaService
{
    Task<(bool Sucesso, Transferencia? Transferencia, string? Erro)> RegistrarTransferenciaManualAsync(
        CriarTransferenciaRequest request);
}
