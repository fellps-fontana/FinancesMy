using MyFinances.DTOs.ContaReceber;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
public class ContasReceberController : ControllerBase
{
    private readonly IContaReceberService _contaReceberService;

    public ContasReceberController(IContaReceberService contaReceberService)
    {
        _contaReceberService = contaReceberService;
    }

    [HttpPost("api/contas-receber/recebiveis")]
    public async Task<ActionResult<ContaReceberResponse>> RegistrarRecebivel(RegistrarRecebivelRequest request)
    {
        var contaReceber = await _contaReceberService.RegistrarRecebivel(
            request.Descricao,
            request.ValorTotal,
            request.DataRegistro,
            request.DataPrevista,
            request.CategoriaId
        );

        var response = ContaReceberResponse.FromContaReceber(contaReceber);
        return Created($"/api/contas-receber/{response.Id}", response);
    }

    [HttpPost("api/contas-receber/emprestimos")]
    public async Task<ActionResult<ContaReceberResponse>> RegistrarEmprestimo(RegistrarEmprestimoRequest request)
    {
        try
        {
            var contaReceber = await _contaReceberService.RegistrarEmprestimo(
                request.Descricao,
                request.Pessoa,
                request.ValorTotal,
                request.ContaOrigemId,
                request.DataRegistro,
                request.DataPrevista,
                request.CategoriaId
            );

            var response = ContaReceberResponse.FromContaReceber(contaReceber);
            return Created($"/api/contas-receber/{response.Id}", response);
        }
        catch (PessoaObrigatoriaParaEmprestimoException ex)
        {
            return UnprocessableEntity(new { erro = ex.Message });
        }
        catch (ContaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }

    [HttpPost("api/contas-receber/{id}/recebimentos")]
    public async Task<ActionResult<RecebimentoResponse>> RegistrarRecebimento(Guid id, RegistrarRecebimentoRequest request)
    {
        try
        {
            var lancamento = await _contaReceberService.RegistrarRecebimento(
                id,
                request.ContaDestinoId,
                request.Valor,
                request.Data,
                request.CategoriaId
            );

            var response = RecebimentoResponse.FromLancamento(lancamento);
            return Ok(response);
        }
        catch (ContaReceberNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (ContaNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
        catch (ValorRecebimentoExcedeSaldoPendenteException ex)
        {
            return UnprocessableEntity(new { erro = ex.Message });
        }
    }

    [HttpGet("api/contas-receber")]
    public async Task<ActionResult<IEnumerable<ContaReceberResponse>>> Listar([FromQuery] string? status)
    {
        try
        {
            var statusFiltro = status == null ? (StatusContaReceber?)null : StatusContaReceberExtensions.FromStorageValue(status);

            var contasReceber = await _contaReceberService.Listar(statusFiltro);
            var response = contasReceber.Select(c => ContaReceberResponse.FromContaReceber(c));
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpGet("api/contas-receber/{id}")]
    public async Task<ActionResult<ContaReceberResponse>> ObterPorId(Guid id)
    {
        try
        {
            var contaReceber = await _contaReceberService.ObterPorId(id);
            var response = ContaReceberResponse.FromContaReceber(contaReceber);
            return Ok(response);
        }
        catch (ContaReceberNaoEncontradaException ex)
        {
            return NotFound(new { erro = ex.Message });
        }
    }
}
