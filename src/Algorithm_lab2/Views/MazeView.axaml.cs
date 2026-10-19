namespace Algorithm_lab2.Views;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using Algorithm_lab2.PersonalAlgorithms.Maze;


public partial class MazeView : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _frameClock = new();

    private MazeResult? _result;
    private int _currentStep;
    private double _accumulator;

    public MazeView()
    {
        InitializeComponent();

        _timer.Tick += (_, _) => OnFrame();

        SizeInput.ValueChanged   += (_, _) => Regenerate();
        SeedInput.ValueChanged   += (_, _) => Regenerate();
        SpeedSlider.ValueChanged += (_, _) => UpdateSpeedLabel();
        PlayPauseButton.Click    += (_, _) => TogglePlayPause();
        RegenerateButton.Click   += (_, _) => Regenerate();
        ResetButton.Click        += (_, _) => RestartAnimation();

        UpdateSpeedLabel();
        Regenerate();
    }

    private bool IsPlaying => _timer.IsEnabled;

    // ─────────────────────────── Регенерация ───────────────────────────

    private void Regenerate()
    {
        _timer.Stop();

        var size = (int)(SizeInput.Value ?? 20);
        SizeInfoText.Text = $"{size} × {size} = {size * size} клеток";

        var seedValue = (int)(SeedInput.Value ?? -1);
        int? seed = seedValue < 0 ? null : seedValue;

        var sw = Stopwatch.StartNew();
        var result = MazeGenerator.Generate(size, size, seed);
        sw.Stop();

        _result = result;
        _currentStep = 0;
        _accumulator = 0;

        Canvas.Resize(size, size);
        Canvas.ResetAnimation();

        CallsText.Text  = $"Рекурсивных вызовов: {result.Calls:N0}";
        DepthText.Text  = $"Макс. глубина рекурсии: {result.MaxDepth:N0}";
        SeedText.Text   = $"Seed: {result.Seed}";
        TimeText.Text   = $"Время расчёта: {sw.Elapsed.TotalMilliseconds:F2} мс";
        StatusText.Text = "";

        UpdateProgress();
        UpdatePlayButton();
    }

    private void RestartAnimation()
    {
        _timer.Stop();
        if (_result is null) { Regenerate(); return; }

        _currentStep = 0;
        _accumulator = 0;
        Canvas.ResetAnimation();
        StatusText.Text = "";
        UpdateProgress();
        UpdatePlayButton();
    }

    // ─────────────────────────── Управление ───────────────────────────

    private void TogglePlayPause()
    {
        if (IsPlaying) { _timer.Stop(); UpdatePlayButton(); }
        else           { Play(); }
    }

    private void Play()
    {
        if (_result is null) return;
        if (_currentStep >= _result.Steps.Count) RestartAnimation();

        _frameClock.Restart();
        _timer.Start();
        UpdatePlayButton();
    }

    private void UpdatePlayButton()
    {
        if (IsPlaying)
            PlayPauseButton.Content = "Пауза";
        else if (_currentStep == 0)
            PlayPauseButton.Content = "Старт";
        else if (_result is not null && _currentStep >= _result.Steps.Count)
            PlayPauseButton.Content = "Заново";
        else
            PlayPauseButton.Content = "Продолжить";
    }

    private void OnFrame()
    {
        if (_result is null) return;

        var elapsedMs = _frameClock.Elapsed.TotalMilliseconds;
        _frameClock.Restart();

        _accumulator += SpeedSlider.Value * elapsedMs / 1000.0;
        var toApply = (int)_accumulator;
        if (toApply <= 0) return;
        _accumulator -= toApply;

        var total = _result.Steps.Count;
        for (int i = 0; i < toApply && _currentStep < total; i++)
        {
            Canvas.ApplyStep(_result.Steps[_currentStep]);
            _currentStep++;
        }

        UpdateProgress();

        if (_currentStep >= total)
        {
            _timer.Stop();
            StatusText.Text = $"Лабиринт построен ✓";
            UpdatePlayButton();
        }
    }

    private void UpdateProgress() =>
        ProgressText.Text = $"Шаг: {_currentStep:N0} / {(_result?.Steps.Count ?? 0):N0}";

    private void UpdateSpeedLabel() =>
        SpeedLabel.Text = $"Скорость: {SpeedSlider.Value:F0} шагов/с";
}