using MyFinances.DTOs.Ativo;
using MyFinances.DTOs.Rendimento;
using MyFinances.Exceptions;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/ativos")]
public class AtivosController : ControllerBase
{
    private readonly IAtivoService _ativoService;
    private readonly IRendimentoService _rendimentoService;

    public AtivosController(IAtivoService ativoService, IRendimentoService rendimentoService)
    {
        _ativoService = ativoService;
        _rendimentoService = rendimentoService;
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
                request.ValorInvestido,
                request.DataCompra);

            var evolucaoPercentual = _ativoService.CalcularEvolucaoPercentual(
                ativo.ValorInvestido,
                ativo.ValorAtual);

            var response = AtivoResponse.FromAtivo(ativo, evolucaoPercentual);

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

        var responses = ativos.Select(ativo => AtivoResponse.FromAtivo(
            ativo,
            _ativoService.CalcularEvolucaoPercentual(ativo.ValorInvestido, ativo.ValorAtual)));

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

    [HttpPost("{id}/rendimentos")]
    public async Task<ActionResult<RendimentoResponse>> RegistrarDividendo(Guid id, RegistrarDividendoRequest request)
    {
        try
        {
            var rendimento = await _rendimentoService.RegistrarDividendo(id, request.Valor, request.Data);
            var response = RendimentoResponse.FromRendimento(rendimento);
            return Created($"/api/ativos/{id}/rendimentos/{response.Id}", response);
        }
        catch (AtivoNaoEncontradoException)
        {
            return NotFound();
        }
        catch (AtivoInativoException)
        {
            return NotFound();
        }
        catch (ValorInvalidoException)
        {
            return BadRequest();
        }
    }

    [HttpGet("{id}/rendimentos")]
    public async Task<ActionResult<IEnumerable<RendimentoResponse>>> ObterHistoricoRendimentos(Guid id)
    {
        var rendimentos = await _rendimentoService.ObterHistorico(id);
        var responses = rendimentos.Select(r => RendimentoResponse.FromRendimento(r));
        return Ok(responses);
    }

    [HttpGet("rendimentos-resumo")]
    public async Task<ActionResult<RendimentosResumoResponse>> ObterResumoRendimentos()
    {
        var resumo = await _rendimentoService.ObterResumoGeral();

        var response = new RendimentosResumoResponse
        {
            TotalDividendos = resumo.TotalDividendos,
            TotalValorizacao = resumo.TotalValorizacao,
            Historico = resumo.Historico.Select(r => RendimentoResponse.FromRendimento(r))
        };

        return Ok(response);
    }
}
