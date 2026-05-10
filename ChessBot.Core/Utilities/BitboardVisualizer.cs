namespace ChessBot.Core.Utilities;

public static class BitboardVisualizer
{
    public static ulong Bitboard { private get; set; }

    public static ulong GetBitboard()
    {
        ulong bitboard = Bitboard;
        Bitboard = 0UL;
        return bitboard;
    }
}