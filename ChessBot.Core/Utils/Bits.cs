namespace ChessBot.Core.Utils;

public static class Bits
{
    public static ulong SetBit(int square)
    {
        return 1UL << square;
    }

    public static bool IsValidSquare(int file, int rank)
    {
        return file >= 0 && file < 8 && rank >= 0 && rank < 8;
    }
}