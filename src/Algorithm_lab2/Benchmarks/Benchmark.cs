using System.Diagnostics;

namespace Algorithm_lab2.Benchmarks;

/// <summary>
/// One algorithm to measure.
/// <paramref name="Prepare"/> builds the input for size n outside the timing and returns the work to time.
/// <paramref name="Theory"/> is f(n) from the complexity, e.g. n·log₂n; the fit finds C in t(n) ≈ C·f(n).
/// </summary>
public sealed record BenchmarkCase(
    string Name,
    string Complexity,
    string SizeLabel,
    IReadOnlyList<int> Sizes,
    Func<int, double> Theory,
    Func<int, Action> Prepare);

public sealed record BenchmarkPoint(int N, double Milliseconds);

/// <summary>Measured points and the least squares fit t(n) ≈ C·f(n) with its mean squared error.</summary>
public sealed record BenchmarkResult(BenchmarkCase Case, IReadOnlyList<BenchmarkPoint> Points, double C, double Mse);

public static class BenchmarkRunner
{
    // A single run of a small input is shorter than the timer can measure reliably, so runs are repeated
    // in a batch until the batch takes at least this long, and the time of one run is batch / count.
    private const double MinBatchMilliseconds = 5;
    private const int Batches = 5;

    /// <summary>Measures every size of the case. Call it off the UI thread: deep recursion needs a big stack.</summary>
    public static BenchmarkResult Run(BenchmarkCase benchmark, IProgress<double>? progress = null,
        CancellationToken cancellation = default)
    {
        // Warm-up: the first call of every method includes JIT compilation and would spoil the first point.
        benchmark.Prepare(benchmark.Sizes[0])();

        var points = new List<BenchmarkPoint>();
        for (var i = 0; i < benchmark.Sizes.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var n = benchmark.Sizes[i];
            points.Add(new BenchmarkPoint(n, Measure(benchmark.Prepare(n))));
            progress?.Report((i + 1.0) / benchmark.Sizes.Count);
        }

        var (c, mse) = Fit(points.Select(p => benchmark.Theory(p.N)).ToArray(),
            points.Select(p => p.Milliseconds).ToArray());
        return new BenchmarkResult(benchmark, points, c, mse);
    }

    /// <summary>Median time of one run, in milliseconds.</summary>
    public static double Measure(Action work)
    {
        var times = new double[Batches];
        for (var batch = 0; batch < Batches; batch++)
        {
            var runs = 0;
            var stopwatch = Stopwatch.StartNew();
            do
            {
                work();
                runs++;
            } while (stopwatch.Elapsed.TotalMilliseconds < MinBatchMilliseconds);

            times[batch] = stopwatch.Elapsed.TotalMilliseconds / runs;
        }

        // The median ignores a batch that was slowed down by the garbage collector or another program.
        Array.Sort(times);
        return times[Batches / 2];
    }

    /// <summary>
    /// Least squares for t ≈ C·f: minimizing Σ(tᵢ − C·fᵢ)² gives C = Σ(tᵢ·fᵢ) / Σ(fᵢ²).
    /// Returns C and the mean squared error of the fit.
    /// </summary>
    public static (double C, double Mse) Fit(IReadOnlyList<double> theory, IReadOnlyList<double> measured)
    {
        double product = 0, square = 0;
        for (var i = 0; i < theory.Count; i++)
        {
            product += measured[i] * theory[i];
            square += theory[i] * theory[i];
        }

        var c = square == 0 ? 0 : product / square;
        var mse = theory.Count == 0
            ? 0
            : theory.Select((f, i) => Math.Pow(measured[i] - c * f, 2)).Average();
        return (c, mse);
    }
}
