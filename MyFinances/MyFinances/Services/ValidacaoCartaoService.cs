using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Models;

namespace MyFinances.Services;

/// <summary>
/// Servico compartilhado de validacao para operacoes em cartao de credito.
/// Centraliza validacoes comuns entre compra, estorno e futuras operacoes.
/// Tambem devolve a Conta ja carregada pra evitar busca repetida no fluxo.
/// </summary>
public class ValidacaoCartaoService
{
    private readonly AppDbContext _context;

    public ValidacaoCartaoService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Valida se a conta existe e e do tipo CARTAO. Retorna Conta ja carregada.
    /// </summary>
    public async Task<(bool Valido, Conta? Conta, string? Erro)> ValidarContaCartaoAsync(Guid contaId)
    {
        var conta = await _context.Contas
            .FirstOrDefaultAsync(c => c.Id == contaId);

        if (conta == null)
        {
            return (false, null, "Conta nao encontrada");
        }

        if (conta.Tipo != TipoContaConstants.Cartao)
        {
            return (false, null, "Conta nao e do tipo CARTAO");
        }

        return (true, conta, null);
    }

    /// <summary>
    /// Valida operacao em cartao e retorna a Conta ja carregada.
    /// </summary>
    public async Task<(bool Valido, Conta? Conta, string? Erro)> ValidarOperacaoCartaoAsync(
        Guid contaId,
        string descricao,
        decimal valor)
    {
        var (validoCartao, conta, erroCartao) = await ValidarContaCartaoAsync(contaId);

        if (!validoCartao)
        {
            return (false, null, erroCartao);
        }

        if (string.IsNullOrWhiteSpace(descricao))
        {
            return (false, null, "Descricao e obrigatoria");
        }

        if (valor <= 0)
        {
            return (false, null, "Valor deve ser positivo");
        }

        return (true, conta, null);
    }
}
