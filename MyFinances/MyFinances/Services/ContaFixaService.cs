using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class ContaFixaService : IContaFixaService
{
    private readonly IContaFixaRepository _contaFixaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IRecorrenciaGeradorService _recorrenciaGeradorService;

    public ContaFixaService(
        IContaFixaRepository contaFixaRepository,
        IContaRepository contaRepository,
        ILancamentoRepository lancamentoRepository,
        IRecorrenciaGeradorService recorrenciaGeradorService)
    {
        _contaFixaRepository = contaFixaRepository;
        _contaRepository = contaRepository;
        _lancamentoRepository = lancamentoRepository;
        _recorrenciaGeradorService = recorrenciaGeradorService;
    }

    // mesReferencia: regra-de-negocio.md item 6 exige validar Conta.Tipo IN (Banco, Cartao)
    // e mesReferencia obrigatorio quando periodicidade resolve para Anual
    // (default = mes de hoje se omitido).
    public async Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> CriarAsync(
        Guid contaId, string descricao, decimal valor, int diaVencimento, Guid? categoriaId,
        string? periodicidade = null, int? mesReferencia = null)
    {
        var validacao = ValidarDiaVencimentoEValor(diaVencimento, valor);
        if (!validacao.Valido)
        {
            return (false, null, validacao.Erro);
        }

        PeriodicidadeContaFixa periodicidadeEnum = PeriodicidadeContaFixa.Mensal;
        if (!string.IsNullOrEmpty(periodicidade))
        {
            var periodicidadeConvertida = ConverterPeriodicidade(periodicidade);
            if (periodicidadeConvertida == null)
            {
                return (false, null, $"Periodicidade invalida: {periodicidade}");
            }

            periodicidadeEnum = periodicidadeConvertida.Value;
        }

        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null)
        {
            return (false, null, "Conta nao encontrada");
        }

        if (conta.Tipo != TipoConta.Banco && conta.Tipo != TipoConta.Cartao)
        {
            return (false, null, $"Tipo de conta '{conta.Tipo}' nao e permitido para Conta Fixa. Apenas Banco e Cartao sao permitidos.");
        }

        var mesReferenciaFinal = mesReferencia;
        if (periodicidadeEnum == PeriodicidadeContaFixa.Anual && !mesReferenciaFinal.HasValue)
        {
            mesReferenciaFinal = DateTime.Today.Month;
        }

        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Descricao = descricao,
            Valor = valor,
            DiaVencimento = diaVencimento,
            CategoriaId = categoriaId,
            Periodicidade = periodicidadeEnum,
            MesReferencia = mesReferenciaFinal,
            Ativa = true
        };

        await _contaFixaRepository.Adicionar(contaFixa);
        await _contaFixaRepository.Salvar();

        var dataReferencia = DateOnly.FromDateTime(DateTime.Today);
        await GerarLancamentosPendentes(contaFixa.Id, dataReferencia);

        return (true, contaFixa, null);
    }

    // mesReferencia: regra-de-negocio.md item 6 exige chamar
    // IRecorrenciaGeradorService.LimparOcorrenciasForaDaPeriodicidadeAsync
    // quando periodicidade/mesReferencia mudam, antes de regenerar.
    public async Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> EditarAsync(
        Guid contaFixaId, decimal valor, int diaVencimento, Guid? categoriaId,
        string? periodicidade = null, int? mesReferencia = null)
    {
        var validacao = ValidarDiaVencimentoEValor(diaVencimento, valor);
        if (!validacao.Valido)
        {
            return (false, null, validacao.Erro);
        }

        PeriodicidadeContaFixa? periodicidadeEnum = null;
        if (!string.IsNullOrEmpty(periodicidade))
        {
            var periodicidadeConvertida = ConverterPeriodicidade(periodicidade);
            if (periodicidadeConvertida == null)
            {
                return (false, null, $"Periodicidade invalida: {periodicidade}");
            }

            periodicidadeEnum = periodicidadeConvertida.Value;
        }

        var contaFixa = await _contaFixaRepository.ObterPorId(contaFixaId);
        if (contaFixa == null)
        {
            throw new ContaFixaNaoEncontradaException(contaFixaId);
        }

        var periodicidadeMudou = periodicidadeEnum.HasValue && periodicidadeEnum.Value != contaFixa.Periodicidade;
        var mesReferenciaMudou = mesReferencia.HasValue && mesReferencia.Value != contaFixa.MesReferencia;

        if (periodicidadeMudou || mesReferenciaMudou)
        {
            await _recorrenciaGeradorService.LimparOcorrenciasForaDaPeriodicidadeAsync(
                contaFixaId,
                periodicidadeEnum ?? contaFixa.Periodicidade,
                mesReferencia ?? contaFixa.MesReferencia);
        }

        contaFixa.Valor = valor;
        contaFixa.DiaVencimento = diaVencimento;
        contaFixa.CategoriaId = categoriaId;
        if (periodicidadeEnum.HasValue)
        {
            contaFixa.Periodicidade = periodicidadeEnum.Value;
        }
        if (mesReferencia.HasValue)
        {
            contaFixa.MesReferencia = mesReferencia.Value;
        }

        await _contaFixaRepository.Atualizar(contaFixa);

        var lancamentosPendentes = contaFixa.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pendente)
            .ToList();

        foreach (var lancamento in lancamentosPendentes)
        {
            lancamento.Valor = valor;
            lancamento.CategoriaId = categoriaId;

            var diasNoMes = DateTime.DaysInMonth(lancamento.Data.Year, lancamento.Data.Month);
            var diaAjustado = Math.Min(diaVencimento, diasNoMes);
            lancamento.Data = new DateOnly(lancamento.Data.Year, lancamento.Data.Month, diaAjustado);

            await _lancamentoRepository.Atualizar(lancamento);
        }

        await _lancamentoRepository.Salvar();

        return (true, contaFixa, null);
    }

    public async Task<(bool Sucesso, string? Erro)> DesativarAsync(Guid contaFixaId)
    {
        var contaFixa = await _contaFixaRepository.ObterPorId(contaFixaId);
        if (contaFixa == null)
        {
            return (false, "Conta fixa nao encontrada");
        }

        contaFixa.Ativa = false;
        await _contaFixaRepository.Atualizar(contaFixa);

        var lancamentosPendentes = contaFixa.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pendente)
            .ToList();

        foreach (var lancamento in lancamentosPendentes)
        {
            await _lancamentoRepository.Remover(lancamento);
        }

        await _lancamentoRepository.Salvar();

        return (true, null);
    }

    public async Task<(bool Sucesso, string? Erro)> ReativarAsync(Guid contaFixaId)
    {
        var contaFixa = await _contaFixaRepository.ObterPorId(contaFixaId);
        if (contaFixa == null)
        {
            return (false, "Conta fixa nao encontrada");
        }

        contaFixa.Ativa = true;
        await _contaFixaRepository.Atualizar(contaFixa);
        await _contaFixaRepository.Salvar();

        var dataReferencia = DateOnly.FromDateTime(DateTime.Today);
        await GerarLancamentosPendentes(contaFixaId, dataReferencia);

        return (true, null);
    }

    public async Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> ObterPorId(Guid contaFixaId)
    {
        var contaFixa = await _contaFixaRepository.ObterPorId(contaFixaId);
        if (contaFixa == null)
        {
            return (false, null, "Conta fixa nao encontrada");
        }

        return (true, contaFixa, null);
    }

    public async Task<(bool Sucesso, IEnumerable<ContaFixa>? ContasFixas, string? Erro)> Listar(bool? ativaFiltro)
    {
        var contasFixas = await _contaFixaRepository.Listar(ativaFiltro);
        return (true, contasFixas, null);
    }

    public async Task<(bool Sucesso, int LancamentosGerados, string? Erro)> GerarLancamentosPendentes(
        Guid contaFixaId, DateOnly dataReferencia)
    {
        var contaFixa = await _contaFixaRepository.ObterPorId(contaFixaId);
        if (contaFixa == null || !contaFixa.Ativa)
        {
            return (false, 0, "Conta fixa nao encontrada ou inativa");
        }

        var lancamentosGerados = 0;

        var proximaOcorrencia = ContaFixaLancamentoFactory.ProximaOcorrencia(dataReferencia, contaFixa.Periodicidade);
        var ocorrencias = new[] { dataReferencia, proximaOcorrencia };

        foreach (var data in ocorrencias)
        {
            var existeLancamento = await _contaFixaRepository.ExisteLancamentoGerado(contaFixa.Id, data.Year, data.Month);

            if (!existeLancamento)
            {
                var lancamento = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, data.Year, data.Month);
                await _lancamentoRepository.Adicionar(lancamento);
                lancamentosGerados++;
            }
        }

        await _lancamentoRepository.Salvar();

        return (true, lancamentosGerados, null);
    }

    private static (bool Valido, string? Erro) ValidarDiaVencimentoEValor(int diaVencimento, decimal valor)
    {
        if (diaVencimento < 1 || diaVencimento > 31)
        {
            return (false, "Dia de vencimento deve estar entre 1 e 31");
        }

        if (valor <= 0)
        {
            return (false, "Valor deve ser maior que zero");
        }

        return (true, null);
    }

    private static PeriodicidadeContaFixa? ConverterPeriodicidade(string? periodicidade)
    {
        if (string.IsNullOrEmpty(periodicidade))
        {
            return null;
        }

        try
        {
            return PeriodicidadeContaFixaExtensions.FromStorageValue(periodicidade.ToUpperInvariant());
        }
        catch
        {
            return null;
        }
    }
}
