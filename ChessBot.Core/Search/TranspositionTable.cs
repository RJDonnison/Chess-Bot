using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ChessBot.Core.MoveGen;

namespace ChessBot.Core.Search;

public class TranspositionTable
{
    public const int Exact = 0;
    public const int Lowerbound = 1;
    public const int Upperbound = 2;
    private const int MateScoreThreshold = 28000;
    
    private readonly Entry[] _entries;
    private readonly ulong _mask;

    public TranspositionTable(int sizeMb = 64)
    {
        int entrySize = Unsafe.SizeOf<Entry>(); 
        int capacity = 1 << (int)Math.Log2(sizeMb * 1024 * 1024 / entrySize);
        _mask = (ulong)(capacity - 1);
        
        _entries = new Entry[capacity];
    } 
    
    private int Index(ulong key) => (int)(key & _mask);
    
    public void Store(ulong key, int score, int depth, int ply, Move bestMove, int flag)
    {
        score = NormalizeMateScoreForStorage(score, ply);
        _entries[Index(key)] = new Entry(key, score, depth, bestMove, flag);
    }

    public Move GetBestMove(ulong key)
    {
        Entry entry = _entries[Index(key)];
        return entry.Key == key ? entry.BestMove : default;
    }
    
    public int? TryGetScore(ulong key, int depth, int ply, int alpha, int beta)
    {
        Entry entry = _entries[Index(key)];

        if (entry.Key != key) return null; // Collision
        if (entry.Depth < depth) return null; // Too shallow

        int score = NormalizeMateScoreForRetrieval(entry.Score, ply);

        if (entry.Flag == Exact) return score;
        if (entry.Flag == Lowerbound) alpha = Math.Max(alpha, score);
        if (entry.Flag == Upperbound) beta = Math.Min(beta, score);

        if (alpha >= beta) return score;

        return null; // Bounds didn't cause a cutoff
    }
    
    public struct Entry
    {
        public readonly ulong Key;
        public readonly int Score;
        public readonly int Depth;
        public readonly Move BestMove;
        public readonly int Flag;

        public Entry(ulong key, int score, int depth, Move bestMove, int flag)
        {
            Key = key;
            Score = score;
            Depth = depth;
            BestMove = bestMove;
            Flag = flag;
        }
    }

    private static int NormalizeMateScoreForStorage(int score, int ply)
    {
        if (score >= MateScoreThreshold)
            return score + ply;

        if (score <= -MateScoreThreshold)
            return score - ply;

        return score;
    }

    private static int NormalizeMateScoreForRetrieval(int score, int ply)
    {
        if (score >= MateScoreThreshold)
            return score - ply;

        if (score <= -MateScoreThreshold)
            return score + ply;

        return score;
    }
}
