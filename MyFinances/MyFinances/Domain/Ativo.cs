namespace MyFinances.Domain;

public class Ativo
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoAtivo Tipo { get; set; }

    // Legado: so ativos cadastrados antes de 2026-10-08. Ativo novo nao preenche (item 8.5).
    public string? Instituicao { get; set; }

    // Conta de investimento dona do ativo (item 8.5). Null so em ativo antigo ainda
    // nao vinculado; obrigatorio no cadastro de ativo novo. Definido, nunca volta a null.
    public Guid? ContaId { get; set; }

    // Total de unidades/cotas em carteira, soma de todos os aportes
    // (regra-de-negocio.md item 8.1). Incrementado a cada RegistrarAporte.
    public decimal Quantidade { get; set; }

    public decimal ValorInvestido { get; set; }

    public decimal ValorAtual { get; set; }

    public DateOnly DataCompra { get; set; }

    public bool Ativa { get; set; } = true;

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    // Navegacao
    public Conta? Conta { get; set; }

    public ICollection<Rendimento> Rendimentos { get; set; } = [];
}
