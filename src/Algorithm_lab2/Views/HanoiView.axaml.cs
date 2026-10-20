using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Algorithm_lab2.Hanoi;

namespace Algorithm_lab2.Views;

public partial class HanoiView : UserControl, IShortcutHandler
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _frameClock = new();
    private readonly ObservableCollection<string> _log = [];

    private HanoiState _state = new(8);
    private IReadOnlyList<HanoiMove> _moves = [];
    private int _done;
    private HanoiMove? _flying;
    private double _progress;

    public HanoiView()
    {
        InitializeComponent();

        MovesList.ItemsSource = _log;
        _timer.Tick += (_, _) => OnFrame();

        DiskCount.ValueChanged += (_, _) => Reset();
        RecursiveRadio.IsCheckedChanged += (_, _) => Reset();
        DurationSlider.ValueChanged += (_, _) => UpdateDurationLabel();
        PlayButton.Click += (_, _) => TogglePlay();
        StepButton.Click += (_, _) => Step();
        ResetButton.Click += (_, _) => Reset();

        UpdateDurationLabel();
        Reset();
    }

    private bool IsPlaying => _timer.IsEnabled;

    public bool HandleShortcut(Key key)
    {
        switch (key)
        {
            case Key.Space: TogglePlay(); return true;
            case Key.Right: Step(); return true;
            case Key.R: Reset(); return true;
            default: return false;
        }
    }

    private void Reset()
    {
        Pause();

        var disks = (int)(DiskCount.Value ?? 8);
        var useStack = StackRadio.IsChecked == true;
        var stopwatch = Stopwatch.StartNew();
        var solution = useStack ? HanoiSolver.SolveWithStack(disks) : HanoiSolver.SolveRecursive(disks);
        stopwatch.Stop();

        _state = new HanoiState(disks);
        _moves = solution.Moves;
        _done = 0;
        _flying = null;
        _log.Clear();
        Board.State = _state;

        MinMovesStat.Value = $"2^{disks} − 1 = {HanoiSolver.MinMoves(disks):N0}";
        DepthStat.Label = useStack ? "Макс. размер стека" : "Макс. глубина рекурсии";
        DepthStat.Value = solution.MaxDepth.ToString();
        TimeStat.Value = TimeFormat.Format(stopwatch.Elapsed);
        Progress.Maximum = Math.Max(1, _moves.Count);
        UpdateProgress();
    }

    private void TogglePlay()
    {
        if (IsPlaying)
        {
            Pause();
            return;
        }

        if (_done == _moves.Count)
            Reset();

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
        if (_flying is null)
        {
            if (_done == _moves.Count)
                return;
            StartMove();
        }
        FinishMove();
    }

    private void OnFrame()
    {
        var elapsed = _frameClock.Elapsed.TotalMilliseconds;
        _frameClock.Restart();

        if (_flying is null)
        {
            if (_done == _moves.Count)
            {
                Pause();
                return;
            }
            StartMove();
        }

        _progress += elapsed / DurationSlider.Value;
        if (_progress >= 1)
            FinishMove();
        else
            Board.SetFlying(_flying, _progress);
    }

    private void StartMove()
    {
        var move = _moves[_done];
        var disk = _state.Lift(move.From);
        if (disk != move.Disk)
            throw new InvalidOperationException($"Ожидался диск {move.Disk}, а сверху лежит {disk}.");

        _flying = move;
        _progress = 0;
        Board.SetFlying(move, 0);
    }

    private void FinishMove()
    {
        var move = _flying!.Value;
        _state.Drop(move.To, move.Disk);
        _flying = null;
        _done++;

        _log.Add($"{_done}. {move}");
        MovesList.ScrollIntoView(_log.Count - 1);
        Board.SetFlying(null, 0);
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        ProgressRow.Value = $"{_done:N0} / {_moves.Count:N0}";
        Progress.Value = _done;
        StatusText.IsVisible = _state.IsSolved();
    }

    private void UpdateDurationLabel() =>
        DurationRow.Value = $"{DurationSlider.Value:F0} мс";
}
