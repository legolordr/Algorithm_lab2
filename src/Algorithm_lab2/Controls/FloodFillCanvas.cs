namespace Algorithm_lab2.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;


public class FloodFillCanvas : Control
{
    private int _gridSize = 40;
    private IReadOnlyList<(int X, int Y)> _sequence = [];
    private int _fillCount;

    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.Parse("#0F141A"));
    private static readonly IBrush FilledBrush     = new SolidColorBrush(Color.Parse("#4CAF50"));
    private static readonly IBrush StartBrush      = new SolidColorBrush(Color.Parse("#FFC107"));
    private static readonly IPen   GridPen         = new Pen(new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)), 1);
    private static readonly IPen   BorderPen       = new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 1);

    /// <summary>Нажатие на клетку: аргументы — координаты в сетке (X, Y).</summary>
    public event Action<int, int>? CellPressed;

    public FloodFillCanvas()
    {
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public int GridSize
    {
        get => _gridSize;
        set
        {
            if (_gridSize == value) return;
            _gridSize = value;
            Clear();
        }
    }

    public int FillCount
    {
        get => _fillCount;
        set
        {
            if (_fillCount == value) return;
            _fillCount = value;
            InvalidateVisual();
        }
    }

    public void SetSequence(IReadOnlyList<(int X, int Y)> sequence)
    {
        _sequence = sequence;
        _fillCount = 0;
        InvalidateVisual();
    }

    public void Clear()
    {
        _sequence = [];
        _fillCount = 0;
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);
        var (cellSize, originX, originY) = GetLayout();

        var gx = (int)((pos.X - originX) / cellSize);
        var gy = (int)((pos.Y - originY) / cellSize);

        if (gx >= 0 && gy >= 0 && gx < _gridSize && gy < _gridSize)
        {
            e.Handled = true;
            CellPressed?.Invoke(gx, gy);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var (cellSize, originX, originY) = GetLayout();
        var size = cellSize * _gridSize;

        context.FillRectangle(BackgroundBrush, new Rect(originX, originY, size, size));

        // Сетка
        if (cellSize >= 3)
        {
            for (int i = 1; i < _gridSize; i++)
            {
                var x = originX + i * cellSize;
                var y = originY + i * cellSize;
                context.DrawLine(GridPen, new Point(x, originY), new Point(x, originY + size));
                context.DrawLine(GridPen, new Point(originX, y), new Point(originX + size, y));
            }
        }

        // Закрашенные клетки
        var count = Math.Min(_fillCount, _sequence.Count);
        for (int i = 0; i < count; i++)
        {
            var (cx, cy) = _sequence[i];
            var rect = new Rect(originX + cx * cellSize, originY + cy * cellSize, cellSize, cellSize);
            context.FillRectangle(i == 0 ? StartBrush : FilledBrush, rect);
        }

        // Рамка
        context.DrawRectangle(null, BorderPen, new Rect(originX, originY, size, size));
    }

    private (double CellSize, double OriginX, double OriginY) GetLayout()
    {
        var available = Math.Min(Bounds.Width, Bounds.Height);
        var cellSize = available / _gridSize;
        var originX = (Bounds.Width - cellSize * _gridSize) / 2;
        var originY = (Bounds.Height - cellSize * _gridSize) / 2;
        return (cellSize, originX, originY);
    }
}