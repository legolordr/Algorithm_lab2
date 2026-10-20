using Algorithm_lab2.Benchmarks;
using Algorithm_lab2.Controls;

namespace Algorithm_lab2.Tests;

public class BenchmarkTests
{
    [Fact]
    public void Fit_FindsExactConstantWithZeroError()
    {
        double[] theory = [1, 4, 16, 64];
        var measured = theory.Select(f => 2.5 * f).ToArray();

        var (c, mse) = BenchmarkRunner.Fit(theory, measured);

        Assert.Equal(2.5, c, 9);
        Assert.Equal(0, mse, 9);
    }

    [Fact]
    public void Fit_MinimizesSquaredError()
    {
        double[] theory = [1, 2, 3];
        double[] measured = [2, 5, 5];

        var (c, mse) = BenchmarkRunner.Fit(theory, measured);

        // C = Σ t·f / Σ f² = (2 + 10 + 15) / (1 + 4 + 9) = 27 / 14
        Assert.Equal(27.0 / 14, c, 9);
        double Error(double k) => theory.Select((f, i) => Math.Pow(measured[i] - k * f, 2)).Average();
        Assert.Equal(Error(c), mse, 9);
        Assert.True(Error(c) < Error(c + 0.01) && Error(c) < Error(c - 0.01));
    }

    [Theory]
    [InlineData(0.0005, "нс")]
    [InlineData(0.5, "мкс")]
    [InlineData(250, "мс")]
    [InlineData(4000, "с")]
    public void PickUnit_KeepsAxisNumbersReadable(double maxMilliseconds, string unit) =>
        Assert.Equal(unit, BenchmarkPlot.PickUnit(maxMilliseconds).Unit);

    [Fact]
    public void Run_MeasuresEverySizeAndReportsProgress()
    {
        var benchmark = new BenchmarkCase("Test", "O(n)", "n", [1, 2, 3], n => n,
            n => () => Thread.SpinWait(1000 * n));
        var reports = new List<double>();

        var result = BenchmarkRunner.Run(benchmark, new SyncProgress(reports.Add));

        Assert.Equal([1, 2, 3], result.Points.Select(p => p.N));
        Assert.All(result.Points, p => Assert.True(p.Milliseconds > 0));
        Assert.Equal([1.0 / 3, 2.0 / 3, 1.0], reports);
    }

    [Fact]
    public void Run_StopsWhenCancelled()
    {
        var benchmark = BenchmarkCases.All[0];
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => BenchmarkRunner.Run(benchmark, null, cancellation.Token));
    }

    [Fact]
    public void EveryCase_RunsOnItsSmallestSize()
    {
        Assert.Equal(6, BenchmarkCases.All.Count);
        Assert.All(BenchmarkCases.All, benchmark =>
        {
            Assert.True(benchmark.Sizes.Count >= 5);
            benchmark.Prepare(benchmark.Sizes[0])();
        });
    }

    /// <summary>Progress&lt;T&gt; posts to a synchronization context; the test needs the reports immediately.</summary>
    private sealed class SyncProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }
}
