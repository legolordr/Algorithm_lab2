using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Algorithm_lab2.Theme;
using Algorithm_lab2.PersonalAlgorithms.MergeSort;

namespace Algorithm_lab2.Controls;

/// <summary>
/// Shows merge sort as its recursion tree: every call <c>Sort(left, right)</c> is a box on the row of its depth.
/// Splitting creates two child boxes below; merging fills the parent box from its two sorted children.
/// Every change is animated: new boxes slide out of their parent, taken values fly up into the parent,
/// highlights fade in and finished children fade out.
/// </summary>
public sealed class MergeTreeCanvas : Control
{
    private enum NodeState
    {
        Unsorted,
        Merging,
        Sorted,
        Consumed,
    }

    private sealed class Node(int left, int right, int depth, int?[] cells, Node? parent, double appearedAt)
    {
        public int Left { get; } = left;
        public int Right { get; } = right;
        public int Depth { get; } = depth;
        public int?[] Cells { get; } = cells;
        public bool[] Taken { get; } = new bool[cells.Length];
        public Node? Parent { get; } = parent;
        public double AppearedAt { get; } = appearedAt;
        public NodeState State { get; private set; } = cells.Length == 1 ? NodeState.Sorted : NodeState.Unsorted;
        public double StateChangedAt { get; private set; } = double.NegativeInfinity;

        public bool Contains(int index) => index >= Left && index <= Right;

        public void SetState(NodeState state, double now)
        {
            State = state;
            StateChangedAt = now;
        }
    }

    /// <summary>A value moving from a child's cell to its place in the parent during a merge.</summary>
    private sealed record Flight(int Value, Node From, int Source, Node To, int Target, double StartedAt);

    private const double ConsumedOpacity = 0.35;

    private static readonly Color BoxColor = Palette.VizIdleSurface;
    private static readonly Color SortedBoxColor = Palette.VizDoneSurface;
    private static readonly IBrush TextBrush = Palette.Brush(Palette.VizText);
    private static readonly IBrush DarkTextBrush = Palette.Brush(Palette.OnHighlight);
    private static readonly IBrush CompareBrush = Palette.Brush(Palette.VizCompare);
    private static readonly IBrush FilledBrush = Palette.Brush(Palette.VizWrite);
    private static readonly IPen ActivePen = new Pen(Palette.Brush(Palette.VizActive), 2);
    private static readonly IPen StackPen = new Pen(Palette.Brush(Color.FromArgb(150, Palette.VizActive.R, Palette.VizActive.G, Palette.VizActive.B)), 1, new DashStyle([4, 3], 0));
    private static readonly IPen SortedPen = new Pen(Palette.Brush(Palette.VizDone), 1);
    private static readonly IPen EmptyCellPen = new Pen(Palette.Brush(Palette.BorderStrong), 1, new DashStyle([3, 3], 0));
    private static readonly IPen LinkPen = new Pen(Palette.Brush(Palette.VizIdle), 1.5);

    private readonly Dictionary<(int Left, int Right), Node> _nodes = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private int _count;
    private int _levels;
    private Node? _active;
    private (int I, int J)? _compared;
    private double _comparedAt;
    private int? _filled;
    private Flight? _flight;
    private bool _animating;

    /// <summary>How long one change takes; the page ties it to the playback speed.</summary>
    public TimeSpan AnimationDuration { get; set; } = TimeSpan.FromMilliseconds(250);

    private double Now => _clock.Elapsed.TotalSeconds;

    /// <summary>Shows only the root call with the unsorted input.</summary>
    public void Reset(int[] input)
    {
        _nodes.Clear();
        _count = input.Length;
        _levels = _count <= 1 ? 1 : (int)Math.Ceiling(Math.Log2(_count)) + 1;
        _active = null;
        _compared = null;
        _filled = null;
        _flight = null;

        if (_count > 0)
            Add(new Node(0, _count - 1, 1, input.Select(v => (int?)v).ToArray(), null, double.NegativeInfinity));
        InvalidateVisual();
    }

