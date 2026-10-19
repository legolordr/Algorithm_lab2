namespace Algorithm_lab2.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Algorithm_lab2.PersonalAlgorithms.Maze;


public class MazeCanvas : Control
{
    private int _width;
    private int _height;
    private bool[,] _rightRemoved = new bool[0, 0];
    private bool[,] _downRemoved  = new bool[0, 0];
    private bool[,] _visited      = new bool[0, 0];
    private (int X, int Y)? _cursor;

    private static readonly IBrush BackgroundBrush   = new SolidColorBrush(Color.Parse("#0F141A"));
    private static readonly IBrush CellUnvisitedBrush = new SolidColorBrush(Color.Parse("#1A1A22"));
    private static readonly IBrush CellVisitedBrush   = new SolidColorBrush(Color.Parse("#1E3A2A"));
    private static readonly IBrush CursorBrush        = new SolidColorBrush(Color.Parse("#FFC107"));
    private static readonly IPen   WallPen  = new Pen(new SolidColorBrush(Color.Parse("#E6FFFFFF")), 1.2);
    private static readonly IPen   BorderPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 1);

    public MazeCanvas()
    {
        ClipToBounds = true;
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _rightRemoved = new bool[width, height];
        _downRemoved  = new bool[width, height];
        _visited      = new bool[width, height];
        _cursor = null;
        InvalidateVisual();
    }

    public void ResetAnimation()
    {
        if (_width == 0 || _height == 0) return;
        Array.Clear(_rightRemoved);
        Array.Clear(_downRemoved);
        Array.Clear(_visited);
        _cursor = null;
        InvalidateVisual();
    }

    /// <summary>Применить один шаг рекурсии к визуальному состоянию.</summary>
    public void ApplyStep(MazeStep step)
    {
        if ((uint)step.X >= (uint)_width || (uint)step.Y >= (uint)_height) return;

        if (step.Kind == MazeStepKind.Enter)
        {
            _visited[step.X, step.Y] = true;

            if (step.FromDirection is { } from)
            {
                switch (from)
                {
                    case Direction.Up:    if (step.Y > 0)          _downRemoved[step.X, step.Y - 1] = true; break;
                    case Direction.Down:  if (step.Y < _height - 1) _downRemoved[step.X, step.Y]     = true; break;
                    case Direction.Left:  if (step.X > 0)          _rightRemoved[step.X - 1, step.Y] = true; break;
                    case Direction.Right: if (step.X < _width - 1)  _rightRemoved[step.X, step.Y]     = true; break;
                }
            }
            _cursor = (step.X, step.Y);
        }
        else if (step.Kind == MazeStepKind.Backtrack)
        {
            _cursor = (step.X, step.Y);
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_width == 0 || _height == 0) return;

        var avail   = Math.Min(Bounds.Width, Bounds.Height);
        var cell    = avail / Math.Max(_width, _height);
        var originX = (Bounds.Width  - cell * _width)  / 2;
        var originY = (Bounds.Height - cell * _height) / 2;

        context.FillRectangle(BackgroundBrush,
            new Rect(originX, originY, cell * _width, cell * _height));

        // Клетки
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
            {
                var brush = _visited[x, y] ? CellVisitedBrush : CellUnvisitedBrush;
                context.FillRectangle(brush,
                    new Rect(originX + x * cell, originY + y * cell, cell, cell));
            }

        // Курсор
        if (_cursor is { } c)
        {
            var rect = new Rect(
                originX + c.X * cell + 1, originY + c.Y * cell + 1,
                Math.Max(1, cell - 2), Math.Max(1, cell - 2));
            context.FillRectangle(CursorBrush, rect);
        }

        // Внутренние стены: рисуем верхнюю и левую грань каждой клетки
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
            {
                bool top  = y == 0 || !_downRemoved[x, y - 1];
                bool left = x == 0 || !_rightRemoved[x - 1, y];

                if (top)
                    context.DrawLine(WallPen,
                        new Point(originX + x * cell, originY + y * cell),
                        new Point(originX + (x + 1) * cell, originY + y * cell));
                if (left)
                    context.DrawLine(WallPen,
                        new Point(originX + x * cell, originY + y * cell),
                        new Point(originX + x * cell, originY + (y + 1) * cell));
            }

        // Внешние правая и нижняя границы
        for (int y = 0; y < _height; y++)
            context.DrawLine(WallPen,
                new Point(originX + _width * cell, originY + y * cell),
                new Point(originX + _width * cell, originY + (y + 1) * cell));
        for (int x = 0; x < _width; x++)
            context.DrawLine(WallPen,
                new Point(originX + x * cell, originY + _height * cell),
                new Point(originX + (x + 1) * cell, originY + _height * cell));

        context.DrawRectangle(null, BorderPen,
            new Rect(originX, originY, cell * _width, cell * _height));
    }
}