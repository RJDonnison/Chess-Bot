using ChessBot.Core.Core;

namespace ChessBot.Core.MoveGen;

public struct Move
{
    public int From { get; private set; }
    public int To { get; private set; }
    public Piece? Promotion { get; private set; }

    public Move(int from, int to)
    {
        From = from;
        To = to;
    }

    public Move(int from, int to, Piece promotion)
    {
        From = from;
        To = to;
        Promotion = promotion;
    }

    public override string ToString() {
        string from = SquareToString(From);
        string to = SquareToString(To);

        if (Promotion != null)
            return $"{from}{to}{PieceUtils.PieceToChar(Promotion)}";

        return $"{from}{to}";
    }
    
    private static string SquareToString(int square)
    {
        int file = square % 8;
        int rank = square / 8;

        char fileChar = (char)('a' + file);
        char rankChar = (char)('1' + rank);

        return $"{fileChar}{rankChar}";
    }
}