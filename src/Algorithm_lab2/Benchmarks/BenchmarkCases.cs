using Avalonia;
using Algorithm_lab2.Fractals;
using Algorithm_lab2.Hanoi;
using Algorithm_lab2.PersonalAlgorithms.FloodFill;
using Algorithm_lab2.PersonalAlgorithms.Maze;
using Algorithm_lab2.PersonalAlgorithms.MergeSort;

namespace Algorithm_lab2.Benchmarks;

/// <summary>The algorithms of the lab with the sizes they are measured on and their theoretical complexity.</summary>
public static class BenchmarkCases
{
    private static readonly Size FractalArea = new(1000, 1000);

    public static IReadOnlyList<BenchmarkCase> All { get; } =
    [
        new("Снежинка Коха", "O(4ⁿ)", "Глубина рекурсии n",
            Range(0, 7, 1),
            n => Math.Pow(4, n),
            n =>
            {
                var koch = new KochSnowflake();
                return () => koch.Build(FractalArea, n);
            }),

        new("Ханойская башня (рекурсия)", "O(2ⁿ)", "Количество колец n",
            Range(1, 20, 1),
            n => Math.Pow(2, n) - 1,
            n => () => HanoiSolver.SolveRecursive(n)),

        new("Ханойская башня (явный стек)", "O(2ⁿ)", "Количество колец n",
            Range(1, 20, 1),
            n => Math.Pow(2, n) - 1,
            n => () => HanoiSolver.SolveWithStack(n)),

        new("Рекурсивная заливка", "O(n²)", "Сторона сетки n (клеток n²)",
            Range(10, 150, 10),
            n => (double)n * n,
            n => () => FloodFillSolver.Solve(n, n, 0, 0, n * n)),

        new("Генератор лабиринта", "O(n²)", "Сторона сетки n (клеток n²)",
            Range(10, 150, 10),
            n => (double)n * n,
            n => () => MazeGenerator.Generate(n, n, seed: 1)),

        new("Сортировка слиянием", "O(n log n)", "Размер массива n",
            Range(10_000, 200_000, 10_000),
            n => n * Math.Log2(n),
            n =>
            {
                // The random input is built once, outside the timing; Sort copies it, so every run gets the same data.
                var random = new Random(n);
                var input = Enumerable.Range(0, n).Select(_ => random.Next()).ToArray();
                return () => MergeSorter.Sort(input, recordSteps: false);
            }),
    ];

    private static int[] Range(int from, int to, int step) =>
        Enumerable.Range(0, (to - from) / step + 1).Select(i => from + i * step).ToArray();
}
