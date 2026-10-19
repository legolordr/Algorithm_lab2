namespace Algorithm_lab2.Views;

using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using Algorithm_lab2.PersonalAlgorithms.FloodFill;


public partial class FloodFillView : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _frameClock = new();

    private IReadOnlyList<(int X, int Y)> _sequence = [];
    private int _currentIndex;
    private double _accumulator;
    private bool _running;

    public FloodFillView()
    {
        InitializeComponent();

        Canvas.CellPressed += OnCellPressed;
        _timer.Tick += (_, _) => OnFrame();

        SpeedSlider.ValueChanged += (_, _) => UpdateSpeedLabel();
        MaxCellsSlider.ValueChanged += (_, _) => UpdateMaxCellsLabel();
        GridSizeInput.ValueChanged += (_, _) => OnGridSizeChanged();
        PlayPauseButton.Click += (_, _) => TogglePlayPause();
        ClearButton.Click += (_, _) => ResetAll();

        UpdateSpeedLabel();
        OnGridSizeChanged();
    }

    private void OnGridSizeChanged()
    {
        var size = (int)(GridSizeInput.Value ?? 40);
        Canvas.GridSize = size;
        GridInfoText.Text = $"{size} × {size} = {size * size} клеток";

        var max = size * size;
        MaxCellsSlider.Maximum = max;
        MaxCellsSlider.Value = max;
        UpdateMaxCellsLabel();

        ResetAll();
    }

    private void OnCellPressed(int x, int y)
    {
        Pause();

        var size = Canvas.GridSize;
        var maxCells = (int)MaxCellsSlider.Value;

        var sw = Stopwatch.StartNew();
        var result = FloodFillSolver.Solve(size, size, x, y, maxCells);
        sw.Stop();

        _sequence = result.Cells;
        _currentIndex = 0;
        _accumulator = 0;

        Canvas.SetSequence(_sequence);
        Canvas.FillCount = 0;

        CallsText.Text = $"Рекурсивных вызовов: {result.Calls:N0}";
        MaxDepthText.Text = $"Макс. глубина рекурсии: {result.MaxDepth:N0}";
        TimeText.Text = $"Время расчёта: {sw.Elapsed.TotalMilliseconds:F2} мс";
        ProgressText.Text = $"Закрашено: 0 / {_sequence.Count:N0}";

        PlayPauseButton.IsEnabled = _sequence.Count > 0;
        if (_sequence.Count > 0)
            Play();
    }

    private void Play()
    {
        if (_currentIndex >= _sequence.Count) return;
        _running = true;
        _frameClock.Restart();
        _timer.Start();
        PlayPauseButton.Content = "Пауза";
    }

    private void Pause()
    {
        _running = false;
        _timer.Stop();
        if (_sequence.Count > 0)
            PlayPauseButton.Content = _currentIndex >= _sequence.Count ? "Готово" : "Продолжить";
    }

    private void TogglePlayPause()
    {
        if (_running) Pause();
        else Play();
    }

    private void OnFrame()
    {
        var elapsedMs = _frameClock.Elapsed.TotalMilliseconds;
        _frameClock.Restart();

        var speed = SpeedSlider.Value;                       // клеток в секунду
        _accumulator += speed * elapsedMs / 1000.0;
        var toAdd = (int)_accumulator;
        if (toAdd <= 0) return;
        _accumulator -= toAdd;

        _currentIndex = Math.Min(_currentIndex + toAdd, _sequence.Count);
        Canvas.FillCount = _currentIndex;
        ProgressText.Text = $"Закрашено: {_currentIndex:N0} / {_sequence.Count:N0}";

        if (_currentIndex >= _sequence.Count)
        {
            Pause();
            PlayPauseButton.Content = "Готово";
        }
    }

    private void ResetAll()
    {
        Pause();
        _sequence = [];
        _currentIndex = 0;
        _accumulator = 0;
        Canvas.Clear();

        CallsText.Text = "Рекурсивных вызовов: 0";
        MaxDepthText.Text = "Макс. глубина рекурсии: 0";
        TimeText.Text = "Время расчёта: —";
        ProgressText.Text = "Готов к запуску";
        PlayPauseButton.IsEnabled = false;
        PlayPauseButton.Content = "Пауза";
    }

    private void UpdateSpeedLabel() =>
        SpeedLabel.Text = $"Скорость: {SpeedSlider.Value:F0} клеток/с";

    private void UpdateMaxCellsLabel() =>
        MaxCellsLabel.Text = $"Максимум клеток: {(int)MaxCellsSlider.Value:N0}";
}