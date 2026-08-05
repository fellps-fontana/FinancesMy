using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IAtivoAporteRepository
{
    Task Adicionar(AtivoAporte aporte);
    Task<IEnumerable<AtivoAporte>> ListarPorAtivo(Guid ativoId);
    Task Salvar();
}
