using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Algorithm_lab2.Fractals;

namespace Algorithm_lab2.Controls;

public sealed class FractalBuiltEventArgs(FractalResult result, TimeSpan elapsed) : EventArgs
{
    public FractalResult Result { get; } = result;
    public TimeSpan Elapsed { get; } = elapsed;
}

/// <summary>Builds the selected fractal for the current size and depth and draws it.</summary>
public sealed class FractalCanvas : Control
{
    public static readonly StyledProperty<Fractal?> FractalProperty =
        AvaloniaProperty.Register<FractalCanvas, Fractal?>(nameof(Fractal));

    public static readonly StyledProperty<int> DepthProperty =
        AvaloniaProperty.Register<FractalCanvas, int>(nameof(Depth));

    // Shapes of one level share a color, so each level becomes one geometry: tens of thousands of shapes
    // are drawn with a handful of draw calls.
    private sealed record Layer(Geometry Geometry, IBrush? Fill, IPen? Pen);

    private List<Layer> _layers = [];

    public Fractal? Fractal
    {
        get => GetValue(FractalProperty);
        set => SetValue(FractalProperty, value);
    }

    public int Depth
    {
        get => GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    public event EventHandler<FractalBuiltEventArgs>? Built;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FractalProperty || change.Property == DepthProperty ||
            (change.Property == BoundsProperty && Bounds.Size != ((Rect?)change.OldValue)?.Size))
            Rebuild();
    }

    private void Rebuild()
    {
        _layers = [];
        if (Fractal is not { } fractal || Bounds.Width < 1 || Bounds.Height < 1)
        {
            InvalidateVisual();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var result = fractal.Build(Bounds.Size, Depth);
        stopwatch.Stop();

        var maxLevel = result.Shapes.Count == 0 ? 0 : result.Shapes.Max(s => s.Level);
        var (from, to) = fractal.Palette;

        foreach (var group in result.Shapes.GroupBy(s => (s.Level, s.Closed, s.Thickness)))
        {
            var color = Lerp(from, to, maxLevel == 0 ? 0 : (double)group.Key.Level / maxLevel);
            var brush = new SolidColorBrush(color);
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                foreach (var shape in group)
                {
                    context.BeginFigure(shape.Points[0], shape.Closed);
                    for (var i = 1; i < shape.Points.Length; i++)
                        context.LineTo(shape.Points[i]);
                    context.EndFigure(shape.Closed);
                }
            }

            _layers.Add(group.Key.Closed
                ? new Layer(geometry, brush, null)
                : new Layer(geometry, null, new Pen(brush, group.Key.Thickness, lineCap: PenLineCap.Round)));
        }

        InvalidateVisual();
        Built?.Invoke(this, new FractalBuiltEventArgs(result, stopwatch.Elapsed));
    }

    public override void Render(DrawingContext context)
    {
        foreach (var layer in _layers)
            context.DrawGeometry(layer.Fill, layer.Pen, layer.Geometry);
    }

    private static Color Lerp(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + (b.R - a.R) * t),
        (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t));
}
