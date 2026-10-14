namespace MyFinances.Domain;

// Regra pura do saldo de conta de investimento e do total investido (itens 8.5 e 10).
public static class ContaInvestimentoSaldoCalculator
{
    // true se existe pelo menos um ativo com Ativa = true.
    public static bool PossuiAtivoAtivoVinculado(IEnumerable<Ativo> ativosDaConta)
    {
        return ativosDaConta.Any(a => a.Ativa);
    }

    // Com ativo ativo vinculado: soma de ValorAtual dos ativos ativos (so ContaId == conta.Id).
    // Sem nenhum: conta.SaldoManual ?? 0m (volta a valer o ultimo saldo_manual).
    public static decimal CalcularSaldo(Conta conta, IEnumerable<Ativo> ativosDaConta)
    {
        var ativosAtivosDaConta = ativosDaConta
            .Where(a => a.Ativa && a.ContaId == conta.Id)
            .ToList();

        if (ativosAtivosDaConta.Count == 0)
        {
            return conta.SaldoManual ?? 0m;
        }

        return ativosAtivosDaConta.Sum(a => a.ValorAtual);
    }

    // Soma de CalcularSaldo de cada conta de investimento ativa + ValorAtual dos ativos
    // ativos com ContaId == null. Ativo vinculado nunca e somado fora da conta.
    public static decimal CalcularTotalInvestido(
        IEnumerable<Conta> contasInvestimentoAtivas,
        IEnumerable<Ativo> ativosAtivos)
    {
        var ativos = ativosAtivos.Where(a => a.Ativa).ToList();

        var totalContas = contasInvestimentoAtivas
            .Where(c => c.Ativa)
            .Sum(c => CalcularSaldo(c, ativos));

        var totalAtivosSemConta = ativos
            .Where(a => a.ContaId == null)
            .Sum(a => a.ValorAtual);

        return totalContas + totalAtivosSemConta;
    }
}
