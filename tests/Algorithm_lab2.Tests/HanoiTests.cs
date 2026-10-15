using Algorithm_lab2.Hanoi;

namespace Algorithm_lab2.Tests;

public class HanoiTests
{
    public static TheoryData<int> DiskCounts => [1, 2, 3, 5, 8, 12];

    [Theory]
    [MemberData(nameof(DiskCounts))]
    public void Recursive_UsesMinimumMovesAndSolvesPuzzle(int disks) =>
        AssertSolves(HanoiSolver.SolveRecursive(disks), disks);

    [Theory]
    [MemberData(nameof(DiskCounts))]
    public void Stack_UsesMinimumMovesAndSolvesPuzzle(int disks) =>
        AssertSolves(HanoiSolver.SolveWithStack(disks), disks);

    [Theory]
    [MemberData(nameof(DiskCounts))]
    public void Stack_ProducesSameMovesAsRecursive(int disks) =>
        Assert.Equal(HanoiSolver.SolveRecursive(disks).Moves, HanoiSolver.SolveWithStack(disks).Moves);

    [Fact]
    public void EightDisks_Take255Moves() =>
        Assert.Equal(255, HanoiSolver.SolveRecursive(8).Moves.Count);

    [Fact]
    public void Recursive_DepthGrowsLinearly() =>
        Assert.Equal(9, HanoiSolver.SolveRecursive(8).MaxDepth);

    [Fact]
    public void State_RejectsBiggerDiskOnSmaller()
    {
        var state = new HanoiState(2);
        state.Apply(new HanoiMove(1, 0, 1));

        Assert.Throws<InvalidOperationException>(() => state.Apply(new HanoiMove(2, 0, 1)));
    }

    [Fact]
    public void State_RejectsMovingDiskThatIsNotOnTop()
    {
        var state = new HanoiState(3);

        Assert.Throws<InvalidOperationException>(() => state.Apply(new HanoiMove(2, 0, 2)));
    }

    private static void AssertSolves(HanoiSolution solution, int disks)
    {
        Assert.Equal(HanoiSolver.MinMoves(disks), solution.Moves.Count);

        var state = new HanoiState(disks);
        foreach (var move in solution.Moves)
            state.Apply(move);

        Assert.True(state.IsSolved());
    }
}
