using Algorithm_lab2.Fractals;
using Avalonia;

namespace Algorithm_lab2.Tests;

public class FractalTests
{
    private static readonly Size Area = new(800, 600);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 15)]
    public void Builder_CountsCallsAndShapes(int depth, int expected)
    {
        var result = new BinarySplit().Build(Area, depth);

        Assert.Equal(expected, result.Calls);
        Assert.Equal(expected, result.Shapes.Count);
    }

    [Fact]
    public void DepthAboveMaximum_IsClamped()
    {
        var fractal = new BinarySplit();

        Assert.Equal(
            fractal.Build(Area, fractal.MaxDepth).Shapes.Count,
            fractal.Build(Area, fractal.MaxDepth + 3).Shapes.Count);
    }

    /// <summary>Minimal recursive fractal used only to test the shared infrastructure.</summary>
    private sealed class BinarySplit : Fractal
    {
        public override string Name => "Test";
        public override string Description => "";
        public override int MaxDepth => 4;

        protected override void Generate(Size area, int depth, FractalBuilder builder) =>
            Split(builder, new Point(0, area.Height / 2), new Point(area.Width, area.Height / 2), depth);

        private static void Split(FractalBuilder builder, Point a, Point b, int depth)
        {
            builder.Enter();
            builder.Line(a, b, depth);
            if (depth == 0)
                return;

            var mid = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            Split(builder, a, mid, depth - 1);
            Split(builder, mid, b, depth - 1);
        }
    }
}
