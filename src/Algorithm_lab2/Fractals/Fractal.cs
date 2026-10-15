using Avalonia;
using Avalonia.Media;

namespace Algorithm_lab2.Fractals;

/// <summary>A polygon (closed) or polyline (open) produced by a fractal.</summary>
public sealed record FractalShape(Point[] Points, int Level, bool Closed, double Thickness = 1);

/// <summary>Everything a fractal produced for one depth, plus how many recursive calls it took.</summary>
public sealed record FractalResult(IReadOnlyList<FractalShape> Shapes, long Calls);

/// <summary>Collects shapes and counts recursive calls while a fractal is being generated.</summary>
public sealed class FractalBuilder
{
    private readonly List<FractalShape> _shapes = [];

    public long Calls { get; private set; }

    /// <summary>Must be called at the start of every recursive call.</summary>
    public void Enter() => Calls++;

    public void Line(Point from, Point to, int level, double thickness = 1) =>
        _shapes.Add(new FractalShape([from, to], level, false, thickness));

    public void Polyline(Point[] points, int level, double thickness = 1) =>
        _shapes.Add(new FractalShape(points, level, false, thickness));

    public void Polygon(int level, params Point[] points) =>
        _shapes.Add(new FractalShape(points, level, true));

    public FractalResult ToResult() => new(_shapes, Calls);
}

public abstract class Fractal
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract int MaxDepth { get; }
    public virtual int DefaultDepth => Math.Min(5, MaxDepth);

    /// <summary>Colors for the lowest and the highest level; levels in between are interpolated.</summary>
    public virtual (Color From, Color To) Palette => (Color.Parse("#4FC3F7"), Color.Parse("#BA68C8"));

    public FractalResult Build(Size area, int depth)
    {
        var builder = new FractalBuilder();
        Generate(area, Math.Clamp(depth, 0, MaxDepth), builder);
        return builder.ToResult();
    }

    protected abstract void Generate(Size area, int depth, FractalBuilder builder);

    public override string ToString() => Name;

    protected static Point Rotate(Vector v, double degrees)
    {
        var rad = degrees * Math.PI / 180;
        var (sin, cos) = Math.SinCos(rad);
        return new Point(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
