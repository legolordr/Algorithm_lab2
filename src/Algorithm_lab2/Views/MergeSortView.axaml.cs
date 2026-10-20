using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Algorithm_lab2.PersonalAlgorithms.MergeSort;

namespace Algorithm_lab2.Views;

public partial class MergeSortView : UserControl, IShortcutHandler
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _frameClock = new();
    private readonly Random _random = new();

    private int[] _input = [];
    private int[] _array = [];   // the array as the algorithm sees it, used for the step captions
    private IReadOnlyList<MergeSortStep> _steps = [];
    private int _done;
    private double _pendingSteps;

    public MergeSortView()
    {
        InitializeComponent();

        _timer.Tick += (_, _) => OnFrame();
        SizeInput.ValueChanged += (_, _) => Generate();
        OrderInput.SelectionChanged += (_, _) => Generate();
        SpeedSlider.ValueChanged += (_, _) => UpdateSpeedLabel();
        PlayButton.Click += (_, _) => TogglePlay();
        StepButton.Click += (_, _) => Step();
        ShuffleButton.Click += (_, _) => Generate();
        ResetButton.Click += (_, _) => { Pause(); Restart(); };

        UpdateSpeedLabel();
        Generate();
    }

    private bool IsPlaying => _timer.IsEnabled;

    /// <summary>Creates a new input array, sorts it once to record the steps and resets the animation.</summary>
    private void Generate()
    {
        Pause();

        var size = (int)(SizeInput.Value ?? 8);
        _input = OrderInput.SelectedIndex switch
        {
            1 => Enumerable.Range(1, size).Reverse().ToArray(),
            2 => AlmostSorted(size),
            _ => Shuffled(size),
        };

        var stopwatch = Stopwatch.StartNew();
        var result = MergeSorter.Sort(_input);
        stopwatch.Stop();
        _steps = result.Steps;

        CallsStat.Value = $"{result.Calls:N0}";
        DepthStat.Value = result.MaxDepth.ToString();
        ComparisonsStat.Value = $"{result.Comparisons:N0}";
        WritesStat.Value = $"{result.Writes:N0}";
        TimeStat.Value = TimeFormat.Format(stopwatch.Elapsed);
        Progress.Maximum = Math.Max(1, _steps.Count);

        Restart();
    }

    private void Restart()
    {
        _array = (int[])_input.Clone();
        _done = 0;
        _pendingSteps = 0;

        Tree.Reset(_input);
        CurrentText.Text = "Нажмите «Старт» или «Шаг».";
        UpdateProgress();
    }

    private void TogglePlay()
    {
        if (IsPlaying)
        {
            Pause();
            return;
        }

        if (_done == _steps.Count)
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
        if (_done < _steps.Count)
            ApplyNext();
    }

    private void OnFrame()
    {
        _pendingSteps += _frameClock.Elapsed.TotalSeconds * SpeedSlider.Value;
        _frameClock.Restart();

        while (_pendingSteps >= 1 && _done < _steps.Count)
        {
            ApplyNext();
            _pendingSteps--;
        }

        if (_done == _steps.Count)
            Pause();
    }

    /// <summary>
    /// Applies steps up to and including the next one that changes the tree. Copying back from the buffer
    /// (<see cref="WriteStep"/>) is not shown: the parent box is already filled by then.
    /// </summary>
    private void ApplyNext()
    {
        MergeSortStep step;
        do
        {
            step = _steps[_done++];
            ApplyStep(step);
        } while (step is WriteStep && _done < _steps.Count);

        UpdateProgress();
    }

    private void ApplyStep(MergeSortStep step)
    {
        Tree.Apply(step);

        switch (step)
        {
            case SplitStep s:
                CurrentText.Text = $"Глубина {s.Depth}: делим [{s.Left}..{s.Right}] на " +
                                   $"[{s.Left}..{s.Middle}] и [{s.Middle + 1}..{s.Right}]";
                break;
            case MergeStep m:
                CurrentText.Text = $"Глубина {m.Depth}: обе половины отсортированы, сливаем " +
                                   $"[{m.Left}..{m.Middle}] и [{m.Middle + 1}..{m.Right}]";
                break;
            case CompareStep c:
                CurrentText.Text = $"Сравниваем {_array[c.I]} и {_array[c.J]} — берём меньшее";
                break;
            case TakeStep t:
                CurrentText.Text = $"Переносим {t.Value} в результат";
                break;
            case WriteStep w:
                _array[w.Index] = w.Value;
                break;
            case MergedStep merged:
                CurrentText.Text = merged.Left == 0 && merged.Right == _input.Length - 1
                    ? "Массив отсортирован ✓"
                    : $"Глубина {merged.Depth}: отрезок [{merged.Left}..{merged.Right}] отсортирован, " +
                      "возвращаемся из вызова";
                break;
        }
    }

    private void UpdateProgress()
    {
        ProgressRow.Value = $"{_done:N0} / {_steps.Count:N0}";
        Progress.Value = _done;
    }

    public bool HandleShortcut(Key key)
    {
        switch (key)
        {
            case Key.Space: TogglePlay(); return true;
            case Key.Right: Step(); return true;
            case Key.R: Pause(); Restart(); return true;
            case Key.N: Generate(); return true;
            default: return false;
        }
    }

    private void UpdateSpeedLabel()
    {
        SpeedRow.Value = $"{SpeedSlider.Value:F0} шаг/с";
        // An animation takes most of the time between two steps, so it finishes before the next one starts.
        Tree.AnimationDuration = TimeSpan.FromSeconds(Math.Clamp(0.75 / SpeedSlider.Value, 0.08, 0.45));
    }

    private int[] Shuffled(int size)
    {
        var values = Enumerable.Range(1, size).ToArray();
        _random.Shuffle(values);
        return values;
    }

    private int[] AlmostSorted(int size)
    {
        var values = Enumerable.Range(1, size).ToArray();
        for (var swap = 0; swap < Math.Max(1, size / 10); swap++)
        {
            var a = _random.Next(size);
            var b = _random.Next(size);
            (values[a], values[b]) = (values[b], values[a]);
        }
        return values;
    }
}
