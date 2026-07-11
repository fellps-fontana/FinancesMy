using MyFinances.Exceptions;
using MyFinances.Models;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class AtivoService : IAtivoService
{
    private readonly IAtivoRepository _ativoRepository;
    private readonly IContaRepository _contaRepository;

    public AtivoService(IAtivoRepository ativoRepository, IContaRepository contaRepository)
    {
        _ativoRepository = ativoRepository;
        _contaRepository = contaRepository;
    }

    public async Task<Ativo> RegistrarCompra(Guid contaId, string ticker, decimal quantidade, decimal precoUnitario, DateOnly data, string? nome)
    {
        ValidarContaInvestimento(contaId, await ObterContaOuFalhar(contaId));
        ValidarValorPositivo(quantidade, nameof(quantidade));
        ValidarValorPositivo(precoUnitario, nameof(precoUnitario));

        var ativoExistente = await _ativoRepository.ObterAtivoAtivoPorTicker(contaId, ticker);

        if (ativoExistente == null)
        {
            var novoAtivo = new Ativo
            {
                Id = Guid.NewGuid(),
                ContaId = contaId,
                Ticker = ticker,
                Nome = nome,
                Quantidade = quantidade,
                PrecoMedio = precoUnitario,
                PrecoAtual = precoUnitario,
                Ativa = true,
                CriadoEm = DateTime.UtcNow
            };

            await _ativoRepository.Adicionar(novoAtivo);
            await RegistrarMovimentacao(novoAtivo.Id, TipoMovimentacaoAtivo.Compra, quantidade, precoUnitario, data);
            await _ativoRepository.Salvar();

            return novoAtivo;
        }

        var precoMedioNovo = CalcularPrecoMedio(ativoExistente.PrecoMedio, ativoExistente.Quantidade, precoUnitario, quantidade);
        ativoExistente.PrecoMedio = precoMedioNovo;
        ativoExistente.Quantidade += quantidade;
        ativoExistente.PrecoAtual = precoUnitario;

        await RegistrarMovimentacao(ativoExistente.Id, TipoMovimentacaoAtivo.Compra, quantidade, precoUnitario, data);
        await _ativoRepository.Salvar();

        return ativoExistente;
    }

    public async Task<Ativo> RegistrarVenda(Guid contaId, Guid ativoId, decimal quantidade, decimal precoUnitario, DateOnly data, string? observacao)
    {
        ValidarValorPositivo(quantidade, nameof(quantidade));
        ValidarValorPositivo(precoUnitario, nameof(precoUnitario));

        var ativo = await _ativoRepository.ObterPorId(ativoId);
        if (ativo == null || !ativo.Ativa || ativo.ContaId != contaId)
        {
            throw new AtivoNaoEncontradoException(ativoId);
        }

        if (quantidade > ativo.Quantidade)
        {
            throw new QuantidadeVendaInvalidaException(ativoId, quantidade, ativo.Quantidade);
        }

        ativo.Quantidade -= quantidade;

        if (ativo.Quantidade == 0)
        {
            ativo.Ativa = false;
        }

        await RegistrarMovimentacao(ativoId, TipoMovimentacaoAtivo.Venda, quantidade, precoUnitario, data, observacao);
        await _ativoRepository.Salvar();

        return ativo;
    }

    public async Task<IEnumerable<Ativo>> ListarAtivosPorConta(Guid contaId)
    {
        var conta = await ObterContaOuFalhar(contaId);
        ValidarContaInvestimento(contaId, conta);
        return await _ativoRepository.ListarAtivosAtivosPorConta(contaId);
    }

    private decimal CalcularPrecoMedio(decimal precoMedioAtual, decimal quantidadeAtual, decimal precoCompra, decimal quantidadeCompra)
    {
        return (precoMedioAtual * quantidadeAtual + precoCompra * quantidadeCompra) / (quantidadeAtual + quantidadeCompra);
    }

    private async Task RegistrarMovimentacao(Guid ativoId, TipoMovimentacaoAtivo tipo, decimal quantidade, decimal precoUnitario, DateOnly data, string? observacao = null)
    {
        var movimentacao = new MovimentacaoAtivo
        {
            Id = Guid.NewGuid(),
            AtivoId = ativoId,
            Tipo = tipo,
            Quantidade = quantidade,
            PrecoUnitario = precoUnitario,
            Data = data,
            Observacao = observacao
        };

        await _ativoRepository.AdicionarMovimentacao(movimentacao);
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

    private void ValidarContaInvestimento(Guid contaId, Conta conta)
    {
        if (conta.Tipo != TipoConta.Investimento)
        {
            throw new ContaNaoEhInvestimentoException(contaId, conta.Tipo ?? TipoConta.Banco);
        }
    }

    private void ValidarValorPositivo(decimal valor, string nomeCampo)
    {
        if (valor <= 0)
        {
            throw new ValorInvalidoException(nomeCampo, valor);
        }
    }
}
