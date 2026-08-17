using MyFinances.Domain;
using MyFinances.DTOs.Ativo;

namespace MyFinances.Services;

public interface IAtivoService
{
    Task<Ativo> CriarAtivo(string nome, TipoAtivo tipo, string instituicao, decimal quantidade, decimal precoUnitario, DateOnly dataCompra);
    Task<AtivoAporte> RegistrarAporte(Guid ativoId, decimal quantidade, decimal precoUnitario, DateOnly data);
    Task<IEnumerable<AtivoAporte>> ListarAportes(Guid ativoId);
    Task<IEnumerable<Ativo>> ListarAtivos();
    Task AtualizarValorAtual(Guid id, decimal novoValorAtual);
    Task DesativarAtivo(Guid id);
    Task<AtivosResumoResponse> ObterResumo();
    decimal CalcularEvolucaoPercentual(decimal valorInvestido, decimal valorAtual);
    decimal CalcularPrecoMedio(decimal valorInvestido, decimal quantidade);
}
