using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class ContaFixaService : IContaFixaService
{
    private readonly IContaFixaRepository _contaFixaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;

    public ContaFixaService(
        IContaFixaRepository contaFixaRepository,
        IContaRepository contaRepository,
        ILancamentoRepository lancamentoRepository)
    {
        _contaFixaRepository = contaFixaRepository;
        _contaRepository = contaRepository;
        _lancamentoRepository = lancamentoRepository;
    }

    public async Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> CriarAsync(
        Guid contaId, string descricao, decimal valor, int diaVencimento, Guid? categoriaId)
    {
        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null)
        {
            return (false, null, "Conta nao encontrada");
        }

        var contaFixa = new ContaFixa
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Descricao = descricao,
            Valor = valor,
            DiaVencimento = diaVencimento,
            CategoriaId = categoriaId,
            Ativa = true
        };

        await _contaFixaRepository.Adicionar(contaFixa);
        await _contaFixaRepository.Salvar();

        var dataReferencia = DateOnly.FromDateTime(DateTime.Today);
        await GerarLancamentosPendentes(contaFixa.Id, dataReferencia);

        return (true, contaFixa, null);
    }

    public async Task<(bool Sucesso, ContaFixa? ContaFixa, string? Erro)> EditarAsync(
        Guid contaFixaId, decimal valor, int diaVencimento, Guid? categoriaId)
    {
        var contaFixa = await _contaFixaRepository.ObterPorId(contaFixaId);
        if (contaFixa == null)
        {
            return (false, null, "Conta fixa nao encontrada");
        }

        contaFixa.Valor = valor;
        contaFixa.DiaVencimento = diaVencimento;
        contaFixa.CategoriaId = categoriaId;

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

        var meses = new[] { 0, 1 };

        foreach (var mesOffset in meses)
        {
            var data = dataReferencia.AddMonths(mesOffset);
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
}
