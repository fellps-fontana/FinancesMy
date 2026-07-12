using MyFinances.DTOs.Conta;
using MyFinances.Exceptions;
using MyFinances.Domain;
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

    public async Task<(bool Sucesso, Conta? Conta, string? Erro)> CriarContaAsync(CriarContaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return (false, null, "Nome da conta e obrigatorio");
        }

        TipoConta? tipo = ConverterTipoConta(request.Tipo);
        if (tipo == null)
        {
            return (false, null, $"Tipo de conta '{request.Tipo}' nao e valido");
        }

        if (tipo.Value == TipoConta.Cartao)
        {
            var (validoCartao, erroCartao) = ValidarCartao(request);
            if (!validoCartao)
            {
                return (false, null, erroCartao);
            }
        }

        if (tipo.Value == TipoConta.Investimento && request.SaldoManual == null)
        {
            return (false, null, "Saldo inicial e obrigatorio para contas de investimento");
        }

        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Origem = OrigemConta.Manual,
            Tipo = tipo.Value,
            SaldoManual = request.SaldoManual,
            DiaFechamento = request.DiaFechamento,
            DiaVencimento = request.DiaVencimento,
            Ativa = true
        };

        await _contaRepository.Adicionar(conta);
        await _contaRepository.Salvar();

        return (true, conta, null);
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

    public async Task<IEnumerable<Conta>> ListarContasPorTipo(TipoConta tipo)
    {
        var contas = await _contaRepository.ListarPorTipo(tipo);
        return contas.Where(c => c.Ativa);
    }

    public async Task<IEnumerable<Conta>> ListarContasInvestimento()
    {
        return await ListarContasPorTipo(TipoConta.Investimento);
    }

    public async Task<decimal> CalcularTotalInvestido()
    {
        var saldos = await ObterSaldosContasInvestimento();
        return saldos.Values.Sum();
    }

    public async Task<Dictionary<Guid, decimal>> ObterSaldosContasInvestimento()
    {
        var saldosComModo = await ObterSaldosComModoContasInvestimento();
        return saldosComModo.ToDictionary(kv => kv.Key, kv => kv.Value.saldo);
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

        if (conta.Tipo == TipoConta.Investimento)
        {
            await ValidarDesativacaoDeContaInvestimento(contaId);
        }

        conta.Ativa = false;
        await _contaRepository.Salvar();
    }

    private async Task ValidarDesativacaoDeContaInvestimento(Guid contaId)
    {
        var ativos = await _ativoRepository.ListarAtivosAtivosPorConta(contaId);

        if (ativos.Any())
        {
            throw new ContaComAtivosNaoPodeSerDesativadaException(contaId);
        }
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

    private static TipoConta? ConverterTipoConta(string tipo)
    {
        try
        {
            return TipoContaExtensions.FromStorageValue(tipo.ToUpperInvariant());
        }
        catch
        {
            return null;
        }
    }

    private static (bool Valido, string? Erro) ValidarCartao(CriarContaRequest request)
    {
        if (request.DiaFechamento == null)
        {
            return (false, "Dia de fechamento e obrigatorio para cartao de credito");
        }

        if (request.DiaVencimento == null)
        {
            return (false, "Dia de vencimento e obrigatorio para cartao de credito");
        }

        if (request.DiaFechamento < 1 || request.DiaFechamento > 31)
        {
            return (false, "Dia de fechamento deve estar entre 1 e 31");
        }

        if (request.DiaVencimento < 1 || request.DiaVencimento > 31)
        {
            return (false, "Dia de vencimento deve estar entre 1 e 31");
        }

        return (true, null);
    }
}
