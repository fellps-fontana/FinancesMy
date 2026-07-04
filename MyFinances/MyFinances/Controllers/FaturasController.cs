using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Dtos;
using MyFinances.Services;

namespace MyFinances.Controllers;

[ApiController]
[Route("api/cartoes/{contaId}/faturas")]
public class FaturasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PagamentoFaturaService _pagamentoFaturaService;

    public FaturasController(AppDbContext context, PagamentoFaturaService pagamentoFaturaService)
    {
        _context = context;
        _pagamentoFaturaService = pagamentoFaturaService;
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

    [HttpPost("~/api/faturas/{id}/pagamento")]
    public async Task<ActionResult<FaturaResponseDto>> PagarFatura(Guid id, PagarFaturaRequest request)
    {
        var (sucesso, fatura, erro) = await _pagamentoFaturaService.PagarFaturaAsync(id, request);

        if (!sucesso)
        {
            return BadRequest(new { error = erro });
        }

        var dto = new FaturaResponseDto
        {
            Id = fatura!.Id,
            ContaId = fatura.ContaId,
            DataFechamento = fatura.DataFechamento,
            DataVencimento = fatura.DataVencimento,
            Status = fatura.Status,
            TransferenciaId = fatura.TransferenciaId
        };

        return Ok(dto);
    }
}
