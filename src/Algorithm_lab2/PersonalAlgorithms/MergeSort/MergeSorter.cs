namespace Algorithm_lab2.PersonalAlgorithms.MergeSort;

/// <summary>One recorded action of the sort, replayed by the animation.</summary>
public abstract record MergeSortStep;

/// <summary>A call <c>Sort(left, right)</c> splits its range into [left..middle] and [middle+1..right].</summary>
public sealed record SplitStep(int Left, int Middle, int Right, int Depth) : MergeSortStep;

/// <summary>Both halves are sorted; merging [left..middle] and [middle+1..right] starts.</summary>
public sealed record MergeStep(int Left, int Middle, int Right, int Depth) : MergeSortStep;

/// <summary>The heads of the two halves, <c>array[I]</c> and <c>array[J]</c>, are compared.</summary>
public sealed record CompareStep(int I, int J) : MergeSortStep;

/// <summary>During a merge the element <c>array[Source]</c> is moved to the buffer: <c>buffer[Target] = Value</c>.</summary>
public sealed record TakeStep(int Source, int Target, int Value) : MergeSortStep;

/// <summary>The merged value is copied back from the buffer: <c>array[Index] = Value</c>.</summary>
public sealed record WriteStep(int Index, int Value) : MergeSortStep;

/// <summary>The range [left..right] is now sorted.</summary>
public sealed record MergedStep(int Left, int Right, int Depth) : MergeSortStep;

public sealed record MergeSortResult(
    int[] Sorted,
    IReadOnlyList<MergeSortStep> Steps,
    long Calls,
    int MaxDepth,
    long Comparisons,
    long Writes);

public static class MergeSorter
{
    /// <summary>
    /// Top-down merge sort: split the range in half, sort each half recursively, merge the two sorted halves.
    /// The input array is not changed; the sorted copy is returned in <see cref="MergeSortResult.Sorted"/>.
    /// </summary>
    public static MergeSortResult Sort(int[] input, bool recordSteps = true)
    {
        var array = (int[])input.Clone();
        var buffer = new int[array.Length];   // one buffer for the whole sort instead of a new array per merge
        var steps = new List<MergeSortStep>();
        long calls = 0, comparisons = 0, writes = 0;
        var maxDepth = 0;

        void Record(MergeSortStep step)
        {
            if (recordSteps)
                steps.Add(step);
        }

        // Sorts array[left..right] inclusive. The root call has depth 1.
        void SortRange(int left, int right, int depth)
        {
            calls++;
            maxDepth = Math.Max(maxDepth, depth);

            if (left >= right)   // base case: 0 or 1 element is already sorted
                return;

            var middle = left + (right - left) / 2;   // not (left + right) / 2: that can overflow for huge arrays
            Record(new SplitStep(left, middle, right, depth));

            SortRange(left, middle, depth + 1);
            SortRange(middle + 1, right, depth + 1);
            Merge(left, middle, right, depth);
        }

        void Merge(int left, int middle, int right, int depth)
        {
            Record(new MergeStep(left, middle, right, depth));

            int i = left, j = middle + 1, k = left;

            void Take(int source)
            {
                buffer[k] = array[source];
                Record(new TakeStep(source, k, array[source]));
                k++;
            }

            // Take the smaller head each time. "<=" keeps equal elements in their original order (stable sort).
            while (i <= middle && j <= right)
            {
                comparisons++;
                Record(new CompareStep(i, j));
                Take(array[i] <= array[j] ? i++ : j++);
            }

            // One half is exhausted: the rest of the other one is already sorted, copy it as is.
            while (i <= middle) Take(i++);
            while (j <= right) Take(j++);

            for (var index = left; index <= right; index++)
            {
                array[index] = buffer[index];
                writes++;
                Record(new WriteStep(index, buffer[index]));
            }

            Record(new MergedStep(left, right, depth));
        }

        SortRange(0, array.Length - 1, 1);
        return new MergeSortResult(array, steps, calls, maxDepth, comparisons, writes);
    }
}
