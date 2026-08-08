using MyFinances.Domain;

namespace MyFinances.Services;

public interface IRendimentoService
{
    Task<Rendimento> RegistrarDividendo(Guid ativoId, decimal valor, DateOnly data);

    Task RegistrarValorizacaoAutomatica(Guid ativoId, decimal valorAtualAnterior, decimal valorAtualNovo, DateOnly data);

    Task<IEnumerable<Rendimento>> ObterHistorico(Guid ativoId);

    Task<RendimentosResumo> ObterResumoGeral();
}

public record RendimentosResumo(decimal TotalDividendos, decimal TotalValorizacao, IEnumerable<Rendimento> Historico);
