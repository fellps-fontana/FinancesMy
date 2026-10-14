using MyFinances.Domain;
using MyFinances.DTOs.Ativo;

namespace MyFinances.Services;

public interface IAtivoService
{
    // Cadastro de ativo NOVO (item 8.5): contaId obrigatorio (Guid.Empty ->
    // CampoObrigatorioException("conta_id")), sem instituicao. Conta inexistente ->
    // ContaNaoEncontradaException; nao-INVESTIMENTO ou inativa -> ContaInvestimentoInvalidaParaAtivoException.
    Task<Ativo> CriarAtivo(string nome, TipoAtivo tipo, Guid contaId, decimal quantidade, decimal precoUnitario, DateOnly dataCompra);

    // Define ou troca a conta de um ativo (item 8.5); nunca volta a null. Erros:
    // CampoObrigatorioException (Guid.Empty), AtivoNaoEncontradoException (inexistente ou inativo), ContaNaoEncontradaException,
    // ContaInvestimentoInvalidaParaAtivoException. Nao toca aportes nem rendimentos.
    Task<Ativo> VincularConta(Guid ativoId, Guid contaId);

    Task<AtivoAporte> RegistrarAporte(Guid ativoId, decimal quantidade, decimal precoUnitario, DateOnly data);
    Task<IEnumerable<AtivoAporte>> ListarAportes(Guid ativoId);
    Task<IEnumerable<Ativo>> ListarAtivos();
    Task AtualizarValorAtual(Guid id, decimal novoValorAtual);
    Task DesativarAtivo(Guid id);
    Task<AtivosResumoResponse> ObterResumo();
    decimal CalcularEvolucaoPercentual(decimal valorInvestido, decimal valorAtual);
    decimal CalcularPrecoMedio(decimal valorInvestido, decimal quantidade);
}
