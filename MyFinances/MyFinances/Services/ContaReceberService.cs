using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class ContaReceberService : IContaReceberService
{
    private const string MotivoEmprestimo = "EMPRESTIMO nao altera o valor_total depois de registrado";
    private const string MotivoJaRecebido = "ja houve recebimento registrado nesta conta";

    private readonly IContaReceberRepository _contaReceberRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IRecebivelRecorrenteRepository? _recebivelRecorrenteRepository;

    public ContaReceberService(
        IContaReceberRepository contaReceberRepository,
        ITransferenciaRepository transferenciaRepository,
        ILancamentoRepository lancamentoRepository,
        IContaRepository contaRepository,
        IRecebivelRecorrenteRepository? recebivelRecorrenteRepository = null)
    {
        _contaReceberRepository = contaReceberRepository;
        _transferenciaRepository = transferenciaRepository;
        _lancamentoRepository = lancamentoRepository;
        _contaRepository = contaRepository;
        _recebivelRecorrenteRepository = recebivelRecorrenteRepository;
    }

    public async Task<ContaReceber> RegistrarRecebivel(
        string descricao,
        decimal valorTotal,
        DateOnly dataRegistro,
        DateOnly? dataPrevista,
        Guid? categoriaId)
    {
        var contaReceber = new ContaReceber
        {
            Id = Guid.NewGuid(),
            Tipo = TipoContaReceber.Recebivel,
            Descricao = descricao,
            ValorTotal = valorTotal,
            DataRegistro = dataRegistro,
            DataPrevista = dataPrevista,
            CategoriaId = categoriaId,
            Status = StatusContaReceber.Pendente
        };

        await _contaReceberRepository.Adicionar(contaReceber);
        await _contaReceberRepository.Salvar();

        return contaReceber;
    }

    public async Task<ContaReceber> RegistrarEmprestimo(
        string descricao,
        string pessoa,
        decimal valorTotal,
        Guid contaOrigemId,
        DateOnly dataRegistro,
        DateOnly? dataPrevista,
        Guid? categoriaId)
    {
        ValidarPessoa(pessoa);

        var contaOrigem = await _contaRepository.ObterPorId(contaOrigemId);
        if (contaOrigem == null)
            throw new ContaNaoEncontradaException(contaOrigemId);

        var contaReceber = new ContaReceber
        {
            Id = Guid.NewGuid(),
            Tipo = TipoContaReceber.Emprestimo,
            Descricao = descricao,
            Pessoa = pessoa,
            ValorTotal = valorTotal,
            DataRegistro = dataRegistro,
            DataPrevista = dataPrevista,
            CategoriaId = categoriaId,
            Status = StatusContaReceber.Pendente
        };

        await _contaReceberRepository.Adicionar(contaReceber);

        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            ContaOrigemId = contaOrigemId,
            ContaDestinoId = null,
            ContaReceberId = contaReceber.Id,
            Valor = valorTotal,
            Data = dataRegistro
        };

        await _transferenciaRepository.Adicionar(transferencia);

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaOrigemId,
            ContaReceberId = contaReceber.Id,
            Tipo = TipoLancamento.Debit,
            Status = StatusLancamento.Pago,
            Valor = valorTotal,
            Data = dataRegistro,
            Manual = true,
            TransferenciaId = transferencia.Id
        };

        await _lancamentoRepository.Adicionar(lancamento);

        await _contaReceberRepository.Salvar();

        return contaReceber;
    }

    public async Task<Lancamento> RegistrarRecebimento(
        Guid contaReceberId,
        Guid contaDestinoId,
        decimal valor,
        DateOnly data,
        Guid? categoriaId)
    {
        var contaReceber = await _contaReceberRepository.ObterPorId(contaReceberId);
        if (contaReceber == null)
            throw new ContaReceberNaoEncontradaException(contaReceberId);

        var contaDestino = await _contaRepository.ObterPorId(contaDestinoId);
        if (contaDestino == null)
            throw new ContaNaoEncontradaException(contaDestinoId);

        var saldo = ContaReceberSaldoCalculator.Calcular(contaReceber);
        if (valor > saldo.SaldoPendente)
            throw new ValorRecebimentoExcedeSaldoPendenteException(valor, saldo.SaldoPendente);

        var categoriaFinal = categoriaId ?? contaReceber.CategoriaId;

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaDestinoId,
            ContaReceberId = contaReceberId,
            Tipo = TipoLancamento.Credit,
            Status = StatusLancamento.Pago,
            Valor = valor,
            Data = data,
            CategoriaId = categoriaFinal,
            Manual = true
        };

        await _lancamentoRepository.Adicionar(lancamento);

        contaReceber.Recebimentos.Add(lancamento);
        var novoSaldo = ContaReceberSaldoCalculator.Calcular(contaReceber);
        contaReceber.Status = novoSaldo.Status;

        await _contaReceberRepository.Atualizar(contaReceber);
        await _contaReceberRepository.Salvar();

        return lancamento;
    }

    public async Task<ContaReceber> ObterPorId(Guid contaReceberId)
    {
        var contaReceber = await _contaReceberRepository.ObterPorId(contaReceberId);
        if (contaReceber == null)
            throw new ContaReceberNaoEncontradaException(contaReceberId);

        return contaReceber;
    }

    public Task<IEnumerable<ContaReceber>> Listar(StatusContaReceber? statusFiltro = null)
    {
        return _contaReceberRepository.Listar(statusFiltro);
    }

    public async Task<decimal> CalcularTotalAReceberEsperadoNoMes(int ano, int mes)
    {
        var contasParaProjecao = await _contaReceberRepository.ListarParaProjecaoDoMes(ano, mes);

        var totalAReceber = contasParaProjecao
            .Sum(cr => ContaReceberSaldoCalculator.Calcular(cr).SaldoPendente);

        return totalAReceber;
    }

    public async Task Excluir(Guid contaReceberId)
    {
        var contaReceber = await _contaReceberRepository.ObterPorId(contaReceberId);
        if (contaReceber == null)
        {
            throw new ContaReceberNaoEncontradaException(contaReceberId);
        }

        if (contaReceber.RecebivelRecorrenteId.HasValue && _recebivelRecorrenteRepository != null)
        {
            var molde = await _recebivelRecorrenteRepository.ObterPorId(contaReceber.RecebivelRecorrenteId.Value);
            if (molde != null)
            {
                // Desativa o molde para nao gerar novas ocorrencias futuras
                molde.Ativa = false;
                await _recebivelRecorrenteRepository.Atualizar(molde);
                await _recebivelRecorrenteRepository.Salvar();

                // Apaga a atual e as posteriores que ainda estiverem PENDENTE. O que passou nao precisa mexer.
                var ocorrenciasParaRemover = molde.Ocorrencias
                    .Where(o => o.Status == StatusContaReceber.Pendente &&
                                (!contaReceber.DataPrevista.HasValue || o.DataPrevista >= contaReceber.DataPrevista.Value))
                    .ToList();

                foreach (var ocorrencia in ocorrenciasParaRemover)
                {
                    await _contaReceberRepository.Remover(ocorrencia);
                }
            }
            else
            {
                await _contaReceberRepository.Remover(contaReceber);
            }
        }
        else
        {
            await _contaReceberRepository.Remover(contaReceber);
        }

        await _contaReceberRepository.Salvar();
    }

    // regra-de-negocio.md itens 9, 13 e 15. Ordem: 404 -> recorrente (409) -> descricao/valor (400)
    // -> pessoa (400) -> valor_total (409 se EMPRESTIMO ou RECEBIVEL fora de Pendente; igual ao atual e no-op)
    // -> grava descricao/pessoa/dataPrevista. Nunca altera Tipo nem Status. Nada e gravado em erro.
    public async Task<ContaReceber> EditarAsync(
        Guid id,
        string descricao,
        string? pessoa,
        DateOnly? dataPrevista,
        decimal? valorTotal)
    {
        var contaReceber = await ObterContaEditavel(id);

        ValidarDescricao(descricao);
        ValidarValorTotalInformado(valorTotal);
        var pessoaFinal = DefinirPessoaDaEdicao(contaReceber.Tipo, pessoa);

        var alterarValorTotal = ValorTotalMudou(contaReceber, valorTotal);
        if (alterarValorTotal)
            ValidarValorTotalEditavel(contaReceber);

        contaReceber.Descricao = descricao;
        contaReceber.Pessoa = pessoaFinal;
        contaReceber.DataPrevista = dataPrevista;

        if (alterarValorTotal)
            contaReceber.ValorTotal = valorTotal!.Value;

        await _contaReceberRepository.Atualizar(contaReceber);
        await _contaReceberRepository.Salvar();

        return contaReceber;
    }

    private async Task<ContaReceber> ObterContaEditavel(Guid id)
    {
        var contaReceber = await ObterPorId(id);
        if (contaReceber.RecebivelRecorrenteId.HasValue)
            throw new ContaReceberRecorrenteNaoEditavelException(id);

        return contaReceber;
    }

    private static void ValidarDescricao(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new CampoObrigatorioException("descricao");
    }

    private static void ValidarValorTotalInformado(decimal? valorTotal)
    {
        if (valorTotal.HasValue && valorTotal.Value <= 0m)
            throw new ValorInvalidoException("valor_total", valorTotal.Value);
    }

    private static string? DefinirPessoaDaEdicao(TipoContaReceber tipo, string? pessoa)
    {
        if (tipo != TipoContaReceber.Emprestimo)
            return null;

        ValidarPessoa(pessoa);
        return pessoa;
    }

    private static bool ValorTotalMudou(ContaReceber contaReceber, decimal? valorTotal)
    {
        return valorTotal.HasValue && valorTotal.Value != contaReceber.ValorTotal;
    }

    private static void ValidarValorTotalEditavel(ContaReceber contaReceber)
    {
        if (contaReceber.Tipo == TipoContaReceber.Emprestimo)
            throw new ValorTotalNaoEditavelException(MotivoEmprestimo);

        var status = ContaReceberSaldoCalculator.Calcular(contaReceber).Status;
        if (status != StatusContaReceber.Pendente)
            throw new ValorTotalNaoEditavelException(MotivoJaRecebido);
    }

    private static void ValidarPessoa(string? pessoa)
    {
        if (string.IsNullOrWhiteSpace(pessoa))
            throw new PessoaObrigatoriaParaEmprestimoException();
    }
}

