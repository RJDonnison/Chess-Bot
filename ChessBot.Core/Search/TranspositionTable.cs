using System.Runtime.InteropServices;

namespace ChessBot.Core.Search;

public class TranspositionTable
{
    private readonly Entry[] _entries;
    private readonly ulong _count;

    public TranspositionTable(int sizeMb = 64)
    {
        int entrySize = Marshal.SizeOf<Entry>(); 
        int capacity = sizeMb * 1024 * 1024 / entrySize;

        _count = (ulong)(capacity);
        _entries = new Entry[capacity];
    } 
    
    private int Index(ulong key) => (int)(key % _count);
    
    public void Store(ulong key, int score, int depth)
    {
        int index = Index(key);
        
       _entries[index] =  new Entry(score, depth);
    }

    public Entry TryGet(ulong key) => _entries[Index(key)];
    
    public struct Entry
    {
        public readonly int Score;
        public readonly int Depth;

        public Entry(int score, int depth)
        {
            Score = score;
            Depth = depth;
        }
        
        public static int GetSize()
        {
            return Marshal.SizeOf<Entry>();
        }
    }
}
