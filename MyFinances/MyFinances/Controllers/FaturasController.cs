using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.DTOs;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/cartoes/{contaId}/faturas")]
public class FaturasController : ControllerBase
{
    private readonly AppDbContext _context;

    public FaturasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FaturaResponseDto>>> ListarFaturas(Guid contaId)
    {
        var faturas = await _context.Faturas
            .Where(f => f.ContaId == contaId)
            .OrderByDescending(f => f.DataFechamento)
            .ToListAsync();

        var dto = faturas.Select(f => new FaturaResponseDto
        {
            Id = f.Id,
            ContaId = f.ContaId,
            DataFechamento = f.DataFechamento,
            DataVencimento = f.DataVencimento,
            Status = f.Status,
            TransferenciaId = f.TransferenciaId
        });

        return Ok(dto);
    }
}
