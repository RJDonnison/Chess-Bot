namespace ChessBot.Core.Search;

public class RepetitionTable
{
    private readonly ulong[] hashes;
    private int count = 0;

    public RepetitionTable()
    {
        hashes = new ulong[256];
    }

    public void Push(ulong hash)
    {
        if (count < hashes.Length)
            hashes[count] = hash;
        count++;
    }

    public void TryPop()
    {
        count = Math.Max(0, count - 1);
    }

    public bool Contains(ulong h)
    {
        for (int i = 0; i < count - 1; i++)
        {
            if (hashes[i] == h)
                return true;
        }
        return false;
    }
}