    public void Apply(MergeSortStep step)
    {
        var now = Now;
        _compared = null;
        _filled = null;

        switch (step)
        {
            case SplitStep s:
            {
                var parent = _nodes[(s.Left, s.Right)];
                var leftSize = s.Middle - s.Left + 1;
                Add(new Node(s.Left, s.Middle, s.Depth + 1, parent.Cells[..leftSize], parent, now));
                Add(new Node(s.Middle + 1, s.Right, s.Depth + 1, parent.Cells[leftSize..], parent, now));
                _active = parent;
                break;
            }
            case MergeStep m:
                _active = _nodes[(m.Left, m.Right)];
                Array.Fill(_active.Cells, null);
                _active.SetState(NodeState.Merging, now);
                break;
            case CompareStep c:
                _compared = (c.I, c.J);
                _comparedAt = now;
                break;
            case TakeStep t when _active is not null:
            {
                _active.Cells[t.Target - _active.Left] = t.Value;
                _filled = t.Target;
                var child = Children(_active).First(n => n.Contains(t.Source));
                child.Taken[t.Source - child.Left] = true;
                _flight = new Flight(t.Value, child, t.Source, _active, t.Target, now);
                break;
            }
            case MergedStep merged:
            {
                var node = _nodes[(merged.Left, merged.Right)];
                node.SetState(NodeState.Sorted, now);
                foreach (var child in Children(node))
                    child.SetState(NodeState.Consumed, now);
                _active = node;
                break;
            }
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        _animating = false;
        if (_count == 0 || Bounds.Width < 1 || Bounds.Height < 1)
            return;

        var slot = Bounds.Width / _count;
        var rowHeight = Math.Min(90, Bounds.Height / _levels);
        var boxHeight = Math.Min(44, rowHeight * 0.6);
        var fontSize = Math.Clamp(Math.Min(slot, boxHeight) * 0.42, 9, 18);

        Rect RestingRect(Node node) => new(
            node.Left * slot + 3,
            (node.Depth - 1) * rowHeight + (rowHeight - boxHeight) / 2,
            (node.Right - node.Left + 1) * slot - 6,
            boxHeight);

        // A new box slides down from its parent's row to its own.
        Rect BoxRect(Node node)
        {
            var rect = RestingRect(node);
            if (node.Parent is null)
                return rect;
            var y = Lerp(RestingRect(node.Parent).Y, rect.Y, Ease(node.AppearedAt));
            return rect.WithY(y);
        }

        Rect CellRect(Node node, int index)
        {
            var box = BoxRect(node);
            return new Rect(index * slot + 6, box.Top + 4, slot - 12, box.Height - 8);
        }

        double NodeOpacity(Node node)
        {
            var appear = node.Parent is null ? 1 : Ease(node.AppearedAt);
            var fade = node.State == NodeState.Consumed ? Lerp(1, ConsumedOpacity, Ease(node.StateChangedAt)) : 1;
            return appear * fade;
        }

        // Links from every call to its two recursive calls.
        foreach (var node in _nodes.Values)
        {
            foreach (var child in Children(node))
            {
                var from = BoxRect(node);
                var to = BoxRect(child);
                using (context.PushOpacity(child.Parent is null ? 1 : Ease(child.AppearedAt)))
                    context.DrawLine(LinkPen, new Point(from.Center.X, from.Bottom), new Point(to.Center.X, to.Top));
            }
        }

        var flightProgress = _flight is null ? 1 : Ease(_flight.StartedAt);

        foreach (var node in _nodes.Values.OrderBy(n => n.Depth))
        {
            var box = BoxRect(node);
            using (context.PushOpacity(NodeOpacity(node)))
            {
                context.DrawRectangle(new SolidColorBrush(BoxFill(node)), PenFor(node), box, 6, 6);
                for (var index = node.Left; index <= node.Right; index++)
                {
                    // The cell a value is flying to stays empty until the value lands.
                    var landing = _flight is not null && flightProgress < 1 && node == _flight.To && index == _flight.Target;
                    DrawCell(context, node, index, CellRect(node, index), fontSize, landing);
                }
            }
        }

        if (_flight is not null && flightProgress < 1)
        {
            var from = CellRect(_flight.From, _flight.Source);
            var to = CellRect(_flight.To, _flight.Target);
            // A small arc reads as "picked up and put down" rather than sliding through other boxes.
            var lift = Math.Sin(Math.PI * flightProgress) * Math.Min(24, boxHeight * 0.6);
            var rect = new Rect(
                Lerp(from.X, to.X, flightProgress),
                Lerp(from.Y, to.Y, flightProgress) - lift,
                to.Width,
                to.Height);
            context.FillRectangle(FilledBrush, rect, 3);
            DrawValue(context, _flight.Value, rect, fontSize, DarkTextBrush);
        }

        if (_animating)
            TopLevel.GetTopLevel(this)?.RequestAnimationFrame(_ => InvalidateVisual());
    }

    private void DrawCell(DrawingContext context, Node node, int index, Rect rect, double fontSize, bool landing)
    {
        var value = node.Cells[index - node.Left];
        if (value is null || landing)
        {
            context.DrawRectangle(null, EmptyCellPen, rect.Deflate(1), 3, 3);
            return;
        }

        var isChildOfActive = _active is not null && node.Depth == _active.Depth + 1 && _active.Contains(node.Left);
        var compared = isChildOfActive && _compared is { } pair && (pair.I == index || pair.J == index);
        var filled = node == _active && _filled == index;

        if (compared || filled)
        {
            using (context.PushOpacity(compared ? Ease(_comparedAt) : 1))
                context.FillRectangle(compared ? CompareBrush : FilledBrush, rect, 3);
        }

        using (context.PushOpacity(node.Taken[index - node.Left] ? 0.3 : 1))
            DrawValue(context, value.Value, rect, fontSize, compared || filled ? DarkTextBrush : TextBrush);
    }

    private static void DrawValue(DrawingContext context, int value, Rect rect, double fontSize, IBrush brush)
    {
        var text = new FormattedText(value.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, Typeface.Default, fontSize, brush);
        context.DrawText(text, new Point(rect.Center.X - text.Width / 2, rect.Center.Y - text.Height / 2));
    }

    /// <summary>A merged box turns from neutral to "sorted" green instead of switching at once.</summary>
    private Color BoxFill(Node node)
    {
        if (node.State is not (NodeState.Sorted or NodeState.Consumed))
            return BoxColor;
        // Leaves are sorted from the start; only boxes that were merged change color.
        if (double.IsNegativeInfinity(node.StateChangedAt) || node.Cells.Length == 1)
            return SortedBoxColor;
        var t = node.State == NodeState.Sorted ? Ease(node.StateChangedAt) : 1;
        return Color.FromRgb(
            (byte)Lerp(BoxColor.R, SortedBoxColor.R, t),
            (byte)Lerp(BoxColor.G, SortedBoxColor.G, t),
            (byte)Lerp(BoxColor.B, SortedBoxColor.B, t));
    }

    private IPen? PenFor(Node node)
    {
        if (node == _active)
            return ActivePen;
        // Calls that contain the active one are still waiting on the call stack.
        if (_active is not null && node.Depth < _active.Depth && node.Contains(_active.Left) && node.State != NodeState.Sorted)
            return StackPen;
        return node.State is NodeState.Sorted or NodeState.Consumed ? SortedPen : null;
    }

    /// <summary>
    /// Eased progress (0..1) of an animation that started at <paramref name="startedAt"/>; also remembers
    /// that another frame is needed while any animation is still running.
    /// </summary>
    private double Ease(double startedAt)
    {
        if (double.IsNegativeInfinity(startedAt))
            return 1;

        var t = Math.Clamp((Now - startedAt) / AnimationDuration.TotalSeconds, 0, 1);
        if (t < 1)
            _animating = true;
        return 1 - Math.Pow(1 - t, 3);   // ease-out: fast start, gentle stop
    }

    private static double Lerp(double from, double to, double t) => from + (to - from) * t;

    private IEnumerable<Node> Children(Node node)
    {
        if (node.Left == node.Right)
            yield break;
        var middle = node.Left + (node.Right - node.Left) / 2;
        if (_nodes.TryGetValue((node.Left, middle), out var left))
            yield return left;
        if (_nodes.TryGetValue((middle + 1, node.Right), out var right))
            yield return right;
    }

    private void Add(Node node) => _nodes[(node.Left, node.Right)] = node;
}
