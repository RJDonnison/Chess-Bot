using ChessBot.Core.Core;
using ChessBot.Core.MoveGen;

namespace ChessBot.Core.Utilities;

public static class BoardHelper
{
    public static string SquareToString(int square)
    {
        int file = square % 8;
        int rank = square / 8;

        char fileChar = (char)('a' + file);
        char rankChar = (char)('1' + rank);

        return $"{fileChar}{rankChar}";
    }

    public static int StringToSquare(string square)
    {
        int file = square[0] - 'a';
        int rank = square[1] - '1';
        return rank * 8 + file;
    }

    public static char PieceToChar(Piece? piece) => piece switch
    {
        Piece.Pawn => 'p',
        Piece.Knight => 'n',
        Piece.Bishop => 'b',
        Piece.Rook => 'r',
        Piece.Queen => 'q',
        Piece.King => 'k',

        _ => '.'
    };

    public static string MoveToString(Move move)
    {
        string files = "abcdefgh";
        string from = $"{files[move.From % 8]}{move.From / 8 + 1}";
        string to = $"{files[move.To % 8]}{move.To / 8 + 1}";
        return $"{from}{to}";
    }

    public static int Rank(int sq) => sq / 8;

    public static int File(int sq) => sq % 8;
}