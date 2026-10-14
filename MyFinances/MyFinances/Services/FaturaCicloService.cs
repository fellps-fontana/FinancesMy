using Microsoft.EntityFrameworkCore;
using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

// Orquestra o ciclo/status da fatura (item 12, "Ciclo e status da fatura"). A regra pura vive em
// FaturaCicloPolitica; aqui ficam a leitura do relogio, a persistencia e a ordem das gravacoes.
public class FaturaCicloService
{
    private const int DiaPadraoCiclo = 1;

    private readonly IFaturaRepository _faturaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly TimeProvider _timeProvider;

    public FaturaCicloService(IFaturaRepository faturaRepository, IContaRepository contaRepository)
        : this(faturaRepository, contaRepository, TimeProvider.System)
    {
    }

    public FaturaCicloService(
        IFaturaRepository faturaRepository,
        IContaRepository contaRepository,
        TimeProvider timeProvider)
    {
        _faturaRepository = faturaRepository;
        _contaRepository = contaRepository;
        _timeProvider = timeProvider;
    }

    // No-op se a conta nao existe ou nao e Cartao. Idempotente.
    public async Task ReconciliarAsync(Guid contaId)
    {
        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null || !EhCartao(conta))
        {
            return;
        }

        await ReconciliarContaAsync(conta);
    }

    // Null se a conta nao e cartao ou se nao existe fatura atual. Pode devolver fatura PAGA.
    public async Task<Fatura?> ObterFaturaAtualAsync(Guid contaId)
    {
        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null || !EhCartao(conta))
        {
            return null;
        }

        await ReconciliarContaAsync(conta);

        var hoje = HojeLocal();
        var faturas = await _faturaRepository.ListarParaReconciliacao(contaId, hoje);
        return FaturaCicloPolitica.SelecionarAtual(faturas, hoje, CalcularFechamentoCicloAtual(conta, hoje));
    }

    public async Task<(Fatura? Fatura, bool Rejeitada, string? Motivo)> ResolverFaturaParaLancamentoAsync(
        Guid contaId,
        DateOnly dataLancamento)
    {
        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null)
        {
            return (null, true, "Conta nao encontrada");
        }

        await ReconciliarContaAsync(conta);

        var hoje = HojeLocal();
        var (dataFechamento, dataVencimento) = CalcularDatasCiclo(
            DiaFechamentoDa(conta),
            DiaVencimentoDa(conta),
            dataLancamento);

        var faturaDoCiclo = await _faturaRepository.ObterPorContaEDataFechamento(contaId, dataFechamento);
        var destino = FaturaCicloPolitica.DecidirDestino(faturaDoCiclo, hoje);

        if (destino == DestinoLancamentoFatura.RejeitarPaga)
        {
            return (null, true, FaturaCicloPolitica.ErroCicloPagoEncerrado);
        }

        var fatura = await AplicarDestinoAsync(conta, destino, faturaDoCiclo, dataFechamento, dataVencimento, hoje);
        return (fatura, false, null);
    }

    // Reabre uma fatura PAGA: volta a FUTURA e reconcilia, que a promove a ABERTA se for a atual.
    public async Task<Fatura> ReabrirPagaAsync(Fatura fatura)
    {
        var conta = await _contaRepository.ObterPorId(fatura.ContaId);
        fatura.Status = StatusFatura.Futura;
        await _faturaRepository.Atualizar(fatura);
        await _faturaRepository.Salvar();

        if (conta != null && EhCartao(conta))
        {
            await ReconciliarContaAsync(conta);
        }

        return fatura;
    }

    // Null quando o ciclo destino nao pode receber (Fechada/Paga, existente ou por calendario).
    // Reconcilia, calcula o ciclo de novaData, aplica Usar ou CriarNova. NUNCA ReabrirEUsar.
    public async Task<Fatura?> ResolverFaturaParaCompraMovidaAsync(Guid contaId, DateOnly novaData)
    {
        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null || !EhCartao(conta))
        {
            return null;
        }

        await ReconciliarContaAsync(conta);

        var hoje = HojeLocal();
        var (dataFechamento, dataVencimento) = CalcularDatasCiclo(
            DiaFechamentoDa(conta),
            DiaVencimentoDa(conta),
            novaData);

        var faturaDoCiclo = await _faturaRepository.ObterPorContaEDataFechamento(contaId, dataFechamento);
        var fechamentoCicloAtual = CalcularFechamentoCicloAtual(conta, hoje);

        if (!FaturaCicloPolitica.PodeReceberCompraMovida(faturaDoCiclo?.Status, dataFechamento, hoje, fechamentoCicloAtual))
        {
            return null;
        }

        return faturaDoCiclo ?? await CriarFaturaAsync(conta, dataFechamento, dataVencimento, hoje);
    }

    public (DateOnly DataFechamento, DateOnly DataVencimento) CalcularDatasCiclo(
        int diaFechamento,
        int diaVencimento,
        DateOnly dataLancamento)
    {
        var dataFechamento = CriarDataValida(dataLancamento.Year, dataLancamento.Month, diaFechamento);

        if (dataLancamento > dataFechamento)
        {
            dataFechamento = dataFechamento.AddMonths(1);
        }

        var mesVencimento = dataFechamento.Month;
        var anoVencimento = dataFechamento.Year;
        var dataVencimento = CriarDataValida(anoVencimento, mesVencimento, diaVencimento);

        if (dataVencimento <= dataFechamento)
        {
            var proximoMes = dataFechamento.AddMonths(1);
            dataVencimento = CriarDataValida(proximoMes.Year, proximoMes.Month, diaVencimento);
        }

        return (dataFechamento, dataVencimento);
    }

    private async Task<Fatura> AplicarDestinoAsync(
        Conta conta,
        DestinoLancamentoFatura destino,
        Fatura? faturaDoCiclo,
        DateOnly dataFechamento,
        DateOnly dataVencimento,
        DateOnly hoje)
    {
        return destino switch
        {
            DestinoLancamentoFatura.ReabrirEUsar => await ReabrirPagaAsync(faturaDoCiclo!),
            DestinoLancamentoFatura.CriarNova => await CriarFaturaAsync(conta, dataFechamento, dataVencimento, hoje),
            _ => faturaDoCiclo!
        };
    }

    // Nasce com o status do calendario, mas ABERTA vira FUTURA: a promocao para ABERTA e feita pela
    // reconciliacao, depois das demissoes, para nao violar o indice unico IX_fatura_conta_aberta.
    // Corrida: outra requisicao criou a fatura do mesmo (conta, data_fechamento) entre o lookup e o
    // INSERT. O indice unico IX_fatura_conta_data_fechamento rejeita o INSERT; devolvemos a vencedora.
    // LIMITACAO: a recuperacao pela vencedora so vale FORA de transacao explicita no Postgres. Dentro de
    // BeginTransactionAsync, a violacao do indice aborta a transacao (SQLSTATE 25P02) e a releitura
    // falha; entao o erro sobe e a operacao inteira e desfeita, sem duplicata gravada. O SQLite nao
    // reproduz esse comportamento (a transacao continua valida apos a falha do statement).
    private async Task<Fatura> CriarFaturaAsync(Conta conta, DateOnly dataFechamento, DateOnly dataVencimento, DateOnly hoje)
    {
        var statusCalendario = FaturaCicloPolitica.ClassificarPorCalendario(
            dataFechamento,
            hoje,
            CalcularFechamentoCicloAtual(conta, hoje));

        var novaFatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            DataFechamento = dataFechamento,
            DataVencimento = dataVencimento,
            Status = statusCalendario == StatusFatura.Aberta ? StatusFatura.Futura : statusCalendario
        };

        var fatura = await InserirOuRecuperarVencedoraAsync(novaFatura, conta.Id, dataFechamento);
        await ReconciliarContaAsync(conta);

        return fatura;
    }

    // Ver LIMITACAO em CriarFaturaAsync: fora de transacao explicita no Postgres apenas.
    private async Task<Fatura> InserirOuRecuperarVencedoraAsync(Fatura novaFatura, Guid contaId, DateOnly dataFechamento)
    {
        try
        {
            await _faturaRepository.Adicionar(novaFatura);
            await _faturaRepository.Salvar();
            return novaFatura;
        }
        catch (DbUpdateException)
        {
            _faturaRepository.Descartar(novaFatura);

            var vencedora = await _faturaRepository.ObterPorContaEDataFechamento(contaId, dataFechamento);
            if (vencedora == null)
            {
                throw;
            }

            return vencedora;
        }
    }

    private async Task ReconciliarContaAsync(Conta conta)
    {
        var hoje = HojeLocal();
        var faturas = await _faturaRepository.ListarParaReconciliacao(conta.Id, hoje);
        var mudancas = FaturaCicloPolitica.Reconciliar(faturas, hoje, CalcularFechamentoCicloAtual(conta, hoje));

        // Demissoes antes da promocao: o indice unico IX_fatura_conta_aberta nao aceita duas ABERTA.
        await AplicarEPersistirAsync(mudancas.Where(m => m.NovoStatus != StatusFatura.Aberta));
        await AplicarEPersistirAsync(mudancas.Where(m => m.NovoStatus == StatusFatura.Aberta));
    }

    private async Task AplicarEPersistirAsync(IEnumerable<MudancaStatusFatura> mudancas)
    {
        var lista = mudancas.ToList();
        if (lista.Count == 0)
        {
            return;
        }

        foreach (var mudanca in lista)
        {
            mudanca.Fatura.Status = mudanca.NovoStatus;
            await _faturaRepository.Atualizar(mudanca.Fatura);
        }

        await _faturaRepository.Salvar();
    }

    private DateOnly HojeLocal() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    private DateOnly CalcularFechamentoCicloAtual(Conta conta, DateOnly hoje)
    {
        return CalcularDatasCiclo(DiaFechamentoDa(conta), DiaVencimentoDa(conta), hoje).DataFechamento;
    }

    private static bool EhCartao(Conta conta) => conta.Tipo == TipoConta.Cartao;

    private static int DiaFechamentoDa(Conta conta) => conta.DiaFechamento ?? DiaPadraoCiclo;

    private static int DiaVencimentoDa(Conta conta) => conta.DiaVencimento ?? DiaPadraoCiclo;

    private static DateOnly CriarDataValida(int ano, int mes, int dia)
    {
        var diasNoMes = DateTime.DaysInMonth(ano, mes);
        var diaAjustado = Math.Min(dia, diasNoMes);
        return new DateOnly(ano, mes, diaAjustado);
    }
}
