using ChessBot.Core.Core;
using ChessBot.Core.Utilities;

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

    public override string ToString()
    {
        string from = BoardHelper.SquareToString(From);
        string to = BoardHelper.SquareToString(To);

        if (Promotion != null)
            return $"{from}{to}{BoardHelper.PieceToChar(Promotion)}";

        return $"{from}{to}";
    }
}