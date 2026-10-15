namespace Algorithm_lab2.Hanoi;

/// <summary>Three rods with disks; enforces the puzzle rules on every move.</summary>
public sealed class HanoiState
{
    private readonly Stack<int>[] _rods = [new(), new(), new()];

    public HanoiState(int disks, int startRod = 0)
    {
        Disks = disks;
        for (var disk = disks; disk >= 1; disk--)
            _rods[startRod].Push(disk);
    }

    public int Disks { get; }

    /// <summary>Disks on a rod from bottom to top.</summary>
    public IEnumerable<int> Rod(int index) => _rods[index].Reverse();

    public bool IsSolved(int targetRod = 2) => _rods[targetRod].Count == Disks;

    public static string RodName(int index) => ((char)('A' + index)).ToString();

    /// <summary>Takes the top disk off a rod (the first half of a move, used by the animation).</summary>
    public int Lift(int rod)
    {
        if (_rods[rod].Count == 0)
            throw new InvalidOperationException($"Стержень {RodName(rod)} пуст.");
        return _rods[rod].Pop();
    }

    /// <summary>Puts a disk on a rod (the second half of a move).</summary>
    public void Drop(int rod, int disk)
    {
        if (_rods[rod].TryPeek(out var top) && top < disk)
            throw new InvalidOperationException($"Нельзя класть диск {disk} на меньший диск {top}.");
        _rods[rod].Push(disk);
    }

    public void Apply(HanoiMove move)
    {
        if (!_rods[move.From].TryPeek(out var top) || top != move.Disk)
            throw new InvalidOperationException($"Диск {move.Disk} не лежит сверху стержня {RodName(move.From)}.");
        if (_rods[move.To].TryPeek(out var target) && target < move.Disk)
            throw new InvalidOperationException($"Нельзя класть диск {move.Disk} на меньший диск {target}.");
        _rods[move.To].Push(_rods[move.From].Pop());
    }
}
