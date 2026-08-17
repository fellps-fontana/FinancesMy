namespace MyFinances.Domain;

public static class RendimentoValorizacaoCalculator
{
    public static decimal? Calcular(decimal valorAtualAnterior, decimal valorAtualNovo)
    {
        var delta = valorAtualNovo - valorAtualAnterior;
        return delta == 0 ? null : delta;
    }
}
