using ChessBot.Core.Utilities;

namespace ChessBot.Core.Tables;

public static class KingAttacks
{
    public static ulong[] Table { get; private set; }

    static KingAttacks() => Table = InitTable();

    private static ulong[] InitTable()
    {
        ulong[] attackTable = new ulong[64];

        for (int square = 0; square < 64; square++)
        {
            ulong king = 1UL << square;

            attackTable[square] =
                ((king << 8)) |  // north
                ((king >> 8)) |  // south
                ((king << 1) & ~Masks.FileA) |  // east
                ((king >> 1) & ~Masks.FileH) |  // west
                ((king << 9) & ~Masks.FileA) |  // north-east
                ((king << 7) & ~Masks.FileH) |  // north-west
                ((king >> 7) & ~Masks.FileA) |  // south-east
                ((king >> 9) & ~Masks.FileH);   // south-west
        }

        return attackTable;
    }
}