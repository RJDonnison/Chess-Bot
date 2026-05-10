using System.Numerics;

namespace ChessBot.Core.Utils;

public static class Bits
{
    public static ulong SetBit(int square) => 1UL << square;


    public static bool IsValidSquare(int file, int rank) => file >= 0 && file < 8 && rank >= 0 && rank < 8;

    public static int LSB(ref ulong bb)
    {
        int sq = BitOperations.TrailingZeroCount(bb);
        bb &= bb - 1;
        return sq;
    }
}