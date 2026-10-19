namespace Algorithm_lab2.PersonalAlgorithms.FloodFill;
public readonly record struct FloodFillResult(
    IReadOnlyList<(int X, int Y)> Cells,
    int Calls,
    int MaxDepth,
    bool StoppedByLimit);

public static class FloodFillSolver
{
    /// <summary>
    /// Рекурсивная 4-связная заливка. Возвращает порядок, в котором клетки были
    /// закрашены, а также статистику рекурсивных вызовов.
    /// </summary>
    public static FloodFillResult Solve(
        int width, int height,
        int startX, int startY,
        int maxCells,
        bool[,]? blocked = null)
    {
        var visited = new bool[width, height];
        var cells = new List<(int X, int Y)>();
        int calls = 0;
        int maxDepth = 0;
        bool stopped = false;

        void Fill(int x, int y, int depth)
        {
            calls++;

            if (cells.Count >= maxCells) { stopped = true; return; }
            if ((uint)x >= (uint)width || (uint)y >= (uint)height) return;
            if (visited[x, y]) return;
            if (blocked != null && blocked[x, y]) return;

            visited[x, y] = true;
            if (depth > maxDepth) maxDepth = depth;
            cells.Add((x, y));

            Fill(x + 1, y, depth + 1);
            Fill(x - 1, y, depth + 1);
            Fill(x, y + 1, depth + 1);
            Fill(x, y - 1, depth + 1);
        }

        if ((uint)startX < (uint)width && (uint)startY < (uint)height)
            Fill(startX, startY, 0);

        return new FloodFillResult(cells, calls, maxDepth, stopped);
    }
}