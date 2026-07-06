using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Models;

namespace MyFinances.Services;

public class ContaService
{
    private readonly AppDbContext _context;

    public ContaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Sucesso, Conta? Conta, string? Erro)> CriarContaAsync(CriarContaRequest request)
    {
        var validacao = ValidarCriacaoConta(request);
        if (!validacao.Valido)
        {
            return (false, null, validacao.Erro);
        }

        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Origem = OrigemConstants.Manual,
            Tipo = request.Tipo,
            DiaFechamento = request.DiaFechamento,
            DiaVencimento = request.DiaVencimento,
            SaldoManual = request.Tipo == TipoContaConstants.Cartao ? null : request.SaldoManual,
            Ativa = true
        };

        _context.Contas.Add(conta);
        await _context.SaveChangesAsync();

        return (true, conta, null);
    }

    private static (bool Valido, string? Erro) ValidarCriacaoConta(CriarContaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return (false, "Nome da conta é obrigatório");
        }

        if (string.IsNullOrWhiteSpace(request.Tipo))
        {
            return (false, "Tipo da conta é obrigatório");
        }

        if (!TipoContaConstants.EValido(request.Tipo))
        {
            return (false, $"Tipo '{request.Tipo}' inválido. Valores aceitos: {TipoContaConstants.Banco}, {TipoContaConstants.Cartao}, {TipoContaConstants.Investimento}");
        }

        if (request.Tipo == TipoContaConstants.Cartao)
        {
            if (!request.DiaFechamento.HasValue)
            {
                return (false, "dia_fechamento é obrigatório para conta tipo CARTAO");
            }

            if (!EhDiaValidoDeMes(request.DiaFechamento.Value))
            {
                return (false, $"dia_fechamento deve estar entre 1 e 31, recebido: {request.DiaFechamento}");
            }

            if (!request.DiaVencimento.HasValue)
            {
                return (false, "dia_vencimento é obrigatório para conta tipo CARTAO");
            }

            if (!EhDiaValidoDeMes(request.DiaVencimento.Value))
            {
                return (false, $"dia_vencimento deve estar entre 1 e 31, recebido: {request.DiaVencimento}");
            }

            if (request.SaldoManual.HasValue)
            {
                return (false, "saldo_manual não deve ser informado para conta tipo CARTAO, pois o saldo é sempre calculado");
            }
        }

        return (true, null);
    }

    private static bool EhDiaValidoDeMes(int dia)
    {
        return dia >= 1 && dia <= 31;
    }
}
