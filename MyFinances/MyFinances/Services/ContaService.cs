using MyFinances.DTOs.Conta;
using MyFinances.Exceptions;
using MyFinances.Domain;
using MyFinances.Repositories;
using Microsoft.Extensions.Logging;

namespace MyFinances.Services;

public class ContaService : IContaService
{
    private const string CampoNome = "Nome";
    private const string NomeCampoDiaFechamento = "DiaFechamento";
    private const string NomeCampoDiaVencimento = "DiaVencimento";
    private const int DiaMinimoCiclo = 1;
    private const int DiaMaximoCiclo = 31;

    private readonly IContaRepository _contaRepository;
    private readonly ILogger<ContaService> _logger;
    private readonly IAtivoRepository _ativoRepository;

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

        SubtipoConta? subtipo = null;
        if (tipo.Value == TipoConta.Banco)
        {
            subtipo = ConverterSubtipoConta(request.Subtipo);
            if (request.Subtipo != null && subtipo == null)
            {
                return (false, null, $"Subtipo '{request.Subtipo}' nao e valido");
            }
        }

        var conta = new Conta
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Origem = OrigemConta.Manual,
            Tipo = tipo.Value,
            Subtipo = subtipo,
            Icone = request.Icone,
            Cor = request.Cor,
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
        var contasInvestimento = await ListarContasInvestimento();
        var ativosAtivos = await _ativoRepository.ListarAtivas();

        return ContaInvestimentoSaldoCalculator.CalcularTotalInvestido(contasInvestimento, ativosAtivos);
    }

    public async Task<(decimal Saldo, bool Calculado)> ObterSaldo(Conta conta)
    {
        var ativosDaConta = (await _ativoRepository.ListarAtivosPorConta(conta.Id)).ToList();

        var calculado = ContaInvestimentoSaldoCalculator.PossuiAtivoAtivoVinculado(ativosDaConta);
        var saldo = ContaInvestimentoSaldoCalculator.CalcularSaldo(conta, ativosDaConta);

        return (saldo, calculado);
    }

    public async Task AtualizarSaldoManual(Guid contaId, decimal novoSaldo)
    {
        var conta = await ObterContaOuFalhar(contaId);

        if (conta.Origem != OrigemConta.Manual)
        {
            throw new SaldoManualNaoPermitidoException(contaId, conta.Origem);
        }

        if (await _ativoRepository.ExisteAtivoAtivoVinculado(contaId))
        {
            throw new SaldoCalculadoNaoEditavelException(contaId);
        }

        conta.SaldoManual = novoSaldo;
        await _contaRepository.Salvar();
    }

    public async Task DesativarConta(Guid contaId)
    {
        var conta = await ObterContaOuFalhar(contaId);

        if (await _ativoRepository.ExisteAtivoAtivoVinculado(contaId))
        {
            throw new ContaPossuiAtivoAtivoException(contaId);
        }

        conta.Ativa = false;
        await _contaRepository.Salvar();
    }

    public async Task<Conta> EditarConta(Guid id, EditarContaRequest request)
    {
        var conta = await ObterContaOuFalhar(id);

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            throw new CampoObrigatorioException(CampoNome);
        }

        var subtipo = ResolverSubtipoEdicao(conta.Tipo, request.Subtipo);
        var ehCartao = conta.Tipo == TipoConta.Cartao;

        if (ehCartao)
        {
            ValidarDiasCartaoEdicao(request.DiaFechamento, request.DiaVencimento);
        }

        conta.Nome = request.Nome;
        conta.Icone = request.Icone;
        conta.Cor = request.Cor;
        conta.Subtipo = subtipo;

        if (ehCartao)
        {
            conta.DiaFechamento = request.DiaFechamento;
            conta.DiaVencimento = request.DiaVencimento;
        }

        await _contaRepository.Salvar();

        return conta;
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

    private static SubtipoConta? ConverterSubtipoConta(string? subtipo)
    {
        if (string.IsNullOrWhiteSpace(subtipo))
        {
            return null;
        }

        try
        {
            return SubtipoContaExtensions.FromStorageValue(subtipo.ToUpperInvariant());
        }
        catch
        {
            return null;
        }
    }

    // Item 10.1/10.2: subtipo so vale para Banco; nos demais tipos e ignorado silenciosamente.
    private static SubtipoConta? ResolverSubtipoEdicao(TipoConta? tipo, string? subtipoInformado)
    {
        if (tipo != TipoConta.Banco)
        {
            return null;
        }

        var subtipo = ConverterSubtipoConta(subtipoInformado);
        if (!string.IsNullOrWhiteSpace(subtipoInformado) && subtipo == null)
        {
            throw new SubtipoContaInvalidoException(subtipoInformado);
        }

        return subtipo;
    }

    // Item 10.2: dias do cartao na edicao. Fechamento e validado antes de Vencimento.
    private static void ValidarDiasCartaoEdicao(int? diaFechamento, int? diaVencimento)
    {
        ValidarDiaCicloEdicao(diaFechamento, NomeCampoDiaFechamento);
        ValidarDiaCicloEdicao(diaVencimento, NomeCampoDiaVencimento);
    }

    private static void ValidarDiaCicloEdicao(int? dia, string nomeCampo)
    {
        if (dia == null)
        {
            throw new CampoObrigatorioException(nomeCampo);
        }

        if (dia < DiaMinimoCiclo || dia > DiaMaximoCiclo)
        {
            throw new ValorInvalidoException(nomeCampo, dia.Value, DiaMinimoCiclo, DiaMaximoCiclo);
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

        if (request.DiaFechamento < DiaMinimoCiclo || request.DiaFechamento > DiaMaximoCiclo)
        {
            return (false, $"Dia de fechamento deve estar entre {DiaMinimoCiclo} e {DiaMaximoCiclo}");
        }

        if (request.DiaVencimento < DiaMinimoCiclo || request.DiaVencimento > DiaMaximoCiclo)
        {
            return (false, $"Dia de vencimento deve estar entre {DiaMinimoCiclo} e {DiaMaximoCiclo}");
        }

        return (true, null);
    }
}
