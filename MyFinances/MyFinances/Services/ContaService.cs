using MyFinances.Exceptions;
using MyFinances.Models;
using MyFinances.Repositories;
using Microsoft.Extensions.Logging;

namespace MyFinances.Services;

public class ContaService : IContaService
{
    private readonly IContaRepository _contaRepository;
    private readonly IAtivoRepository _ativoRepository;
    private readonly ILogger<ContaService> _logger;

    public ContaService(IContaRepository contaRepository, IAtivoRepository ativoRepository, ILogger<ContaService> logger)
    {
        _contaRepository = contaRepository;
        _ativoRepository = ativoRepository;
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
        var saldosCalculados = await ObterSaldosContasInvestimento();
        decimal total = 0;

        foreach (var conta in contasInvestimento)
        {
            decimal saldo = ObterSaldoContaInvestimento(conta, saldosCalculados);
            total += saldo;
        }

        return total;
    }

    public async Task<Dictionary<Guid, decimal>> ObterSaldosContasInvestimento()
    {
        var contasInvestimento = await ListarContasInvestimento();
        var contaIds = contasInvestimento.Select(c => c.Id).ToList();

        if (!contaIds.Any())
        {
            return new Dictionary<Guid, decimal>();
        }

        var saldosCalculados = await _ativoRepository.SomarValorAtivosPorConta(contaIds);
        var modsCarteira = await VerificarContasEmModoCarteira(contaIds);
        var resultado = new Dictionary<Guid, decimal>();

        foreach (var conta in contasInvestimento)
        {
            if (modsCarteira[conta.Id])
            {
                resultado[conta.Id] = saldosCalculados.ContainsKey(conta.Id) ? saldosCalculados[conta.Id] : 0m;
            }
            else
            {
                resultado[conta.Id] = conta.SaldoManual ?? 0m;
            }
        }

        return resultado;
    }

    public async Task<Dictionary<Guid, bool>> VerificarContasEmModoCarteira(IEnumerable<Guid> contaIds)
    {
        return await _ativoRepository.VerificarContasComAtivos(contaIds);
    }

    public async Task<Dictionary<Guid, (decimal saldo, bool estaEmModoCarteira)>> ObterSaldosComModoContasInvestimento()
    {
        var contasInvestimento = await ListarContasInvestimento();
        var contaIds = contasInvestimento.Select(c => c.Id).ToList();

        if (!contaIds.Any())
        {
            return new Dictionary<Guid, (decimal, bool)>();
        }

        var saldosCalculados = await _ativoRepository.SomarValorAtivosPorConta(contaIds);
        var modsCarteira = await VerificarContasEmModoCarteira(contaIds);
        var resultado = new Dictionary<Guid, (decimal, bool)>();

        foreach (var conta in contasInvestimento)
        {
            var estaEmModoCarteira = modsCarteira[conta.Id];
            decimal saldo = estaEmModoCarteira
                ? (saldosCalculados.ContainsKey(conta.Id) ? saldosCalculados[conta.Id] : 0m)
                : (conta.SaldoManual ?? 0m);

            resultado[conta.Id] = (saldo, estaEmModoCarteira);
        }

        return resultado;
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

    private decimal ObterSaldoContaInvestimento(Conta conta, Dictionary<Guid, decimal> saldosCalculados)
    {
        if (saldosCalculados.ContainsKey(conta.Id))
        {
            return saldosCalculados[conta.Id];
        }

        if (conta.SaldoManual == null)
        {
            _logger.LogWarning("Conta de investimento {ContaId} ({ContaNome}) nao esta em modo carteira e possui SaldoManual nulo. Tratando como zero.", conta.Id, conta.Nome);
        }

        return conta.SaldoManual ?? 0m;
    }
}
