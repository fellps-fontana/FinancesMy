using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class RendimentoService : IRendimentoService
{
    private readonly IRendimentoRepository _rendimentoRepository;
    private readonly IAtivoRepository _ativoRepository;

    public RendimentoService(IRendimentoRepository rendimentoRepository, IAtivoRepository ativoRepository)
    {
        _rendimentoRepository = rendimentoRepository;
        _ativoRepository = ativoRepository;
    }

    public async Task<Rendimento> RegistrarDividendo(Guid ativoId, decimal valor, DateOnly data)
    {
        if (valor <= 0)
        {
            throw new ValorInvalidoException("valor", valor);
        }

        var ativo = await _ativoRepository.ObterPorId(ativoId);
        if (ativo == null)
        {
            throw new AtivoNaoEncontradoException(ativoId);
        }

        if (!ativo.Ativa)
        {
            throw new AtivoInativoException(ativoId);
        }

        var rendimento = new Rendimento
        {
            Id = Guid.NewGuid(),
            AtivoId = ativoId,
            Tipo = TipoRendimento.Dividendo,
            Origem = OrigemRendimento.Manual,
            Valor = valor,
            Data = data,
            CriadoEm = DateTime.UtcNow
        };

        await _rendimentoRepository.Adicionar(rendimento);
        await _rendimentoRepository.Salvar();

        return rendimento;
    }

    public async Task RegistrarValorizacaoAutomatica(Guid ativoId, decimal valorAtualAnterior, decimal valorAtualNovo, DateOnly data)
    {
        var delta = RendimentoValorizacaoCalculator.Calcular(valorAtualAnterior, valorAtualNovo);

        if (delta == null)
            return;

        var rendimento = new Rendimento
        {
            Id = Guid.NewGuid(),
            AtivoId = ativoId,
            Tipo = TipoRendimento.Valorizacao,
            Origem = OrigemRendimento.Automatico,
            Valor = delta.Value,
            Data = data,
            CriadoEm = DateTime.UtcNow
        };

        await _rendimentoRepository.Adicionar(rendimento);
    }

    public async Task<IEnumerable<Rendimento>> ObterHistorico(Guid ativoId)
    {
        return await _rendimentoRepository.ListarPorAtivo(ativoId);
    }

    public async Task<RendimentosResumo> ObterResumoGeral()
    {
        var rendimentos = (await _rendimentoRepository.ListarTodos()).ToList();

        var totalDividendos = rendimentos
            .Where(r => r.Tipo == TipoRendimento.Dividendo)
            .Sum(r => r.Valor);

        var totalValorizacao = rendimentos
            .Where(r => r.Tipo == TipoRendimento.Valorizacao)
            .Sum(r => r.Valor);

        return new RendimentosResumo(totalDividendos, totalValorizacao, rendimentos);
    }
}
