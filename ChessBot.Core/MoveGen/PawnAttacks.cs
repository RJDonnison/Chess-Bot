using ChessBot.Core.Utilities;

namespace ChessBot.Core.MoveGen;

public static class PawnAttacks
{
    public static ulong[,] Table { get; private set; }

    static PawnAttacks() => Table = InitTable();

    private static ulong[,] InitTable()
    {
        ulong[,] attackTable = new ulong[2, 64];

        for (int square = 0; square < 64; square++)
        {
            ulong pawn = 1UL << square;

            attackTable[0, square] = ((pawn << 9) & ~Masks.FileA)
                                     | ((pawn << 7) & ~Masks.FileH);

            attackTable[1, square] = ((pawn >> 7) & ~Masks.FileA)
                                     | ((pawn >> 9) & ~Masks.FileH);
        }

        return attackTable;
    }
}