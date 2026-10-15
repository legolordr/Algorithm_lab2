namespace Algorithm_lab2.Hanoi;

/// <summary>Move the top disk (1 = smallest) from one rod to another; rods are numbered 0..2.</summary>
public readonly record struct HanoiMove(int Disk, int From, int To)
{
    public override string ToString() => $"Диск {Disk}: {HanoiState.RodName(From)} → {HanoiState.RodName(To)}";
}

/// <summary>The moves and the deepest point the algorithm reached (call depth or explicit stack size).</summary>
public sealed record HanoiSolution(IReadOnlyList<HanoiMove> Moves, int MaxDepth);

public static class HanoiSolver
{
    public static long MinMoves(int disks) => (1L << disks) - 1;

    /// <summary>
    /// Classic recursion: move n-1 disks out of the way, move the biggest disk, put n-1 disks back on top of it.
    /// </summary>
    public static HanoiSolution SolveRecursive(int disks, int from = 0, int to = 2, int via = 1)
    {
        var moves = new List<HanoiMove>();
        var maxDepth = 0;

        void Move(int n, int source, int target, int spare, int depth)
        {
            maxDepth = Math.Max(maxDepth, depth);
            if (n == 0)
                return;

            Move(n - 1, source, spare, target, depth + 1);
            moves.Add(new HanoiMove(n, source, target));
            Move(n - 1, spare, target, source, depth + 1);
        }

        Move(disks, from, to, via, 1);
        return new HanoiSolution(moves, maxDepth);
    }

    /// <summary>
    /// The same algorithm without recursion: the call stack is replaced by an explicit <see cref="Stack{T}"/>.
    /// Each frame is a "task"; a task that is not expanded yet is split into the same three steps as in
    /// <see cref="SolveRecursive"/>, pushed in reverse order because a stack is LIFO.
    /// </summary>
    public static HanoiSolution SolveWithStack(int disks, int from = 0, int to = 2, int via = 1)
    {
        var moves = new List<HanoiMove>();
        var stack = new Stack<Frame>();
        var maxDepth = 0;

        stack.Push(new Frame(disks, from, to, via, Expanded: false));
        while (stack.Count > 0)
        {
            maxDepth = Math.Max(maxDepth, stack.Count);
            var (n, source, target, spare, expanded) = stack.Pop();
            if (n == 0)
                continue;

            if (expanded)
            {
                // Both subtasks before this one are done: it is time to move disk n itself.
                moves.Add(new HanoiMove(n, source, target));
                continue;
            }

            stack.Push(new Frame(n - 1, spare, target, source, Expanded: false));
            stack.Push(new Frame(n, source, target, spare, Expanded: true));
            stack.Push(new Frame(n - 1, source, spare, target, Expanded: false));
        }

        return new HanoiSolution(moves, maxDepth);
    }

    private readonly record struct Frame(int N, int From, int To, int Via, bool Expanded);
}
