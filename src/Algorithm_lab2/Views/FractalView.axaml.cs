using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Algorithm_lab2.Controls;
using Algorithm_lab2.Fractals;

namespace Algorithm_lab2.Views;

public partial class FractalView : UserControl, IShortcutHandler
{
    // Step mode replays one split per step: 4^depth - 1 of them, so deeper levels would take too long to watch.
    private const int StepModeMaxDepth = 5;

    // Add new fractals here; the list, depth slider and statistics pick them up automatically.
    private readonly Fractal[] _fractals =
    [
        new KochSnowflake(),
    ];

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _frameClock = new();

    private KochTrace? _trace;
    private int _done;
    private double _pendingSteps;

    public FractalView()
    {
        InitializeComponent();

        FractalList.ItemsSource = _fractals;
        FractalList.SelectionChanged += (_, _) => OnFractalSelected();
        DepthSlider.ValueChanged += (_, e) => OnDepthChanged((int)e.NewValue);
        StepRadio.IsCheckedChanged += (_, _) => OnModeChanged();
        SpeedSlider.ValueChanged += (_, _) => UpdateSpeed();
        PlayButton.Click += (_, _) => TogglePlay();
        StepButton.Click += (_, _) => Step();
        ResetButton.Click += (_, _) => Restart();
        Canvas.Built += OnBuilt;
        _timer.Tick += (_, _) => OnFrame();

        UpdateSpeed();

        if (_fractals.Length == 0)
        {
            DepthSlider.IsEnabled = false;
            StepRadio.IsEnabled = false;
            DepthRow.Value = "—";
            DescriptionText.Text = "Фракталы пока не добавлены.";
            return;
        }

        FractalList.SelectedIndex = 0;
    }

    private bool IsStepMode => StepRadio.IsChecked == true;

    private bool IsPlaying => _timer.IsEnabled;

    private void OnFractalSelected()
    {
        if (FractalList.SelectedItem is not Fractal fractal)
            return;

        DescriptionText.Text = fractal.Description;
        // Only the Koch snowflake records its splits, so only it has a step-by-step mode.
        StepRadio.IsEnabled = fractal is KochSnowflake;
        if (!StepRadio.IsEnabled)
            FinishedRadio.IsChecked = true;

        DepthSlider.Maximum = MaxDepth(fractal);
        DepthSlider.Value = Math.Min(fractal.DefaultDepth, DepthSlider.Maximum);
        Canvas.Depth = (int)DepthSlider.Value;
        Canvas.Fractal = fractal;
        DepthRow.Value = DepthSlider.Value.ToString("F0");
        BuildTrace();
    }

    private void OnDepthChanged(int depth)
    {
        DepthRow.Value = depth.ToString();
        Canvas.Depth = depth;
        BuildTrace();
    }

    private void OnModeChanged()
    {
        var step = IsStepMode;
        Canvas.IsVisible = !step;
        StepCanvas.IsVisible = step;
        StepPanel.IsVisible = step;
        SpeedPanel.IsVisible = step;
        DescriptionCard.IsVisible = !step;
        StatsCard.IsVisible = !step;

        if (FractalList.SelectedItem is Fractal fractal)
            DepthSlider.Maximum = MaxDepth(fractal);
        if (step)
            BuildTrace();
        else
            Pause();
    }

    private int MaxDepth(Fractal fractal) => IsStepMode ? Math.Min(fractal.MaxDepth, StepModeMaxDepth) : fractal.MaxDepth;

    /// <summary>Runs the recursion once, records its splits and shows the starting triangle.</summary>
    private void BuildTrace()
    {
        if (!IsStepMode || FractalList.SelectedItem is not KochSnowflake koch)
            return;

        Pause();
        _trace = koch.Trace(KochStepCanvas.ReferenceSize, (int)DepthSlider.Value);
        Progress.Maximum = Math.Max(1, _trace.Splits.Count);
        Restart();
    }

    private void Restart()
    {
        if (_trace is null)
            return;

        Pause();
        _done = 0;
        _pendingSteps = 0;
        StepCanvas.Reset(_trace);
        UpdateStepInfo(null);
    }

    private void TogglePlay()
    {
        if (IsPlaying)
        {
            Pause();
            return;
        }

        if (_trace is null)
            return;
        if (_done == _trace.Splits.Count)
            Restart();

        _frameClock.Restart();
        _timer.Start();
        PlayButton.Content = "Пауза";
    }

    private void Pause()
    {
        _timer.Stop();
        PlayButton.Content = "Старт";
    }

    private void Step()
    {
        Pause();
        if (_trace is not null && _done < _trace.Splits.Count)
            ApplyNext();
    }

    private void OnFrame()
    {
        _pendingSteps += _frameClock.Elapsed.TotalSeconds * SpeedSlider.Value;
        _frameClock.Restart();

        while (_pendingSteps >= 1 && _trace is not null && _done < _trace.Splits.Count)
        {
            ApplyNext();
            _pendingSteps--;
        }

        if (_trace is null || _done == _trace.Splits.Count)
            Pause();
    }

    private void ApplyNext()
    {
        var split = _trace!.Splits[_done++];
        StepCanvas.Apply(split);
        UpdateStepInfo(split);
    }

    private void UpdateStepInfo(KochSplit? split)
    {
        if (_trace is null)
            return;

        var depth = (int)DepthSlider.Value;
        var total = _trace.Splits.Count;
        ProgressRow.Value = $"{_done:N0} / {total:N0}";
        Progress.Value = _done;
        // Every split turns one segment into four: +3 segments.
        SegmentsNowRow.Value = $"{3 + 3 * _done:N0}";
        LevelRow.Value = split is null ? "—" : $"{depth - split.Depth + 1} из {depth}";

        StepText.Text = (split, _done == total) switch
        {
            (_, true) when total == 0 => "Глубина 0 — это база рекурсии: отрезки не делятся, остаётся треугольник.",
            (_, true) => $"Готово: {3 + 3 * total:N0} отрезков = 3 · 4^{depth}.",
            (null, _) => "Исходный треугольник: 3 отрезка. Нажмите «Старт» или «Шаг».",
            _ => "Делим отрезок на 3 части, среднюю заменяем двумя сторонами зубца: 1 отрезок → 4.",
        };
    }

    private void UpdateSpeed()
    {
        SpeedRow.Value = $"{SpeedSlider.Value:F0} шаг/с";
        // The animation takes most of the time between two steps.
        StepCanvas.AnimationDuration = TimeSpan.FromSeconds(Math.Clamp(0.85 / SpeedSlider.Value, 0.08, 0.9));
    }

    public bool HandleShortcut(Key key)
    {
        if (IsStepMode)
        {
            switch (key)
            {
                case Key.Space: TogglePlay(); return true;
                case Key.Right: Step(); return true;
                case Key.R: Restart(); return true;
            }
        }

        if (!DepthSlider.IsEnabled || key is not (Key.Left or Key.Right))
            return false;

        DepthSlider.Value = Math.Clamp(DepthSlider.Value + (key == Key.Right ? 1 : -1), 0, DepthSlider.Maximum);
        return true;
    }

    private void OnBuilt(object? sender, FractalBuiltEventArgs e)
    {
        CallsStat.Value = $"{e.Result.Calls:N0}";
        // A polygon has as many sides as points, a polyline one fewer.
        var segments = e.Result.Shapes.Sum(s => s.Closed ? s.Points.Length : s.Points.Length - 1);
        SegmentsStat.Value = $"{segments:N0}";
        TimeStat.Value = TimeFormat.Format(e.Elapsed);
    }
}
