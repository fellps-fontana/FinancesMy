using MyFinances.Domain;
using MyFinances.DTOs.Ativo;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class AtivoService : IAtivoService
{
    private readonly IAtivoRepository _ativoRepository;

    public AtivoService(IAtivoRepository ativoRepository)
    {
        _ativoRepository = ativoRepository;
    }

    public async Task<Ativo> CriarAtivo(string nome, TipoAtivo tipo, string instituicao, decimal valorInvestido, DateOnly dataCompra)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new CampoObrigatorioException(nameof(nome));
        }

        if (string.IsNullOrWhiteSpace(instituicao))
        {
            throw new CampoObrigatorioException(nameof(instituicao));
        }

        if (valorInvestido <= 0)
        {
            throw new ValorInvalidoException("valor_investido", valorInvestido);
        }

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Tipo = tipo,
            Instituicao = instituicao,
            ValorInvestido = valorInvestido,
            ValorAtual = valorInvestido,
            DataCompra = dataCompra,
            Ativa = true,
            CriadoEm = DateTime.UtcNow
        };

        await _ativoRepository.Adicionar(ativo);
        await _ativoRepository.Salvar();

        return ativo;
    }

    public async Task<IEnumerable<Ativo>> ListarAtivos()
    {
        return await _ativoRepository.ListarAtivas();
    }

    public async Task AtualizarValorAtual(Guid id, decimal novoValorAtual)
    {
        if (novoValorAtual <= 0)
        {
            throw new ValorInvalidoException("valor_atual", novoValorAtual);
        }

        var ativo = await _ativoRepository.ObterPorId(id);
        if (ativo == null)
        {
            throw new AtivoNaoEncontradoException(id);
        }

        ativo.ValorAtual = novoValorAtual;
        ativo.AtualizadoEm = DateTime.UtcNow;

        await _ativoRepository.Salvar();
    }

    public async Task DesativarAtivo(Guid id)
    {
        var ativo = await _ativoRepository.ObterPorId(id);
        if (ativo == null)
        {
            throw new AtivoNaoEncontradoException(id);
        }

        ativo.Ativa = false;
        ativo.AtualizadoEm = DateTime.UtcNow;

        await _ativoRepository.Salvar();
    }

    public async Task<AtivosResumoResponse> ObterResumo()
    {
        var ativos = (await _ativoRepository.ListarAtivas()).ToList();

        if (!ativos.Any())
        {
            return new AtivosResumoResponse
            {
                TotalInvestido = 0m,
                TotalAtual = 0m,
                PorTipo = []
            };
        }

        var totalInvestido = ativos.Sum(a => a.ValorInvestido);
        var totalAtual = ativos.Sum(a => a.ValorAtual);

        var porTipo = ativos
            .GroupBy(a => a.Tipo)
            .Select(g => new ResumoPorTipo
            {
                Tipo = g.Key.ToStorageValue(),
                ValorAtual = g.Sum(a => a.ValorAtual),
                PercentualDaCarteira = totalAtual > 0 ? (g.Sum(a => a.ValorAtual) / totalAtual) * 100 : 0m
            })
            .ToList();

        return new AtivosResumoResponse
        {
            TotalInvestido = totalInvestido,
            TotalAtual = totalAtual,
            PorTipo = porTipo
        };
    }

    public decimal CalcularEvolucaoPercentual(decimal valorInvestido, decimal valorAtual)
    {
        if (valorInvestido == 0)
        {
            return 0m;
        }

        return ((valorAtual - valorInvestido) / valorInvestido) * 100;
    }
}
