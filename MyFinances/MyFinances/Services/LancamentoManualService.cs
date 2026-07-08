using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Models;

namespace MyFinances.Services;

public class LancamentoManualService
{
    private readonly AppDbContext _context;

    public LancamentoManualService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Sucesso, Lancamento? Lancamento, string? Erro)> CriarLancamentoAsync(
        Guid contaId,
        CriarLancamentoRequest request)
    {
        var conta = await _context.Contas.FirstOrDefaultAsync(c => c.Id == contaId);
        if (conta == null)
        {
            return (false, null, "Conta nao encontrada");
        }

        var validacaoConta = ValidarContaOrigemManual(conta);
        if (!validacaoConta.Valido)
        {
            return (false, null, validacaoConta.Erro);
        }

        var validacaoRequest = ValidarRequest(request.Tipo, request.Status, request.Valor, request.Descricao);
        if (!validacaoRequest.Valido)
        {
            return (false, null, validacaoRequest.Erro);
        }

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta,
            CategoriaId = request.CategoriaId,
            Descricao = request.Descricao,
            Valor = request.Valor,
            Tipo = request.Tipo,
            Data = request.Data,
            Status = request.Status,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = null,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };

        _context.Lancamentos.Add(lancamento);
        await _context.SaveChangesAsync();

        return (true, lancamento, null);
    }

    public async Task<(bool Sucesso, Lancamento? Lancamento, string? Erro)> EditarLancamentoAsync(
        Guid contaId,
        Guid lancamentoId,
        EditarLancamentoRequest request)
    {
        var conta = await _context.Contas.FirstOrDefaultAsync(c => c.Id == contaId);
        if (conta == null)
        {
            return (false, null, "Conta nao encontrada");
        }

        var validacaoConta = ValidarContaOrigemManual(conta);
        if (!validacaoConta.Valido)
        {
            return (false, null, validacaoConta.Erro);
        }

        var validacaoRequest = ValidarRequest(request.Tipo, request.Status, request.Valor, request.Descricao);
        if (!validacaoRequest.Valido)
        {
            return (false, null, validacaoRequest.Erro);
        }

        var lancamento = await _context.Lancamentos
            .FirstOrDefaultAsync(l => l.Id == lancamentoId && l.ContaId == contaId);

        if (lancamento == null)
        {
            return (false, null, "Lancamento nao encontrado");
        }

        lancamento.Tipo = request.Tipo;
        lancamento.CategoriaId = request.CategoriaId;
        lancamento.Descricao = request.Descricao;
        lancamento.Valor = request.Valor;
        lancamento.Data = request.Data;
        lancamento.Status = request.Status;

        _context.Lancamentos.Update(lancamento);
        await _context.SaveChangesAsync();

        return (true, lancamento, null);
    }

    public async Task<List<LancamentoResponseDto>> ListarLancamentosAsync(
        Guid contaId,
        string? status = null)
    {
        var query = _context.Lancamentos
            .Where(l => l.ContaId == contaId)
            .Where(l => l.Manual)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        var lancamentos = await query
            .OrderByDescending(l => l.Data)
            .Select(l => new LancamentoResponseDto
            {
                Id = l.Id,
                ContaId = l.ContaId,
                CategoriaId = l.CategoriaId,
                Descricao = l.Descricao,
                Valor = l.Valor,
                Tipo = l.Tipo,
                Data = l.Data,
                Status = l.Status,
                Manual = l.Manual
            })
            .ToListAsync();

        return lancamentos;
    }

    public async Task<(bool Sucesso, string? Erro)> ExcluirLancamentoAsync(
        Guid contaId,
        Guid lancamentoId)
    {
        var conta = await _context.Contas.FirstOrDefaultAsync(c => c.Id == contaId);
        if (conta == null)
        {
            return (false, "Conta nao encontrada");
        }

        var validacaoConta = ValidarContaOrigemManual(conta);
        if (!validacaoConta.Valido)
        {
            return (false, validacaoConta.Erro);
        }

        var lancamento = await _context.Lancamentos
            .FirstOrDefaultAsync(l => l.Id == lancamentoId && l.ContaId == contaId);

        if (lancamento == null)
        {
            return (false, "Lancamento nao encontrado");
        }

        var podeExcluir = PodeExcluirLancamento(lancamento);
        if (!podeExcluir.Pode)
        {
            return (false, podeExcluir.Motivo);
        }

        _context.Lancamentos.Remove(lancamento);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    private (bool Valido, string? Erro) ValidarContaOrigemManual(Conta conta)
    {
        if (conta.Origem != OrigemConstants.Manual)
        {
            return (false, "Operacao permitida apenas em contas com origem MANUAL");
        }

        return (true, null);
    }

    private (bool Valido, string? Erro) ValidarRequest(string tipo, string status, decimal valor, string descricao)
    {
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return (false, "Tipo é obrigatório");
        }

        if (!TipoValido(tipo))
        {
            return (false, $"Tipo '{tipo}' inválido. Valores aceitos: DEBIT, CREDIT");
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            return (false, "Status é obrigatório");
        }

        if (!StatusValido(status))
        {
            return (false, $"Status '{status}' inválido. Valores aceitos: PENDENTE, PAGO");
        }

        if (valor <= 0)
        {
            return (false, "Valor deve ser maior que zero");
        }

        if (string.IsNullOrWhiteSpace(descricao))
        {
            return (false, "Descricao é obrigatória");
        }

        return (true, null);
    }

    private bool TipoValido(string tipo)
    {
        return tipo == TipoLancamentoConstants.Debit || tipo == TipoLancamentoConstants.Credit;
    }

    private bool StatusValido(string status)
    {
        return status == LancamentoStatusConstants.Pendente || status == LancamentoStatusConstants.Pago;
    }

    private (bool Pode, string? Motivo) PodeExcluirLancamento(Lancamento lancamento)
    {
        if (lancamento.TransferenciaId.HasValue)
        {
            return (false, "Lancamento vinculado a transferencia nao pode ser excluído");
        }

        if (lancamento.FaturaId.HasValue)
        {
            return (false, "Lancamento vinculado a fatura nao pode ser excluído");
        }

        if (lancamento.ConciliadoCom.HasValue)
        {
            return (false, "Lancamento conciliado nao pode ser excluído");
        }

        return (true, null);
    }
}
