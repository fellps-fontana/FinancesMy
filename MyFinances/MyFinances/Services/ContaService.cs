using MyFinances.Exceptions;
using MyFinances.Models;
using MyFinances.Repositories;
using Microsoft.Extensions.Logging;

namespace MyFinances.Services;

public class ContaService : IContaService
{
    private readonly IContaRepository _contaRepository;
    private readonly ILogger<ContaService> _logger;

    public ContaService(IContaRepository contaRepository, ILogger<ContaService> logger)
    {
        _contaRepository = contaRepository;
        _logger = logger;
    }

    public async Task<Conta> CriarContaInvestimento(string nome, decimal saldoInicial)
    {
        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Origem = OrigemConta.Manual,
            Tipo = TipoConta.Investimento,
            SaldoManual = saldoInicial,
            Ativa = true
        };

        await _contaRepository.Adicionar(conta);
        await _contaRepository.Salvar();

        return conta;
    }

    public async Task<IEnumerable<Conta>> ListarContasInvestimento()
    {
        var contas = await _contaRepository.ListarPorTipo(TipoConta.Investimento);
        return contas.Where(c => c.Ativa);
    }

    public async Task<decimal> CalcularTotalInvestido()
    {
        var contasInvestimento = await ListarContasInvestimento();
        decimal total = 0;

        foreach (var conta in contasInvestimento)
        {
            if (conta.SaldoManual == null)
            {
                _logger.LogWarning("Conta de investimento {ContaId} ({ContaNome}) possui SaldoManual nulo. Tratando como zero.", conta.Id, conta.Nome);
            }

            total += conta.SaldoManual ?? 0;
        }

        return total;
    }

    public async Task AtualizarSaldoManual(Guid contaId, decimal novoSaldo)
    {
        var conta = await ObterContaOuFalhar(contaId);

        if (conta.Origem != OrigemConta.Manual)
        {
            throw new SaldoManualNaoPermitidoException(contaId, conta.Origem);
        }

        conta.SaldoManual = novoSaldo;
        await _contaRepository.Salvar();
    }

    public async Task DesativarConta(Guid contaId)
    {
        var conta = await ObterContaOuFalhar(contaId);

        conta.Ativa = false;
        await _contaRepository.Salvar();
    }

    private async Task<Conta> ObterContaOuFalhar(Guid contaId)
    {
        var conta = await _contaRepository.ObterPorId(contaId);

        if (conta == null)
        {
            throw new ContaNaoEncontradaException(contaId);
        }

        return conta;
    }
}
