namespace MyFinances.Tests.Helpers;

// Relogio controlavel para testes de ciclo de fatura (TASK-238). Sem pacote novo.
// Fixado ao meio-dia UTC para que a data local continue igual a data definida.
public sealed class RelogioFake : TimeProvider
{
    private DateTimeOffset _agora;

    public RelogioFake(DateOnly data)
    {
        Definir(data);
    }

    public void Definir(DateOnly data)
    {
        _agora = new DateTimeOffset(data.Year, data.Month, data.Day, 12, 0, 0, TimeSpan.Zero);
    }

    public void Avancar(TimeSpan intervalo)
    {
        _agora = _agora.Add(intervalo);
    }

    public override DateTimeOffset GetUtcNow() => _agora;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
