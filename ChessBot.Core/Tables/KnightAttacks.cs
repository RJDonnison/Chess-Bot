using ChessBot.Core.Utils;

namespace ChessBot.Core.Tables;

public static class KnightAttacks
{
    public static ulong[] Table { get; private set; }

    static KnightAttacks() => Table = InitTable();

    private static ulong[] InitTable()
    {
        ulong[] attackTable = new ulong[64];

        for (int square = 0; square < 64; square++)
        {
            ulong knight = 1UL << square;

            attackTable[square] =
                ((knight << 17) & ~Masks.FileA) |  // up 2, right 1
                ((knight << 15) & ~Masks.FileH) |  // up 2, left 1
                ((knight >> 15) & ~Masks.FileA) |  // down 2, right 1
                ((knight >> 17) & ~Masks.FileH) |  // down 2, left 1
                ((knight << 10) & ~(Masks.FileA | Masks.FileB)) |  // up 1, right 2
                ((knight << 6) & ~(Masks.FileG | Masks.FileH)) |  // up 1, left 2
                ((knight >> 6) & ~(Masks.FileA | Masks.FileB)) |  // down 1, right 2
                ((knight >> 10) & ~(Masks.FileG | Masks.FileH));   // down 1, left 2
        }

        return attackTable;
    }
}