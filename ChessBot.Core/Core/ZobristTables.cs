using System.Numerics;

namespace ChessBot.Core.Core;

public static class ZobristTables
{
    public static readonly ulong[,,] Pieces = new ulong[2, 6, 64];

    public static readonly ulong[] CastlingRights = new ulong[16];
    public static readonly ulong[] EnPassantFile = new ulong[8];
    public static readonly ulong SideToMove;

    static ZobristTables()
    {
        const int seed = 79302675;
        Random rng = new Random(seed);

        for (int color = 0; color < 2; color++)
        {
            for (int piece = 0; piece < 6; piece++)
            {
                for (int sq = 0; sq < 64; sq++)
                    Pieces[color, piece, sq] = RandomUlong(rng);
            }
        }

        for (int i = 0; i < CastlingRights.Length; i++)
            CastlingRights[i] = RandomUlong(rng);

        for (int i = 0; i < EnPassantFile.Length; i++)
            EnPassantFile[i] = RandomUlong(rng);

        SideToMove = RandomUlong(rng);
    }

    // Calculate zobrist key
    // Note, this is slow and should only be used from fen.
    public static ulong CalculateZobristKey(Board board)
    {
        ulong key = 0;

        for (int color = 0; color < 2; color++)
        {
            for (int piece = 0; piece < 6; piece++)
            {
                ulong bb = board.Bitboards[color, piece];
                while (bb != 0)
                {
                    int sq = BitOperations.TrailingZeroCount(bb);
                    key ^= Pieces[color, piece, sq];
                    bb &= bb - 1;
                }
            }
        }

        if (board.EnPassantSquare != null)
            key ^= EnPassantFile[board.EnPassantSquare.Value % 8];

        if (board.ToMove == (int)Color.Black)
            key ^= SideToMove;

        key ^= CastlingRights[board.CastlingRights];

        return key;
    }

    static ulong RandomUlong(Random rng)
    {
        byte[] buffer = new byte[8];
        rng.NextBytes(buffer);
        return BitConverter.ToUInt64(buffer, 0);
    }
}