namespace Algorithm_lab2.Views;

/// <summary>Formats a measured duration for the "Время расчёта" statistic on every page.</summary>
internal static class TimeFormat
{
    // Small inputs finish in microseconds; "0,00 мс" would hide the difference between them.
    public static string Format(TimeSpan elapsed) => elapsed.TotalMilliseconds < 0.1
        ? $"{elapsed.TotalMicroseconds:F1} мкс"
        : $"{elapsed.TotalMilliseconds:F2} мс";
}
