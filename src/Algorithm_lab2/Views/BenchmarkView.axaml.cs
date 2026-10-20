using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Algorithm_lab2.Benchmarks;
using Algorithm_lab2.Controls;

namespace Algorithm_lab2.Views;

public partial class BenchmarkView : UserControl, IShortcutHandler
{
    // Flood fill and the maze recurse once per cell: 150 × 150 calls deep does not fit the default 1 MB stack.
    private const int BenchmarkStackSize = 256 * 1024 * 1024;

    private readonly Dictionary<BenchmarkCase, BenchmarkResult> _results = [];
    private CancellationTokenSource? _cancellation;

    public BenchmarkView()
    {
        InitializeComponent();

        AlgorithmList.ItemsSource = BenchmarkCases.All;
        AlgorithmList.SelectionChanged += (_, _) => ShowSelected();
        RunButton.Click += (_, _) => RunOrStop([Selected]);
        RunAllButton.Click += (_, _) => RunOrStop(BenchmarkCases.All);
        ResetZoomButton.Click += (_, _) => ResetZoom();

        AlgorithmList.SelectedIndex = 0;
    }

    private BenchmarkCase Selected => (BenchmarkCase)AlgorithmList.SelectedItem!;

    private bool IsRunning => _cancellation is not null;

    public bool HandleShortcut(Key key)
    {
        switch (key)
        {
            case Key.Space: RunOrStop([Selected]); return true;
            case Key.D0 or Key.NumPad0 when ResetZoomButton.IsVisible: ResetZoom(); return true;
            default: return false;
        }
    }

    private void RunOrStop(IReadOnlyList<BenchmarkCase> cases)
    {
        if (IsRunning)
        {
            _cancellation!.Cancel();
            return;
        }

        var cancellation = _cancellation = new CancellationTokenSource();
        SetRunning(true);

        // Created on the UI thread, so reports from the benchmark thread are posted back to it.
        var done = 0;
        var progress = new Progress<double>(part => Progress.Value = (done + part) / cases.Count);

        var thread = new Thread(() =>
        {
            try
            {
                foreach (var benchmark in cases)
                {
                    Dispatcher.UIThread.Post(() => StatusText.Text = $"Замер: {benchmark.Name}…");
                    var result = BenchmarkRunner.Run(benchmark, progress, cancellation.Token);
                    Dispatcher.UIThread.Post(() =>
                    {
                        done++;
                        _results[benchmark] = result;
                        if (Selected == benchmark)
                            ShowSelected();
                    });
                }
                Dispatcher.UIThread.Post(() => StatusText.Text = "Замер завершён.");
            }
            catch (OperationCanceledException)
            {
                Dispatcher.UIThread.Post(() => StatusText.Text = "Замер остановлен.");
            }
            finally
            {
                Dispatcher.UIThread.Post(() => SetRunning(false));
            }
        }, BenchmarkStackSize) { IsBackground = true, Name = "Benchmark" };
        thread.Start();
    }

    private void SetRunning(bool running)
    {
        if (!running)
        {
            _cancellation?.Dispose();
            _cancellation = null;
        }
        else
        {
            Progress.Value = 0;
        }

        RunButton.Content = running ? "Остановить" : "Запустить замер";
        RunAllButton.IsEnabled = !running;
    }

    private void ShowSelected()
    {
        if (AlgorithmList.SelectedItem is not BenchmarkCase benchmark)
            return;

        _results.TryGetValue(benchmark, out var result);
        BenchmarkPlot.Show(Chart, result);
        EmptyText.IsVisible = result is null;
        ResetZoomButton.IsVisible = result is not null;

        ComplexityStat.Value = benchmark.Complexity;
        ConstantStat.Value = result is null ? "—" : result.C.ToString("0.###E+0", CultureInfo.CurrentCulture) + " мс";
        MseStat.Value = result is null ? "—" : result.Mse.ToString("0.###E+0", CultureInfo.CurrentCulture) + " мс²";
        MaxTimeStat.Value = result is null ? "—" : Format(result.Points.Max(p => p.Milliseconds));

        DataHint.IsVisible = result is null;
        DataList.ItemsSource = result?.Points
            .Select(p => new StatRow { Label = $"n = {p.N:N0}", Value = Format(p.Milliseconds) })
            .ToArray();
    }

    private void ResetZoom()
    {
        Chart.Plot.Axes.AutoScale();
        Chart.Refresh();
    }

    private static string Format(double milliseconds) => TimeFormat.Format(TimeSpan.FromMilliseconds(milliseconds));
}
