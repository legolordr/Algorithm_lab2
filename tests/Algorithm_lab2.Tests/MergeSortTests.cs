using Algorithm_lab2.PersonalAlgorithms.MergeSort;

namespace Algorithm_lab2.Tests;

public class MergeSortTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(32)]
    [InlineData(1000)]
    public void SortsRandomArrays(int size)
    {
        var random = new Random(size);
        var input = Enumerable.Range(0, size).Select(_ => random.Next(-50, 50)).ToArray();

        var result = MergeSorter.Sort(input);

        Assert.Equal(input.Order(), result.Sorted);
    }

    [Fact]
    public void SortsReversedAndAlreadySortedArrays()
    {
        var sorted = Enumerable.Range(1, 20).ToArray();

        Assert.Equal(sorted, MergeSorter.Sort(sorted.Reverse().ToArray()).Sorted);
        Assert.Equal(sorted, MergeSorter.Sort(sorted).Sorted);
    }

    [Fact]
    public void DoesNotChangeInput()
    {
        int[] input = [5, 3, 1, 4, 2];

        MergeSorter.Sort(input);

        Assert.Equal([5, 3, 1, 4, 2], input);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(8, 15)]
    [InlineData(100, 199)]
    public void MakesTwoNMinusOneCalls(int size, long calls) =>
        // The recursion tree has n leaves (single elements) and n - 1 inner nodes (splits).
        Assert.Equal(calls, MergeSorter.Sort(new int[size]).Calls);

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(32, 6)]
    [InlineData(33, 7)]
    public void DepthIsLogarithmic(int size, int depth) =>
        Assert.Equal(depth, MergeSorter.Sort(new int[size]).MaxDepth);

    [Theory]
    [InlineData(16)]
    [InlineData(100)]
    public void ComparisonsStayWithinNLogN(int size)
    {
        var input = Enumerable.Range(0, size).Reverse().ToArray();
        var result = MergeSorter.Sort(input);
        var log = (int)Math.Ceiling(Math.Log2(size));

        Assert.InRange(result.Comparisons, 1, (long)size * log);
        Assert.InRange(result.Writes, result.Comparisons, (long)size * log);
    }

    [Fact]
    public void PowerOfTwo_WritesEveryElementOnEveryLevel() =>
        // 16 elements -> 4 levels of merging, each level writes all 16 elements back.
        Assert.Equal(16 * 4, MergeSorter.Sort(new int[16]).Writes);

    [Fact]
    public void ReplayingWriteSteps_ProducesSortedArray()
    {
        int[] input = [38, 27, 43, 3, 9, 82, 10];
        var result = MergeSorter.Sort(input);
        var replay = (int[])input.Clone();

        foreach (var write in result.Steps.OfType<WriteStep>())
            replay[write.Index] = write.Value;

        Assert.Equal([3, 9, 10, 27, 38, 43, 82], replay);
    }

    [Fact]
    public void StepsMatchCounters()
    {
        var result = MergeSorter.Sort([5, 1, 4, 2, 3, 9, 0]);

        Assert.Equal(result.Comparisons, result.Steps.OfType<CompareStep>().Count());
        Assert.Equal(result.Writes, result.Steps.OfType<WriteStep>().Count());
        Assert.Equal(result.Writes, result.Steps.OfType<TakeStep>().Count());
        Assert.Equal(result.Steps.OfType<SplitStep>().Count(), result.Steps.OfType<MergedStep>().Count());

        var last = Assert.IsType<MergedStep>(result.Steps[^1]);
        Assert.Equal((0, 6), (last.Left, last.Right));
    }

    [Fact]
    public void EachMerge_TakesEveryElementOfItsRangeInSortedOrder()
    {
        int[] input = [38, 27, 43, 3, 9, 82, 10];
        var steps = MergeSorter.Sort(input).Steps;

        foreach (var (merge, index) in steps.Select((s, i) => (s, i)).Where(p => p.s is MergeStep))
        {
            var m = (MergeStep)merge;
            var takes = steps.Skip(index + 1).TakeWhile(s => s is not MergedStep).OfType<TakeStep>().ToArray();

            Assert.Equal(Enumerable.Range(m.Left, m.Right - m.Left + 1), takes.Select(t => t.Target));
            Assert.Equal(Enumerable.Range(m.Left, m.Right - m.Left + 1), takes.Select(t => t.Source).Order());
            Assert.Equal(takes.Select(t => t.Value).Order(), takes.Select(t => t.Value));
        }
    }

    [Fact]
    public void WithoutRecording_HasNoStepsButSameResult()
    {
        int[] input = [3, 1, 2];
        var result = MergeSorter.Sort(input, recordSteps: false);

        Assert.Empty(result.Steps);
        Assert.Equal([1, 2, 3], result.Sorted);
    }
}
