using Algorithm_lab2.PersonalAlgorithms.Maze;

namespace Algorithm_lab2.Tests;

public class MazeTests
{
    public static TheoryData<int, int> Sizes => new()
    {
        { 1, 1 },
        { 5, 5 },
        { 7, 3 },
        { 20, 20 },
    };

    [Theory]
    [MemberData(nameof(Sizes))]
    public void EveryCellIsVisitedExactlyOnce(int width, int height)
    {
        // Many seeds: the old shared-buffer bug left cells unvisited in about a quarter of the mazes.
        for (var seed = 0; seed < 200; seed++)
        {
            var maze = MazeGenerator.Generate(width, height, seed);
            var entered = maze.Steps.Where(s => s.Kind == MazeStepKind.Enter).Select(s => (s.X, s.Y)).ToArray();

            Assert.Equal(width * height, maze.Calls);
            Assert.Equal(width * height, entered.Length);
            Assert.Equal(width * height, entered.Distinct().Count());
        }
    }

    [Fact]
    public void Maze_IsATree_WithOnePassagePerCellExceptTheStart()
    {
        var maze = MazeGenerator.Generate(15, 15, seed: 42);

        // Every cell but the first is entered from a neighbor, which opens exactly one wall: cells − 1 passages.
        var passages = maze.Steps.Count(s => s.Kind == MazeStepKind.Enter && s.FromDirection is not null);
        Assert.Equal(maze.InternalWalls, passages);
    }

    [Fact]
    public void SameSeed_GivesTheSameMaze()
    {
        var first = MazeGenerator.Generate(10, 10, seed: 7);
        var second = MazeGenerator.Generate(10, 10, seed: 7);

        Assert.Equal(first.Steps, second.Steps);
    }
}
