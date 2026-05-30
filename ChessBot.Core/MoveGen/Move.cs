using ChessBot.Core.Core;
using ChessBot.Core.Utilities;

namespace ChessBot.Core.MoveGen;

public struct Move : IEquatable<Move>
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

    public bool Equals(Move other)
    {
        return From == other.From && To == other.To && Promotion == other.Promotion;
    }

    public override bool Equals(object? obj)
    {
        return obj is Move other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(From, To, Promotion);
    }
    
    public static bool operator ==(Move left, Move right) => left.Equals(right);
    public static bool operator !=(Move left, Move right) => !left.Equals(right);
}