using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class RecorrenciaGeradorService : IRecorrenciaGeradorService
{
    private readonly IContaFixaRepository _contaFixaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly CompraCartaoService _compraCartaoService;

    public RecorrenciaGeradorService(
        IContaFixaRepository contaFixaRepository,
        ILancamentoRepository lancamentoRepository,
        CompraCartaoService compraCartaoService)
    {
        _contaFixaRepository = contaFixaRepository;
        _lancamentoRepository = lancamentoRepository;
        _compraCartaoService = compraCartaoService;
    }

    public async Task<int> GerarOcorrenciaAtualEProximaAsync(Guid contaFixaId, DateOnly dataReferencia)
    {
        var contaFixa = await _contaFixaRepository.ObterPorId(contaFixaId);
        if (contaFixa == null || !contaFixa.Ativa)
        {
            return 0;
        }

        var lancamentosGerados = 0;

        var proximaOcorrencia = ContaFixaLancamentoFactory.ProximaOcorrencia(dataReferencia, contaFixa.Periodicidade);
        var ocorrencias = new[] { dataReferencia, proximaOcorrencia };

        foreach (var data in ocorrencias)
        {
            var existeLancamento = await _contaFixaRepository.ExisteLancamentoGerado(contaFixa.Id, data.Year, data.Month);

            if (!existeLancamento)
            {
                if (contaFixa.Conta?.Tipo == TipoConta.Cartao)
                {
                    var lancamentoAtual = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, data.Year, data.Month);
                    var (sucesso, _, _) = await _compraCartaoService.CriarCompraAsync(
                        contaFixa.ContaId,
                        new CriarCompraRequest
                        {
                            Descricao = lancamentoAtual.Descricao,
                            Valor = lancamentoAtual.Valor,
                            Data = lancamentoAtual.Data,
                            CategoriaId = lancamentoAtual.CategoriaId
                        },
                        contaFixaId);

                    if (sucesso)
                    {
                        lancamentosGerados++;
                    }
                }
                else
                {
                    var lancamento = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, data.Year, data.Month);
                    await _lancamentoRepository.Adicionar(lancamento);
                    lancamentosGerados++;
                }
            }
        }

        await _lancamentoRepository.Salvar();

        return lancamentosGerados;
    }

    public async Task GarantirOcorrenciasAtivasDoMesAsync(int ano, int mes)
    {
        var contasFixas = await _contaFixaRepository.Listar(true);

        foreach (var contaFixa in contasFixas)
        {
            if (ContaFixaLancamentoFactory.EhOcorrenciaValida(contaFixa, ano, mes))
            {
                var existeLancamento = await _contaFixaRepository.ExisteLancamentoGerado(contaFixa.Id, ano, mes);
                if (!existeLancamento)
                {
                    await GerarOcorrenciaAtualEProximaAsync(contaFixa.Id, new DateOnly(ano, mes, 1));
                }
            }
        }
    }

    public async Task<bool> GarantirOcorrenciaDoMesAsync(Guid contaFixaId, int ano, int mes)
    {
        var contaFixa = await _contaFixaRepository.ObterPorId(contaFixaId);
        if (contaFixa == null || !contaFixa.Ativa)
        {
            return false;
        }

        var existeLancamento = await _contaFixaRepository.ExisteLancamentoGerado(contaFixaId, ano, mes);

        if (!existeLancamento)
        {
            if (contaFixa.Conta?.Tipo == TipoConta.Cartao)
            {
                var lancamentoAtual = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, ano, mes);
                var (sucesso, _, _) = await _compraCartaoService.CriarCompraAsync(
                    contaFixa.ContaId,
                    new CriarCompraRequest
                    {
                        Descricao = lancamentoAtual.Descricao,
                        Valor = lancamentoAtual.Valor,
                        Data = lancamentoAtual.Data,
                        CategoriaId = lancamentoAtual.CategoriaId
                    },
                    contaFixaId);

                return sucesso;
            }
            else
            {
                var lancamento = ContaFixaLancamentoFactory.CriarLancamentoPendente(contaFixa, ano, mes);
                await _lancamentoRepository.Adicionar(lancamento);
                await _lancamentoRepository.Salvar();
                return true;
            }
        }

        return false;
    }

    public async Task<int> LimparOcorrenciasForaDaPeriodicidadeAsync(
        Guid contaFixaId,
        PeriodicidadeContaFixa periodicidadeNova,
        int? mesReferenciaNovo)
    {
        var contaFixa = await _contaFixaRepository.ObterPorId(contaFixaId);
        if (contaFixa == null)
        {
            return 0;
        }

        var contaFixaNovaPeriodicidade = new ContaFixa
        {
            Periodicidade = periodicidadeNova,
            MesReferencia = mesReferenciaNovo,
            DiaVencimento = contaFixa.DiaVencimento
        };

        var dataReferencia = DateOnly.FromDateTime(DateTime.Today);
        var (dataAtualValida, dataProximaValida) = ContaFixaLancamentoFactory.CalcularConjuntoAtualEProxima(
            contaFixaNovaPeriodicidade,
            dataReferencia);

        var lancamentosRemovidos = 0;
        var lancamentoPendentes = contaFixa.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pendente)
            .ToList();

        foreach (var lancamento in lancamentoPendentes)
        {
            var ehMesAtualValido = lancamento.Data.Year == dataAtualValida.Year && lancamento.Data.Month == dataAtualValida.Month;
            var ehMesProximoValido = lancamento.Data.Year == dataProximaValida.Year && lancamento.Data.Month == dataProximaValida.Month;

            if (!ehMesAtualValido && !ehMesProximoValido)
            {
                await _lancamentoRepository.Remover(lancamento);
                lancamentosRemovidos++;
            }
        }

        await _lancamentoRepository.Salvar();

        return lancamentosRemovidos;
    }
}
