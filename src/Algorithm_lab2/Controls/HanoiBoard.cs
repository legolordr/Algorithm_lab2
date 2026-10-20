using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Algorithm_lab2.Theme;
using Algorithm_lab2.Hanoi;

namespace Algorithm_lab2.Controls;

/// <summary>Draws the three rods, the disks on them and the disk that is currently flying between rods.</summary>
public sealed class HanoiBoard : Control
{
    private static readonly IBrush RodBrush = Palette.Brush(Palette.BorderStrong);
    private static readonly IBrush LabelBrush = Palette.Brush(Palette.MutedForeground);
    private static readonly IPen HighlightPen = new Pen(Palette.Brush(Palette.Foreground), 2);

    private HanoiState? _state;
    private HanoiMove? _flying;
    private double _progress;
    private int _lastDisk;

    public HanoiState? State
    {
        get => _state;
        set
        {
            _state = value;
            _flying = null;
            _lastDisk = 0;
            InvalidateVisual();
        }
    }

    /// <summary>Shows a disk that was lifted off <c>move.From</c>; <paramref name="progress"/> goes from 0 to 1.</summary>
    public void SetFlying(HanoiMove? move, double progress)
    {
        _flying = move;
        _progress = Math.Clamp(progress, 0, 1);
        if (move is { } m)
            _lastDisk = m.Disk;
        InvalidateVisual();
    }

    public void Refresh() => InvalidateVisual();

    public override void Render(DrawingContext context)
    {
        if (_state is null || Bounds.Width < 1 || Bounds.Height < 1)
            return;

        var w = Bounds.Width;
        var h = Bounds.Height;
        var disks = _state.Disks;

        var floor = h - 48;
        var diskHeight = Math.Min(30, (h * 0.65) / (disks + 1));
        var rodTop = floor - diskHeight * (disks + 1.5);
        var maxWidth = w / 3 * 0.9;
        var minWidth = maxWidth * 0.22;

        double RodX(int rod) => w * (1 + 2 * rod) / 6;
        double DiskWidth(int disk) => disks == 1 ? maxWidth : minWidth + (maxWidth - minWidth) * (disk - 1) / (disks - 1);
        Rect DiskRect(int disk, double centerX, double bottom) =>
            new(centerX - DiskWidth(disk) / 2, bottom - diskHeight, DiskWidth(disk), diskHeight - 2);

        // Base and rods.
        context.FillRectangle(RodBrush, new Rect(w * 0.03, floor, w * 0.94, 10), 4);
        for (var rod = 0; rod < 3; rod++)
        {
            context.FillRectangle(RodBrush, new Rect(RodX(rod) - 4, rodTop, 8, floor - rodTop), 4);
            var label = new FormattedText(HanoiState.RodName(rod), CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, Typeface.Default, 18, LabelBrush);
            context.DrawText(label, new Point(RodX(rod) - label.Width / 2, floor + 16));
        }

        // Disks resting on rods.
        for (var rod = 0; rod < 3; rod++)
        {
            var bottom = floor;
            foreach (var disk in _state.Rod(rod))
            {
                DrawDisk(context, disk, DiskRect(disk, RodX(rod), bottom));
                bottom -= diskHeight;
            }
        }

        // The flying disk: up from the source rod, across above the rods, down onto the target rod.
        if (_flying is { } move)
        {
            var sourceY = floor - diskHeight * _state.Rod(move.From).Count();
            var targetY = floor - diskHeight * _state.Rod(move.To).Count();
            var liftY = rodTop - diskHeight * 0.5;
            var (x, y) = _progress switch
            {
                < 1.0 / 3 => (RodX(move.From), Lerp(sourceY, liftY, _progress * 3)),
                < 2.0 / 3 => (Lerp(RodX(move.From), RodX(move.To), _progress * 3 - 1), liftY),
                _ => (RodX(move.To), Lerp(liftY, targetY, _progress * 3 - 2)),
            };
            DrawDisk(context, move.Disk, DiskRect(move.Disk, x, y));
        }
    }

    private void DrawDisk(DrawingContext context, int disk, Rect rect)
    {
        var hue = 300.0 * (disk - 1) / Math.Max(1, _state!.Disks - 1);
        var brush = new SolidColorBrush(new HsvColor(1, hue, 0.55, 0.92).ToRgb());
        context.DrawRectangle(brush, disk == _lastDisk ? HighlightPen : null, rect, 6, 6);
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
