namespace ChessBot.Core.Search;

public class RepetitionTable
{
    private readonly ulong[] _hashes;
    private int _count = 0;

    public RepetitionTable()
    {
        _hashes = new ulong[256];
    }

    public void Push(ulong hash)
    {
        if (_count < _hashes.Length)
            _hashes[_count] = hash;
        _count++;
    }

    public void TryPop()
    {
        _count = Math.Max(0, _count - 1);
    }

    public bool Contains(ulong h)
    {
        for (int i = 0; i < _count - 1; i++)
        {
            if (_hashes[i] == h)
                return true;
        }
        return false;
    }
}