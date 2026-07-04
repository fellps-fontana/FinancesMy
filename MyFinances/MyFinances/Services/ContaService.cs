using MyFinances.Exceptions;
using MyFinances.Models;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class ContaService : IContaService
{
    private readonly IContaRepository _contaRepository;

    public ContaService(IContaRepository contaRepository)
    {
        _contaRepository = contaRepository;
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

    public async Task<IEnumerable<Conta>> ListarContasInvestimento()
    {
        var contas = await _contaRepository.ListarPorTipo(TipoConta.Investimento);
        return contas.Where(c => c.Ativa);
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

        conta.Ativa = false;
        await _contaRepository.Salvar();
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
}
