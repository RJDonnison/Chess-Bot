using System.Numerics;
using System.Text;

namespace ChessBot.Core.Core;

public class Board
{
    public Color ToMove { get; set; } = Color.White;
    public ulong[,] Bitboards { get; } = new ulong[2, 6];

    public ulong WhitePieces => Bitboards[(int)Color.White, (int)Piece.Pawn] | Bitboards[(int)Color.White, (int)Piece.Knight] | Bitboards[(int)Color.White, (int)Piece.Bishop] | Bitboards[(int)Color.White, (int)Piece.Rook] | Bitboards[(int)Color.White, (int)Piece.Queen] | Bitboards[(int)Color.White, (int)Piece.King];
    public ulong BlackPieces => Bitboards[(int)Color.Black, (int)Piece.Pawn] | Bitboards[(int)Color.Black, (int)Piece.Knight] | Bitboards[(int)Color.Black, (int)Piece.Bishop] | Bitboards[(int)Color.Black, (int)Piece.Rook] | Bitboards[(int)Color.Black, (int)Piece.Queen] | Bitboards[(int)Color.Black, (int)Piece.King];

    public ulong FriendlyPieces => ToMove == Color.White ? WhitePieces : BlackPieces;
    public ulong EnemyPieces => ToMove == Color.White ? BlackPieces : WhitePieces;

    public ulong Occupied => WhitePieces | BlackPieces;
    public ulong Empty => ~Occupied;

    public override string ToString()
    {
        char[] squares = new char[64];

        Array.Fill(squares, '.');

        for (int color = 0; color < 2; color++)
        {
            for (int piece = 0; piece < 6; piece++)
            {
                ulong bb = Bitboards[color, piece];

                while (bb != 0)
                {
                    int sq = BitOperations.TrailingZeroCount(bb);

                    char ch = PieceUtils.PieceToChar((Piece)piece);
                    squares[sq] = (Color)color == Color.White ? char.ToUpper(ch) : ch;

                    bb &= bb - 1;
                }
            }
        }

        StringBuilder sb = new();

        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                int sq = rank * 8 + file;

                sb.Append(squares[sq]);
                sb.Append(' ');
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}