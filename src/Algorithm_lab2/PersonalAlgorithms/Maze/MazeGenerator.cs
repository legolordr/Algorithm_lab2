namespace Algorithm_lab2.PersonalAlgorithms.Maze;

public enum Direction { Up, Right, Down, Left }

public enum MazeStepKind { Enter, Backtrack }

/// <summary>
/// Один шаг рекурсии для анимации.
/// Enter   — зашли в клетку (FromDirection — откуда пришли: направление на предыдущую клетку).
/// Backtrack — вернулись в клетку после исчерпания её ветки.
/// </summary>
public readonly record struct MazeStep(MazeStepKind Kind, int X, int Y, Direction? FromDirection);

public sealed class MazeResult
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required IReadOnlyList<MazeStep> Steps { get; init; }
    public required int Calls { get; init; }
    public required int MaxDepth { get; init; }
    public required int Seed { get; init; }

    public int CellCount => Width * Height;
    public int InternalWalls => CellCount - 1; // для идеального лабиринта
}

public static class MazeGenerator
{
    private static readonly (Direction Dir, int Dx, int Dy)[] Deltas =
    [
        (Direction.Up,    0, -1),
        (Direction.Right, 1,  0),
        (Direction.Down,  0,  1),
        (Direction.Left,  -1, 0),
    ];

    public static MazeResult Generate(int width, int height, int? seed = null)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Размеры лабиринта должны быть положительными.");

        var actualSeed = seed ?? Random.Shared.Next();
        var rng = new Random(actualSeed);

        var visited = new bool[width, height];
        var steps = new List<MazeStep>(width * height * 2);
        var buffer = new (Direction Dir, int Dx, int Dy)[4];
        int calls = 0;
        int maxDepth = 0;

        void Carve(int x, int y, Direction? from, int depth)
        {
            calls++;
            if (depth > maxDepth) maxDepth = depth;

            visited[x, y] = true;
            steps.Add(new MazeStep(MazeStepKind.Enter, x, y, from));

            Array.Copy(Deltas, buffer, 4);
            for (int i = 3; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
            }

            foreach (var (dir, dx, dy) in buffer)
            {
                int nx = x + dx;
                int ny = y + dy;
                if ((uint)nx >= (uint)width || (uint)ny >= (uint)height) continue;
                if (visited[nx, ny]) continue;

                Carve(nx, ny, Opposite(dir), depth + 1);
                steps.Add(new MazeStep(MazeStepKind.Backtrack, x, y, null));
            }
        }

        Carve(0, 0, null, 0);

        return new MazeResult
        {
            Width = width,
            Height = height,
            Steps = steps,
            Calls = calls,
            MaxDepth = maxDepth,
            Seed = actualSeed,
        };
    }

    public static Direction Opposite(Direction d) => d switch
    {
        Direction.Up    => Direction.Down,
        Direction.Down  => Direction.Up,
        Direction.Left  => Direction.Right,
        Direction.Right => Direction.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(d)),
    };
}