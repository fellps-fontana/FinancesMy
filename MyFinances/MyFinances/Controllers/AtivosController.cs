using MyFinances.DTOs.Ativo;
using MyFinances.Exceptions;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/ativos")]
public class AtivosController : ControllerBase
{
    private readonly IAtivoService _ativoService;

    public AtivosController(IAtivoService ativoService)
    {
        _ativoService = ativoService;
    }

    [HttpPost]
    public async Task<ActionResult<AtivoResponse>> CriarAtivo(CriarAtivoRequest request)
    {
        try
        {
            var ativo = await _ativoService.CriarAtivo(
                request.Nome,
                request.Tipo,
                request.Instituicao,
                request.Quantidade,
                request.PrecoUnitario,
                request.DataCompra);

            var evolucaoPercentual = _ativoService.CalcularEvolucaoPercentual(
                ativo.ValorInvestido,
                ativo.ValorAtual);

            var precoMedio = _ativoService.CalcularPrecoMedio(ativo.ValorInvestido, ativo.Quantidade);

            var response = AtivoResponse.FromAtivo(ativo, evolucaoPercentual, precoMedio);

            return Created($"/api/ativos/{response.Id}", response);
        }
        catch (CampoObrigatorioException)
        {
            return BadRequest();
        }
        catch (ValorInvalidoException)
        {
            return BadRequest();
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AtivoResponse>>> ListarAtivos()
    {
        var ativos = await _ativoService.ListarAtivos();

        var responses = ativos.Select(ativo =>
        {
            var evolucaoPercentual = _ativoService.CalcularEvolucaoPercentual(ativo.ValorInvestido, ativo.ValorAtual);
            var precoMedio = _ativoService.CalcularPrecoMedio(ativo.ValorInvestido, ativo.Quantidade);
            return AtivoResponse.FromAtivo(ativo, evolucaoPercentual, precoMedio);
        });

        return Ok(responses);
    }

    [HttpPatch("{id}/valor-atual")]
    public async Task<IActionResult> AtualizarValorAtual(Guid id, AtualizarValorAtualRequest request)
    {
        try
        {
            await _ativoService.AtualizarValorAtual(id, request.NovoValorAtual);
            return Ok();
        }
        catch (AtivoNaoEncontradoException)
        {
            return NotFound();
        }
        catch (ValorInvalidoException)
        {
            return BadRequest();
        }
    }

    [HttpPatch("{id}/desativar")]
    public async Task<IActionResult> DesativarAtivo(Guid id)
    {
        try
        {
            await _ativoService.DesativarAtivo(id);
            return Ok();
        }
        catch (AtivoNaoEncontradoException)
        {
            return NotFound();
        }
    }

    [HttpGet("resumo")]
    public async Task<ActionResult<AtivosResumoResponse>> ObterResumo()
    {
        var resumo = await _ativoService.ObterResumo();
        return Ok(resumo);
    }

    [HttpPost("{id}/aportes")]
    public async Task<ActionResult<AtivoAporteResponse>> RegistrarAporte(Guid id, RegistrarAporteRequest request)
    {
        try
        {
            var aporte = await _ativoService.RegistrarAporte(id, request.Quantidade, request.PrecoUnitario, request.Data);
            var response = AtivoAporteResponse.FromAporte(aporte);
            return Created($"/api/ativos/{id}/aportes/{response.Id}", response);
        }
        catch (AtivoNaoEncontradoException)
        {
            return NotFound();
        }
        catch (ValorInvalidoException)
        {
            return BadRequest();
        }
    }

    [HttpGet("{id}/aportes")]
    public async Task<ActionResult<IEnumerable<AtivoAporteResponse>>> ListarAportes(Guid id)
    {
        var aportes = await _ativoService.ListarAportes(id);
        var responses = aportes.Select(AtivoAporteResponse.FromAporte);
        return Ok(responses);
    }
}
