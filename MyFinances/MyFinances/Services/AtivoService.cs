using MyFinances.Domain;
using MyFinances.DTOs.Ativo;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class AtivoService : IAtivoService
{
    private readonly IAtivoRepository _ativoRepository;
    private readonly IRendimentoService _rendimentoService;
    private readonly IAtivoAporteRepository _ativoAporteRepository;

    public AtivoService(IAtivoRepository ativoRepository, IRendimentoService rendimentoService, IAtivoAporteRepository ativoAporteRepository)
    {
        _ativoRepository = ativoRepository;
        _rendimentoService = rendimentoService;
        _ativoAporteRepository = ativoAporteRepository;
    }

    public async Task<Ativo> CriarAtivo(string nome, TipoAtivo tipo, string instituicao, decimal quantidade, decimal precoUnitario, DateOnly dataCompra)
    {
        ValidarCampoObrigatorio(nome, "nome");
        ValidarCampoObrigatorio(instituicao, "instituicao");
        ValidarValor(quantidade, "quantidade");
        ValidarValor(precoUnitario, "preco_unitario");

        var valorInvestido = quantidade * precoUnitario;
        var agora = DateTime.UtcNow;

        var ativo = new Ativo
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Tipo = tipo,
            Instituicao = instituicao,
            Quantidade = quantidade,
            ValorInvestido = valorInvestido,
            ValorAtual = valorInvestido,
            DataCompra = dataCompra,
            Ativa = true,
            CriadoEm = agora
        };

        var aporte = new AtivoAporte
        {
            Id = Guid.NewGuid(),
            AtivoId = ativo.Id,
            Data = dataCompra,
            Quantidade = quantidade,
            PrecoUnitario = precoUnitario,
            CriadoEm = agora
        };

        await _ativoRepository.Adicionar(ativo);
        await _ativoAporteRepository.Adicionar(aporte);
        await _ativoRepository.Salvar();
        await _ativoAporteRepository.Salvar();

        return ativo;
    }

    public async Task<AtivoAporte> RegistrarAporte(Guid ativoId, decimal quantidade, decimal precoUnitario, DateOnly data)
    {
        ValidarValor(quantidade, "quantidade");
        ValidarValor(precoUnitario, "preco_unitario");

        var ativo = await _ativoRepository.ObterPorId(ativoId);
        if (ativo == null || !ativo.Ativa)
        {
            throw new AtivoNaoEncontradoException(ativoId);
        }

        ativo.Quantidade += quantidade;
        ativo.ValorInvestido += precoUnitario * quantidade;
        ativo.AtualizadoEm = DateTime.UtcNow;

        var aporte = new AtivoAporte
        {
            Id = Guid.NewGuid(),
            AtivoId = ativoId,
            Data = data,
            Quantidade = quantidade,
            PrecoUnitario = precoUnitario,
            CriadoEm = DateTime.UtcNow
        };

        await _ativoAporteRepository.Adicionar(aporte);
        await _ativoRepository.Salvar();
        await _ativoAporteRepository.Salvar();

        return aporte;
    }

    public async Task<IEnumerable<AtivoAporte>> ListarAportes(Guid ativoId)
    {
        return await _ativoAporteRepository.ListarPorAtivo(ativoId);
    }

    private static void ValidarCampoObrigatorio(string valor, string nomeCampo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new CampoObrigatorioException(nomeCampo);
        }
    }

    private static void ValidarValor(decimal valor, string nomeCampo)
    {
        if (valor <= 0)
        {
            throw new ValorInvalidoException(nomeCampo, valor);
        }
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

        var valorAtualAnterior = ativo.ValorAtual;

        ativo.ValorAtual = novoValorAtual;
        ativo.AtualizadoEm = DateTime.UtcNow;

        await _rendimentoService.RegistrarValorizacaoAutomatica(
            id, valorAtualAnterior, novoValorAtual, DateOnly.FromDateTime(DateTime.UtcNow));

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

    public decimal CalcularPrecoMedio(decimal valorInvestido, decimal quantidade)
    {
        if (quantidade == 0)
        {
            return 0m;
        }

        return valorInvestido / quantidade;
    }
}
