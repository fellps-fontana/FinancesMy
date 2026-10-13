using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

// Validacoes compartilhadas de lancamento (manual avulso, item 18; parcelado em
// conta banco, item 17). Mensagens e comportamento preservados do
// LancamentoManualService original.
public class ValidadorLancamentoManual
{
    public const string MensagemContaInativa = "Conta inativa nao pode ser utilizada";

    private readonly IContaRepository _contaRepository;
    private readonly ICategoriaRepository _categoriaRepository;

    public ValidadorLancamentoManual(
        IContaRepository contaRepository,
        ICategoriaRepository categoriaRepository)
    {
        _contaRepository = contaRepository;
        _categoriaRepository = categoriaRepository;
    }

    // Conta existe e esta ativa.
    public async Task<(bool Valido, Conta? Conta, string? Erro)> ValidarContaAtivaAsync(Guid contaId)
    {
        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null)
        {
            return (false, null, "Conta nao encontrada");
        }

        if (!conta.Ativa)
        {
            return (false, null, MensagemContaInativa);
        }

        return (true, conta, null);
    }

    // Conta existe, ativa, Origem Manual e nao Cartao (itens 17 e 18).
    public async Task<(bool Valido, Conta? Conta, string? Erro)> ValidarContaDeLancamentoManualAsync(Guid contaId)
    {
        var validacao = await ValidarContaAtivaAsync(contaId);
        if (!validacao.Valido)
        {
            return validacao;
        }

        if (validacao.Conta!.Origem != OrigemConta.Manual)
        {
            return (false, null, "Lancamento avulso so pode ser feito em conta manual");
        }

        if (validacao.Conta.Tipo == TipoConta.Cartao)
        {
            return (false, null, "Conta de cartao nao aceita lancamento avulso");
        }

        return validacao;
    }

    // Categoria opcional; se informada, existe e e compativel com o tipo do lancamento.
    public async Task<(bool Valido, string? Erro)> ValidarCategoriaDoTipoAsync(Guid? categoriaId, TipoLancamento tipo)
    {
        if (categoriaId == null)
        {
            return (true, null);
        }

        var categoria = await _categoriaRepository.ObterPorId(categoriaId.Value);
        if (categoria == null)
        {
            return (false, "Categoria nao encontrada");
        }

        if (categoria.Tipo != ObterTipoCategoriaDoLancamento(tipo))
        {
            return (false, "Categoria incompativel com o tipo do lancamento: DEBIT aceita so DESPESA, CREDIT aceita so RECEITA");
        }

        return (true, null);
    }

    public static TipoCategoria ObterTipoCategoriaDoLancamento(TipoLancamento tipo) => tipo switch
    {
        TipoLancamento.Debit => TipoCategoria.Despesa,
        TipoLancamento.Credit => TipoCategoria.Receita,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static (bool Valido, string? Erro) ValidarDadosLancamento(string? descricao, decimal valor)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            return (false, "Descricao do lancamento e obrigatoria");
        }

        if (valor <= 0)
        {
            return (false, "Valor do lancamento deve ser maior que zero");
        }

        return (true, null);
    }

    // Criacao/parcelado: status obrigatorio e manual (PENDENTE | PAGO).
    public static (StatusLancamento? Status, string? Erro) ValidarStatusManual(string status)
    {
        var statusParsed = TentarParsearStatus(status);
        if (statusParsed.Status == null)
        {
            return statusParsed;
        }

        if (statusParsed.Status == StatusLancamento.Sugerido)
        {
            return (null, "Status SUGERIDO nao e permitido para lancamentos manuais");
        }

        return statusParsed;
    }

    // Edicao: status omitido/vazio = mantem o atual.
    public static (StatusLancamento? Status, string? Erro) ValidarStatusOpcional(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return (null, null);
        }

        return ValidarStatusManual(status);
    }

    public static (TipoLancamento? Tipo, string? Erro) TentarParsearTipo(string tipo)
    {
        try
        {
            var parsed = TipoLancamentoExtensions.FromStorageValue(tipo);
            return (parsed, null);
        }
        catch
        {
            return (null, $"Tipo invalido: {tipo}. Use DEBIT ou CREDIT");
        }
    }

    private static (StatusLancamento? Status, string? Erro) TentarParsearStatus(string status)
    {
        try
        {
            var parsed = StatusLancamentoExtensions.FromStorageValue(status);
            return (parsed, null);
        }
        catch
        {
            return (null, $"Status invalido: {status}. Use PENDENTE, SUGERIDO ou PAGO");
        }
    }
}
