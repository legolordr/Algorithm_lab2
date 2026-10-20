using ScottPlot;
using ScottPlot.Avalonia;
using Algorithm_lab2.Benchmarks;
using Palette = Algorithm_lab2.Theme.Palette;

namespace Algorithm_lab2.Controls;

/// <summary>Draws a benchmark result on a ScottPlot chart in the app's dark palette.</summary>
public static class BenchmarkPlot
{
    /// <summary>Picks the time unit from the largest value so the axis numbers stay readable (1..1000).</summary>
    public static (string Unit, double PerMillisecond) PickUnit(double maxMilliseconds) => maxMilliseconds switch
    {
        < 0.001 => ("нс", 1_000_000),
        < 1 => ("мкс", 1_000),
        < 1_000 => ("мс", 1),
        _ => ("с", 0.001),
    };

    public static void Show(AvaPlot view, BenchmarkResult? result)
    {
        var plot = view.Plot;
        plot.Clear();
        ApplyTheme(plot);

        if (result is null || result.Points.Count == 0)
        {
            view.Refresh();
            return;
        }

        var benchmark = result.Case;
        var xs = result.Points.Select(p => (double)p.N).ToArray();
        var fitMs = result.Points.Select(p => result.C * benchmark.Theory(p.N)).ToArray();

        var (unit, scale) = PickUnit(Math.Max(result.Points.Max(p => p.Milliseconds), fitMs.Max()));

        var fit = plot.Add.ScatterLine(xs, fitMs.Select(t => t * scale).ToArray());
        fit.Color = ToScott(Palette.VizWrite);
        fit.LineWidth = 2;
        fit.LinePattern = LinePattern.Dashed;
        fit.LegendText = $"Теория: C·f(n), {benchmark.Complexity}";

        var measured = plot.Add.Scatter(xs, result.Points.Select(p => p.Milliseconds * scale).ToArray());
        measured.Color = ToScott(Palette.VizActive);
        measured.LineWidth = 1.5f;
        measured.MarkerSize = 7;
        measured.LegendText = "Эксперимент";

        plot.XLabel(benchmark.SizeLabel);
        plot.YLabel($"Время одного запуска, {unit}");
        plot.ShowLegend(Alignment.UpperLeft);
        plot.Axes.AutoScale();
        view.Refresh();
    }

    private static void ApplyTheme(Plot plot)
    {
        plot.Font.Automatic();   // picks a font that has Cyrillic letters for the labels
        plot.FigureBackground.Color = ToScott(Palette.Canvas);
        plot.DataBackground.Color = ToScott(Palette.Canvas);
        plot.Axes.Color(ToScott(Palette.MutedForeground));
        plot.Grid.MajorLineColor = ToScott(Palette.Border);
        plot.Legend.BackgroundColor = ToScott(Palette.Surface);
        plot.Legend.FontColor = ToScott(Palette.Foreground);
        plot.Legend.OutlineColor = ToScott(Palette.Border);
    }

    private static ScottPlot.Color ToScott(Avalonia.Media.Color color) => new(color.R, color.G, color.B, color.A);
}
