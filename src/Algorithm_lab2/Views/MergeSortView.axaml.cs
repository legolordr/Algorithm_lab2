using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using Algorithm_lab2.PersonalAlgorithms.MergeSort;

namespace Algorithm_lab2.Views;

public partial class MergeSortView : UserControl
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

        var result = MergeSorter.Sort(_input);
        _steps = result.Steps;

        CallsText.Text = $"Рекурсивных вызовов: {result.Calls:N0}";
        DepthText.Text = $"Макс. глубина рекурсии: {result.MaxDepth}";
        ComparisonsText.Text = $"Сравнений: {result.Comparisons:N0}";
        WritesText.Text = $"Записей: {result.Writes:N0}";

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

    private void UpdateProgress() => ProgressText.Text = $"Шаг: {_done:N0} / {_steps.Count:N0}";

    private void UpdateSpeedLabel() => SpeedLabel.Text = $"Скорость: {SpeedSlider.Value:F0} шаг/с";

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
