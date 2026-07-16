using MyFinances.Domain;
using MyFinances.DTOs.Ativo;

namespace MyFinances.Services;

public interface IAtivoService
{
    Task<Ativo> CriarAtivo(string nome, TipoAtivo tipo, string instituicao, decimal valorInvestido, DateOnly dataCompra);
    Task<IEnumerable<Ativo>> ListarAtivos();
    Task AtualizarValorAtual(Guid id, decimal novoValorAtual);
    Task DesativarAtivo(Guid id);
    Task<AtivosResumoResponse> ObterResumo();
    decimal CalcularEvolucaoPercentual(decimal valorInvestido, decimal valorAtual);
}
