using Algorithm_lab2.Fractals;
using Avalonia;

namespace Algorithm_lab2.Tests;

public class KochSnowflakeTests
{
    private static readonly Size Area = new(800, 600);

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 12)]
    [InlineData(4, 768)]
    [InlineData(7, 49152)]
    public void HasThreeTimesFourToThePowerDepthSides(int depth, int sides)
    {
        var shape = Assert.Single(new KochSnowflake().Build(Area, depth).Shapes);

        Assert.True(shape.Closed);
        Assert.Equal(sides, shape.Points.Length);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 15)]
    [InlineData(4, 1023)]
    public void MakesFourToThePowerDepthPlusOneMinusOneCalls(int depth, long calls) =>
        // Each of the 3 sides is a full 4-ary call tree: 3 · (1 + 4 + ... + 4^depth) = 4^(depth + 1) - 1.
        Assert.Equal(calls, new KochSnowflake().Build(Area, depth).Calls);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void AllSidesHaveTheSameLength(int depth)
    {
        var points = new KochSnowflake().Build(Area, depth).Shapes[0].Points;
        var lengths = Sides(points).Select(s => ((Vector)(s.To - s.From)).Length).ToArray();

        Assert.All(lengths, length => Assert.Equal(lengths[0], length, 6));
    }

    [Fact]
    public void EachLevelMakesSidesThreeTimesShorter()
    {
        var fractal = new KochSnowflake();
        double SideLength(int depth)
        {
            var points = fractal.Build(Area, depth).Shapes[0].Points;
            return ((Vector)(points[1] - points[0])).Length;
        }

        Assert.Equal(SideLength(2) / 3, SideLength(3), 6);
    }

    [Fact]
    public void BumpsPointOutward_AreaGrowsWithDepth()
    {
        // Outward bumps add area; if they pointed inside, the area would shrink.
        var fractal = new KochSnowflake();
        var areas = Enumerable.Range(0, 5).Select(d => PolygonArea(fractal.Build(Area, d).Shapes[0].Points)).ToArray();

        for (var d = 1; d < areas.Length; d++)
            Assert.True(areas[d] > areas[d - 1], $"depth {d}: {areas[d]} <= {areas[d - 1]}");

        // The limit is 8/5 of the starting triangle.
        Assert.InRange(areas[^1] / areas[0], 1.5, 1.6);
    }

    [Theory]
    [InlineData(800, 600)]
    [InlineData(300, 900)]
    [InlineData(1200, 200)]
    public void StaysInsideArea(double width, double height)
    {
        var area = new Size(width, height);
        var fractal = new KochSnowflake();
        var points = fractal.Build(area, fractal.MaxDepth).Shapes[0].Points;

        Assert.All(points, p =>
        {
            Assert.InRange(p.X, 0, area.Width);
            Assert.InRange(p.Y, 0, area.Height);
        });
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 3)]
    [InlineData(2, 15)]
    [InlineData(4, 255)]
    public void Trace_HasOneSplitPerNonBaseCall(int depth, int splits)
    {
        var fractal = new KochSnowflake();

        // All calls minus the base ones (one per final segment): 4^(d+1) - 1 - 3·4^d = 4^d - 1.
        Assert.Equal(splits, fractal.Trace(Area, depth).Splits.Count);
        Assert.Equal(splits, fractal.Build(Area, depth).Calls - 3 * (int)Math.Pow(4, depth));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void ReplayingSplits_GivesTheSameSnowflakeAsBuild(int depth)
    {
        var fractal = new KochSnowflake();
        var trace = fractal.Trace(Area, depth);
        var figure = trace.Triangle.ToList();

        foreach (var split in trace.Splits)
        {
            var index = figure.IndexOf(split.A);
            Assert.Equal(split.B, figure[(index + 1) % figure.Count]);
            figure.InsertRange(index + 1, [split.P1, split.Peak, split.P3]);
        }

        Assert.Equal(fractal.Build(Area, depth).Shapes[0].Points, figure);
    }

    [Fact]
    public void Trace_StartsWithTheFirstSideAtFullDepth()
    {
        var trace = new KochSnowflake().Trace(Area, 3);
        var first = trace.Splits[0];

        Assert.Equal((trace.Triangle[0], trace.Triangle[1], 3), (first.A, first.B, first.Depth));
        // Recursion goes depth-first: the next split works on the first quarter of that side.
        Assert.Equal((first.A, first.P1, 2), (trace.Splits[1].A, trace.Splits[1].B, trace.Splits[1].Depth));
    }

    private static IEnumerable<(Point From, Point To)> Sides(Point[] points) =>
        points.Select((p, i) => (p, points[(i + 1) % points.Length]));

    // Shoelace formula.
    private static double PolygonArea(Point[] points) =>
        Math.Abs(Sides(points).Sum(s => s.From.X * s.To.Y - s.To.X * s.From.Y)) / 2;
}
