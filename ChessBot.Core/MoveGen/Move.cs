using ChessBot.Core.Core;
using ChessBot.Core.Utilities;

namespace ChessBot.Core.MoveGen;

public struct Move : IEquatable<Move>
{
    private readonly ushort _data;

    public int From => _data & 0x3F;
    public int To => (_data >> 6) & 0x3F;
    public bool IsEnPassant => (_data & (1 << 12)) != 0;
    public Piece? Promotion
    {
        get
        {
            int promotionCode = (_data >> 13) & 0x7;
            if (promotionCode == 0)
                return null;

            return (Piece)promotionCode;
        }
    }

    public Move(int from, int to, bool isEnPassant = false)
    {
        _data = (ushort)(from | (to << 6) | (isEnPassant ? 1 << 12 : 0));
    }

    public Move(int from, int to, Piece promotion)
    {
        if (promotion is Piece.Pawn or Piece.King)
            throw new ArgumentOutOfRangeException(nameof(promotion));

        _data = (ushort)(from | (to << 6) | ((int)promotion << 13));
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
        return _data == other._data;
    }

    public override bool Equals(object? obj)
    {
        return obj is Move other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _data.GetHashCode();
    }

    public static bool operator ==(Move left, Move right) => left.Equals(right);
    public static bool operator !=(Move left, Move right) => !left.Equals(right);
}
