using Microsoft.AspNetCore.Mvc;
using MyFinances.Domain;
using MyFinances.DTOs.PlanejamentoMensal;
using MyFinances.Exceptions;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
public class PlanejamentoMensalController : ControllerBase
{
    private const string MensagemPlanoNaoEncontrado = "Plano mensal não encontrado para o mês informado.";
    private const string MensagemSublimiteNulo = "Sublimites não pode conter itens nulos.";

    private readonly IPlanejamentoMensalService _service;

    public PlanejamentoMensalController(IPlanejamentoMensalService service)
    {
        _service = service;
    }

    [HttpGet("api/planejamento-mensal/{ano:int}/{mes:int}")]
    public async Task<ActionResult<PlanoMensalResponse>> Obter(int ano, int mes)
    {
        try
        {
            var consulta = await _service.ObterAsync(ano, mes);

            if (consulta is null)
                return NotFound(new { erro = MensagemPlanoNaoEncontrado });

            return Ok(PlanoMensalResponse.From(consulta));
        }
        catch (Exception ex) when (ex is ValorInvalidoException or PlanoMensalForaDoHorizonteException)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPut("api/planejamento-mensal/{ano:int}/{mes:int}")]
    public async Task<ActionResult<PlanoMensalResponse>> Salvar(int ano, int mes, SalvarPlanoMensalRequest request)
    {
        if (ContemSublimiteNulo(request.Sublimites))
            return BadRequest(new { erro = MensagemSublimiteNulo });

        try
        {
            var consulta = await _service.SalvarAsync(
                ano,
                mes,
                request.RendaPrevista!.Value,
                request.PercentualInvestimento!.Value,
                MapearSublimites(request.Sublimites));

            return Ok(PlanoMensalResponse.From(consulta));
        }
        catch (Exception ex) when (ex is ValorInvalidoException or PlanoMensalForaDoHorizonteException or RendaPrevistaNegativaException)
        {
            return BadRequest(new { erro = ex.Message });
        }
        catch (CategoriaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (PlanoMensalPassadoSomenteLeituraException ex)
        {
            return Conflict(new { erro = ex.Message });
        }
        catch (Exception ex) when (ex is CategoriaInvalidaParaSublimiteException
                                      or SublimiteDuplicadoException
                                      or SubcategoriaSemSublimiteNoPaiException
                                      or SomaPercentuaisExcedeCemException)
        {
            return UnprocessableEntity(new { erro = ex.Message });
        }
    }

    private static bool ContemSublimiteNulo(IReadOnlyList<SublimiteRequest> sublimites) =>
        sublimites.Any(sublimite => sublimite is null);

    // Chamado so depois de ContemSublimiteNulo e da validacao de model state: CategoriaId e Percentual estao preenchidos.
    private static IReadOnlyList<SublimiteEntrada> MapearSublimites(IReadOnlyList<SublimiteRequest> sublimites) =>
        sublimites
            .Select(sublimite => new SublimiteEntrada(sublimite.CategoriaId!.Value, sublimite.Percentual!.Value))
            .ToList();
}
