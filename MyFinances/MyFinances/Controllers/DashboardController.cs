using MyFinances.DTOs;
using MyFinances.Services;
using Microsoft.AspNetCore.Mvc;

namespace MyFinances.Controllers;

[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IProjecaoMesService _projecaoMesService;

    public DashboardController(IProjecaoMesService projecaoMesService)
    {
        _projecaoMesService = projecaoMesService;
    }

    [HttpGet("api/dashboard/projecao-mes")]
    public async Task<ActionResult<ProjecaoMesResponse>> CalcularProjecaoDoMes(int ano, int mes)
    {
        var resultado = await _projecaoMesService.CalcularProjecaoDoMes(ano, mes);
        var response = ProjecaoMesResponse.FromResultado(resultado);
        return Ok(response);
    }
}
