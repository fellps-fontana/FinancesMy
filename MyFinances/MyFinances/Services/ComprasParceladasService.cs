using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class ComprasParceladasService
{
    private readonly ICompraParceladaRepository _compraParceladaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly FaturaCicloService _faturaCicloService;
    private readonly ValidacaoCartaoService _validacaoCartaoService;

    public ComprasParceladasService(
        ICompraParceladaRepository compraParceladaRepository,
        ILancamentoRepository lancamentoRepository,
        FaturaCicloService faturaCicloService,
        ValidacaoCartaoService validacaoCartaoService)
    {
        _compraParceladaRepository = compraParceladaRepository;
        _lancamentoRepository = lancamentoRepository;
        _faturaCicloService = faturaCicloService;
        _validacaoCartaoService = validacaoCartaoService;
    }

    public async Task<(bool Sucesso, CompraParcelada? CompraParcelada, string? Erro)> CriarCompraParceladaAsync(
        Guid contaId,
        CriarCompraParceladaRequest request)
    {
        if (request.QuantidadeParcelas < 2)
        {
            return (false, null, "Quantidade de parcelas deve ser no minimo 2");
        }

        var (valido, conta, erro) = await _validacaoCartaoService.ValidarOperacaoCartaoAsync(
            contaId,
            request.Descricao,
            request.ValorTotal);

        if (!valido)
        {
            return (false, null, erro);
        }

        var valores = ParcelamentoCalculator.CalcularValoresParcelas(
            request.ValorTotal,
            request.QuantidadeParcelas);

        var (faturas, erroFatura) = await ResolverFaturasParceladasAsync(
            contaId,
            request.DataCompra,
            request.QuantidadeParcelas);

        if (faturas == null)
        {
            return (false, null, erroFatura);
        }

        var lancamentos = MontarLancamentos(
            contaId,
            conta!,
            request,
            valores,
            faturas);

        var compraParcelada = new CompraParcelada
        {
            Id = Guid.NewGuid(),
            Descricao = request.Descricao,
            ValorTotal = request.ValorTotal,
            QuantidadeParcelas = request.QuantidadeParcelas,
            DataCompra = request.DataCompra,
            Lancamentos = lancamentos
        };

        await _compraParceladaRepository.Adicionar(compraParcelada);

        foreach (var lancamento in lancamentos)
        {
            lancamento.CompraParceladaId = compraParcelada.Id;
            await _lancamentoRepository.Adicionar(lancamento);
        }

        await _compraParceladaRepository.Salvar();

        return (true, compraParcelada, null);
    }

    private async Task<(IReadOnlyList<Fatura>? Faturas, string? Erro)> ResolverFaturasParceladasAsync(
        Guid contaId,
        DateOnly dataPrimeiraCompra,
        int quantidadeParcelas)
    {
        var faturas = new List<Fatura>();
        DateOnly dataReferencia = dataPrimeiraCompra;

        for (int i = 0; i < quantidadeParcelas; i++)
        {
            var (fatura, rejeitada, motivo) = await _faturaCicloService.ResolverFaturaParaLancamentoAsync(
                contaId,
                dataReferencia);

            if (rejeitada)
            {
                return (null, motivo);
            }

            var novaFatura = fatura!;
            faturas.Add(novaFatura);
            dataReferencia = novaFatura.DataVencimento.AddDays(1);
        }

        return (faturas.AsReadOnly(), null);
    }

    private List<Lancamento> MontarLancamentos(
        Guid contaId,
        Conta conta,
        CriarCompraParceladaRequest request,
        IReadOnlyList<decimal> valores,
        IReadOnlyList<Fatura> faturas)
    {
        var lancamentos = new List<Lancamento>();
        DateOnly dataReferencia = request.DataCompra;

        for (int i = 0; i < valores.Count; i++)
        {
            var lancamento = new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Conta = conta,
                CategoriaId = request.CategoriaId,
                Descricao = request.Descricao,
                Valor = valores[i],
                Tipo = TipoLancamento.Debit,
                Data = dataReferencia,
                Status = StatusLancamento.Pago,
                Manual = true,
                Oculto = false,
                PierreTxnId = null,
                FaturaId = faturas[i].Id,
                TransferenciaId = null,
                ConciliadoCom = null,
                ContaFixaId = null,
                ParcelaNumero = i + 1
            };

            lancamentos.Add(lancamento);
            dataReferencia = faturas[i].DataVencimento.AddDays(1);
        }

        return lancamentos;
    }
}
