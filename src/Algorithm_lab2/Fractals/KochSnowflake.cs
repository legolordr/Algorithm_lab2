using Avalonia;
using Avalonia.Media;

namespace Algorithm_lab2.Fractals;

/// <summary>Koch snowflake: each side of a triangle is replaced by four sides with a "bump" in the middle.</summary>
public sealed class KochSnowflake : Fractal
{
    public override string Name => "Снежинка Коха";
    public override string Description =>
        "Отрезок делится на три части, на средней строится равносторонний «зубец», " +
        "и рекурсия повторяется для каждого из четырёх новых отрезков. Начало — треугольник, " +
        "база рекурсии (глубина 0) — отрезок без изменений. Отрезков: 3·4^глубина.";
    public override int MaxDepth => 7;
    public override int DefaultDepth => 4;
    public override (Color From, Color To) Palette => (Color.Parse("#4FC3F7"), Color.Parse("#4FC3F7"));

    protected override void Generate(Size area, int depth, FractalBuilder builder)
    {
        // The finished snowflake is as wide as the triangle's side and 2/sqrt(3) times as tall.
        var side = Math.Min(area.Width * 0.9, area.Height * 0.9 * Math.Sqrt(3) / 2);
        var height = side * Math.Sqrt(3) / 2;
        var center = new Point(area.Width / 2, area.Height / 2);

        // Vertices go clockwise on screen, so the "bump" must be built to the left of each side.
        var top = new Point(center.X, center.Y - height * 2 / 3);
        var right = new Point(center.X + side / 2, center.Y + height / 3);
        var left = new Point(center.X - side / 2, center.Y + height / 3);

        var points = new List<Point>();
        Koch(builder, top, right, depth, points);
        Koch(builder, right, left, depth, points);
        Koch(builder, left, top, depth, points);
        builder.Polygon(0, points.ToArray());
    }

    /// <summary>
    /// Adds the Koch curve from <paramref name="a"/> to <paramref name="b"/> to <paramref name="points"/>.
    /// Only the start of every final segment is added: its end is the start of the next one.
    /// </summary>
    private static void Koch(FractalBuilder builder, Point a, Point b, int depth, List<Point> points)
    {
        builder.Enter();
        if (depth == 0)
        {
            points.Add(a);
            return;
        }

        var third = (b - a) / 3;
        var p1 = a + third;
        var p3 = a + third * 2;
        // Peak of the bump: the middle third turned by 60 degrees around p1.
        var peak = p1 + (Vector)Rotate(third, -60);

        Koch(builder, a, p1, depth - 1, points);
        Koch(builder, p1, peak, depth - 1, points);
        Koch(builder, peak, p3, depth - 1, points);
        Koch(builder, p3, b, depth - 1, points);
    }
}
