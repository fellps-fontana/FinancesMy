using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class ContaReceberService : IContaReceberService
{
    private readonly IContaReceberRepository _contaReceberRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IContaRepository _contaRepository;

    public ContaReceberService(
        IContaReceberRepository contaReceberRepository,
        ITransferenciaRepository transferenciaRepository,
        ILancamentoRepository lancamentoRepository,
        IContaRepository contaRepository)
    {
        _contaReceberRepository = contaReceberRepository;
        _transferenciaRepository = transferenciaRepository;
        _lancamentoRepository = lancamentoRepository;
        _contaRepository = contaRepository;
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
            Manual = true
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

    private static void ValidarPessoa(string pessoa)
    {
        if (string.IsNullOrWhiteSpace(pessoa))
            throw new PessoaObrigatoriaParaEmprestimoException();
    }
}
