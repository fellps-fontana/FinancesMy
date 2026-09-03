namespace MyFinances.Domain;

public enum DiaDaSemana
{
    Segunda,
    Terca,
    Quarta,
    Quinta,
    Sexta,
    Sabado,
    Domingo
}

public static class DiaDaSemanaExtensions
{
    public static string ToStorageValue(this DiaDaSemana dia) => dia switch
    {
        DiaDaSemana.Segunda => "SEG",
        DiaDaSemana.Terca => "TER",
        DiaDaSemana.Quarta => "QUA",
        DiaDaSemana.Quinta => "QUI",
        DiaDaSemana.Sexta => "SEX",
        DiaDaSemana.Sabado => "SAB",
        DiaDaSemana.Domingo => "DOM",
        _ => throw new ArgumentOutOfRangeException(nameof(dia))
    };

    public static DiaDaSemana FromStorageValue(string value) => value switch
    {
        "SEG" => DiaDaSemana.Segunda,
        "TER" => DiaDaSemana.Terca,
        "QUA" => DiaDaSemana.Quarta,
        "QUI" => DiaDaSemana.Quinta,
        "SEX" => DiaDaSemana.Sexta,
        "SAB" => DiaDaSemana.Sabado,
        "DOM" => DiaDaSemana.Domingo,
        _ => throw new ArgumentException($"Valor desconhecido para DiaDaSemana: {value}", nameof(value))
    };

    // Ponte para System.DayOfWeek (usada no calculo da ancora semanal, item 15).
    public static DayOfWeek ToDayOfWeek(this DiaDaSemana dia) => dia switch
    {
        DiaDaSemana.Segunda => DayOfWeek.Monday,
        DiaDaSemana.Terca => DayOfWeek.Tuesday,
        DiaDaSemana.Quarta => DayOfWeek.Wednesday,
        DiaDaSemana.Quinta => DayOfWeek.Thursday,
        DiaDaSemana.Sexta => DayOfWeek.Friday,
        DiaDaSemana.Sabado => DayOfWeek.Saturday,
        DiaDaSemana.Domingo => DayOfWeek.Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(dia))
    };
}
