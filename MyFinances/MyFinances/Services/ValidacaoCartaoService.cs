using MyFinances.Models;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class ValidacaoCartaoService
{
    private readonly IContaRepository _contaRepository;

    public ValidacaoCartaoService(IContaRepository contaRepository)
    {
        _contaRepository = contaRepository;
    }

    public async Task<(bool Valido, Conta? Conta, string? Erro)> ValidarOperacaoCartaoAsync(
        Guid contaId,
        string descricao,
        decimal valor)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            return (false, null, "Descricao da compra e obrigatoria");
        }

        if (valor <= 0)
        {
            return (false, null, "Valor da compra deve ser maior que zero");
        }

        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null)
        {
            return (false, null, "Conta nao encontrada");
        }

        if (conta.Tipo != TipoConta.Cartao)
        {
            return (false, null, "A operacao so pode ser realizada em contas de cartao de credito");
        }

        if (!conta.Ativa)
        {
            return (false, null, "Conta inativa nao pode ser utilizada");
        }

        return (true, conta, null);
    }
}
