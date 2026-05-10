using System.Text;

namespace ChessBot.Core.Utils;

public static class BitboardVisualizer
{
    public static ulong Bitboard { private get; set; }

    public static ulong GetBitboard()
    {
        ulong bitboard = Bitboard;
        Bitboard = 0UL;
        return bitboard;
    }
    
    public static void ToBoard(ulong bitboard)
    {
        StringBuilder sb = new();

        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;

                bool isSet = ((bitboard >> square) & 1UL) != 0;

                sb.Append(isSet ? "1 " : ". ");
            }

            sb.AppendLine();
        }

        Console.WriteLine(sb.ToString());
    }
}