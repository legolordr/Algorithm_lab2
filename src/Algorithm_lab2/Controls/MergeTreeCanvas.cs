using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Algorithm_lab2.PersonalAlgorithms.MergeSort;

namespace Algorithm_lab2.Controls;

/// <summary>
/// Shows merge sort as its recursion tree: every call <c>Sort(left, right)</c> is a box on the row of its depth.
/// Splitting creates two child boxes below; merging fills the parent box from its two sorted children.
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

    private sealed class Node(int left, int right, int depth, int?[] cells)
    {
        public int Left { get; } = left;
        public int Right { get; } = right;
        public int Depth { get; } = depth;
        public int?[] Cells { get; } = cells;
        public bool[] Taken { get; } = new bool[cells.Length];
        public NodeState State { get; set; } = cells.Length == 1 ? NodeState.Sorted : NodeState.Unsorted;

        public bool Contains(int index) => index >= Left && index <= Right;
    }

    private static readonly IBrush BoxBrush = new SolidColorBrush(Color.Parse("#263238"));
    private static readonly IBrush SortedBoxBrush = new SolidColorBrush(Color.Parse("#1E3A2B"));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#ECEFF1"));
    private static readonly IBrush DarkTextBrush = new SolidColorBrush(Color.Parse("#1A1A1A"));
    private static readonly IBrush CompareBrush = new SolidColorBrush(Color.Parse("#FFD54F"));
    private static readonly IBrush FilledBrush = new SolidColorBrush(Color.Parse("#FF8A65"));
    private static readonly IPen ActivePen = new Pen(new SolidColorBrush(Color.Parse("#4FC3F7")), 2);
    private static readonly IPen StackPen = new Pen(new SolidColorBrush(Color.Parse("#804FC3F7")), 1, new DashStyle([4, 3], 0));
    private static readonly IPen SortedPen = new Pen(new SolidColorBrush(Color.Parse("#81C784")), 1);
    private static readonly IPen EmptyCellPen = new Pen(new SolidColorBrush(Color.Parse("#546E7A")), 1, new DashStyle([3, 3], 0));
    private static readonly IPen LinkPen = new Pen(new SolidColorBrush(Color.Parse("#37474F")), 1.5);

    private readonly Dictionary<(int Left, int Right), Node> _nodes = [];
    private int _count;
    private int _levels;
    private Node? _active;
    private (int I, int J)? _compared;
    private int? _filled;

    /// <summary>Shows only the root call with the unsorted input.</summary>
    public void Reset(int[] input)
    {
        _nodes.Clear();
        _count = input.Length;
        _levels = _count <= 1 ? 1 : (int)Math.Ceiling(Math.Log2(_count)) + 1;
        _active = null;
        _compared = null;
        _filled = null;

        if (_count > 0)
            Add(new Node(0, _count - 1, 1, input.Select(v => (int?)v).ToArray()));
        InvalidateVisual();
    }

    public void Apply(MergeSortStep step)
    {
        _compared = null;
        _filled = null;

        switch (step)
        {
            case SplitStep s:
            {
                var parent = _nodes[(s.Left, s.Right)];
                var leftSize = s.Middle - s.Left + 1;
                Add(new Node(s.Left, s.Middle, s.Depth + 1, parent.Cells[..leftSize]));
                Add(new Node(s.Middle + 1, s.Right, s.Depth + 1, parent.Cells[leftSize..]));
                _active = parent;
                break;
            }
            case MergeStep m:
                _active = _nodes[(m.Left, m.Right)];
                Array.Fill(_active.Cells, null);
                _active.State = NodeState.Merging;
                break;
            case CompareStep c:
                _compared = (c.I, c.J);
                break;
            case TakeStep t when _active is not null:
            {
                _active.Cells[t.Target - _active.Left] = t.Value;
                _filled = t.Target;
                var child = Children(_active).First(n => n.Contains(t.Source));
                child.Taken[t.Source - child.Left] = true;
                break;
            }
            case MergedStep merged:
            {
                var node = _nodes[(merged.Left, merged.Right)];
                node.State = NodeState.Sorted;
                foreach (var child in Children(node))
                    child.State = NodeState.Consumed;
                _active = node;
                break;
            }
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_count == 0 || Bounds.Width < 1 || Bounds.Height < 1)
            return;

        var slot = Bounds.Width / _count;
        var rowHeight = Math.Min(90, Bounds.Height / _levels);
        var boxHeight = Math.Min(44, rowHeight * 0.6);
        var fontSize = Math.Clamp(Math.Min(slot, boxHeight) * 0.42, 9, 18);

        Rect BoxRect(Node node) => new(
            node.Left * slot + 3,
            (node.Depth - 1) * rowHeight + (rowHeight - boxHeight) / 2,
            (node.Right - node.Left + 1) * slot - 6,
            boxHeight);

        // Links from every call to its two recursive calls.
        foreach (var node in _nodes.Values)
        {
            foreach (var child in Children(node))
            {
                var from = BoxRect(node);
                var to = BoxRect(child);
                context.DrawLine(LinkPen, new Point(from.Center.X, from.Bottom), new Point(to.Center.X, to.Top));
            }
        }

        foreach (var node in _nodes.Values.OrderBy(n => n.Depth))
        {
            var box = BoxRect(node);
            using (context.PushOpacity(node.State == NodeState.Consumed ? 0.35 : 1))
            {
                context.DrawRectangle(node.State == NodeState.Sorted ? SortedBoxBrush : BoxBrush, PenFor(node), box, 6, 6);
                for (var index = node.Left; index <= node.Right; index++)
                    DrawCell(context, node, index, new Rect(index * slot + 6, box.Top + 4, slot - 12, box.Height - 8), fontSize);
            }
        }
    }

    private void DrawCell(DrawingContext context, Node node, int index, Rect rect, double fontSize)
    {
        var value = node.Cells[index - node.Left];
        if (value is null)
        {
            context.DrawRectangle(null, EmptyCellPen, rect.Deflate(1), 3, 3);
            return;
        }

        var isChildOfActive = _active is not null && node.Depth == _active.Depth + 1 && _active.Contains(node.Left);
        var compared = isChildOfActive && _compared is { } pair && (pair.I == index || pair.J == index);
        var filled = node == _active && _filled == index;

        if (compared || filled)
            context.FillRectangle(compared ? CompareBrush : FilledBrush, rect, 3);

        var text = new FormattedText(value.Value.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, Typeface.Default, fontSize, compared || filled ? DarkTextBrush : TextBrush);
        using (context.PushOpacity(node.Taken[index - node.Left] ? 0.3 : 1))
            context.DrawText(text, new Point(rect.Center.X - text.Width / 2, rect.Center.Y - text.Height / 2));
    }

    private IPen? PenFor(Node node)
    {
        if (node == _active)
            return ActivePen;
        // Calls that contain the active one are still waiting on the call stack.
        if (_active is not null && node.Depth < _active.Depth && node.Contains(_active.Left) && node.State != NodeState.Sorted)
            return StackPen;
        return node.State == NodeState.Sorted ? SortedPen : null;
    }

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
