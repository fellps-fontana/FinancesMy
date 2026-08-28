using Microsoft.AspNetCore.Mvc;
using MyFinances.DTOs.RecebivelRecorrente;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
public class RecebivelRecorrenteController : ControllerBase
{
    private readonly IRecebivelRecorrenteService _service;

    public RecebivelRecorrenteController(IRecebivelRecorrenteService service)
    {
        _service = service;
    }

    [HttpPost("api/recebiveis-recorrentes")]
    public Task<ActionResult<RecebivelRecorrenteResponse>> Criar(CriarRecebivelRecorrenteRequest request)
        => throw new NotImplementedException();

    [HttpPut("api/recebiveis-recorrentes/{id}")]
    public Task<ActionResult<RecebivelRecorrenteResponse>> Editar(Guid id, EditarRecebivelRecorrenteRequest request)
        => throw new NotImplementedException();

    [HttpPost("api/recebiveis-recorrentes/{id}/desativar")]
    public Task<IActionResult> Desativar(Guid id)
        => throw new NotImplementedException();

    [HttpPost("api/recebiveis-recorrentes/{id}/reativar")]
    public Task<IActionResult> Reativar(Guid id)
        => throw new NotImplementedException();

    [HttpGet("api/recebiveis-recorrentes/{id}")]
    public Task<ActionResult<RecebivelRecorrenteResponse>> ObterPorId(Guid id)
        => throw new NotImplementedException();

    [HttpGet("api/recebiveis-recorrentes")]
    public Task<ActionResult<IEnumerable<RecebivelRecorrenteResponse>>> Listar([FromQuery] bool? ativa)
        => throw new NotImplementedException();
}
