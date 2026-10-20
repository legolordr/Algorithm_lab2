using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Algorithm_lab2.Fractals;
using Algorithm_lab2.Theme;

namespace Algorithm_lab2.Controls;

/// <summary>
/// Builds the Koch snowflake one recursive split at a time. Each split is animated in two phases:
/// the segment is highlighted and marked at its thirds, then the middle third lifts into the bump.
/// </summary>
public sealed class KochStepCanvas : Control
{
    /// <summary>The trace is computed in this size and scaled to the control, so resizing needs no new trace.</summary>
    public static readonly Size ReferenceSize = new(1000, 1000);

    // Share of the animation spent marking the thirds before the bump starts to grow.
    private const double MarkPhase = 0.35;

    private static readonly IBrush FillBrush = Palette.Brush(Color.FromArgb(40, Palette.VizActive.R, Palette.VizActive.G, Palette.VizActive.B));
    private static readonly IPen OutlinePen = new Pen(Palette.Brush(Palette.VizText), 1.5, lineJoin: PenLineJoin.Round);
    private static readonly IPen SegmentPen = new Pen(Palette.Brush(Palette.VizCompare), 3, lineCap: PenLineCap.Round);
    private static readonly IPen BumpPen = new Pen(Palette.Brush(Palette.VizWrite), 3, lineCap: PenLineCap.Round);
    private static readonly IBrush MarkBrush = Palette.Brush(Palette.VizCompare);
    private static readonly IBrush PeakBrush = Palette.Brush(Palette.VizWrite);

    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly List<Point> _figure = [];
    private KochSplit? _current;
    private int _currentIndex;
    private double _startedAt;

    public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Shows the starting triangle.</summary>
    public void Reset(KochTrace trace)
    {
        _figure.Clear();
        _figure.AddRange(trace.Triangle);
        _current = null;
        InvalidateVisual();
    }

    /// <summary>Replaces segment A → B with A → P1 → Peak → P3 → B and animates the change.</summary>
    public void Apply(KochSplit split)
    {
        // Splits come in recursion order, so A is always the start of a segment that is on the figure now.
        var index = _figure.IndexOf(split.A);
        _figure.InsertRange(index + 1, [split.P1, split.Peak, split.P3]);
        _current = split;
        _currentIndex = index;
        _startedAt = _clock.Elapsed.TotalSeconds;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_figure.Count < 3 || Bounds.Width < 1 || Bounds.Height < 1)
            return;

        var scale = Math.Min(Bounds.Width / ReferenceSize.Width, Bounds.Height / ReferenceSize.Height);
        var offset = new Point(
            (Bounds.Width - ReferenceSize.Width * scale) / 2,
            (Bounds.Height - ReferenceSize.Height * scale) / 2);
        Point Map(Point p) => new(offset.X + p.X * scale, offset.Y + p.Y * scale);

        var t = Math.Clamp((_clock.Elapsed.TotalSeconds - _startedAt) / AnimationDuration.TotalSeconds, 0, 1);
        var mark = Ease(Math.Clamp(t / MarkPhase, 0, 1));
        var lift = Ease(Math.Clamp((t - MarkPhase) / (1 - MarkPhase), 0, 1));

        // While the bump grows its tip moves from the middle of the segment to the real peak.
        var points = _figure.Select(Map).ToArray();
        Point? peak = null;
        if (_current is { } split)
        {
            var middle = new Point((split.P1.X + split.P3.X) / 2, (split.P1.Y + split.P3.Y) / 2);
            peak = Map(new Point(Lerp(middle.X, split.Peak.X, lift), Lerp(middle.Y, split.Peak.Y, lift)));
            points[_currentIndex + 2] = peak.Value;
        }

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(points[0], true);
            foreach (var point in points.Skip(1))
                stream.LineTo(point);
            stream.EndFigure(true);
        }
        context.DrawGeometry(FillBrush, OutlinePen, geometry);

        if (_current is { } s && peak is { } tip)
        {
            Point a = Map(s.A), p1 = Map(s.P1), p3 = Map(s.P3), b = Map(s.B);

            // The two outer thirds stay on the old line, the two sides of the bump are new.
            context.DrawLine(SegmentPen, a, p1);
            context.DrawLine(SegmentPen, p3, b);
            if (lift > 0)
            {
                context.DrawLine(BumpPen, p1, tip);
                context.DrawLine(BumpPen, tip, p3);
            }
            else
            {
                context.DrawLine(SegmentPen, p1, p3);
            }

            var radius = Math.Clamp(Distance(a, b) / 18, 2.5, 6);
            using (context.PushOpacity(mark))
            {
                context.DrawEllipse(MarkBrush, null, p1, radius, radius);
                context.DrawEllipse(MarkBrush, null, p3, radius, radius);
            }
            if (lift > 0)
                context.DrawEllipse(PeakBrush, null, tip, radius, radius);
        }

        if (t < 1)
            TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => InvalidateVisual());
    }

    private static double Ease(double t) => 1 - Math.Pow(1 - t, 3);

    private static double Lerp(double from, double to, double t) => from + (to - from) * t;

    private static double Distance(Point p, Point q) => Math.Sqrt((p.X - q.X) * (p.X - q.X) + (p.Y - q.Y) * (p.Y - q.Y));
}
