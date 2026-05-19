namespace ChessBot.Core.Evaluation.PieceSquareTables;

public class QueenPieceSquare
{
    // Opening: develop late, avoid early exposure, slight centre pull
    private static readonly int[] StartTable =
    {
        -20, -10, -10,  -5,  -5, -10, -10, -20,  // rank 8
        -10,   0,   0,   0,   0,   0,   0, -10,  // rank 7
        -10,   0,   5,   5,   5,   5,   0, -10,  // rank 6
        -5,   0,   5,   5,   5,   5,   0,  -5,  // rank 5
        0,   0,   5,   5,   5,   5,   0,  -5,  // rank 4
        -10,   5,   5,   5,   5,   5,   0, -10,  // rank 3
        -10,   0,   5,   0,   0,   0,   0, -10,  // rank 2
        -20, -10, -10,  -5,  -5, -10, -10, -20,  // rank 1
    };

    // Endgame: centralise aggressively, edge penalty increases
    private static readonly int[] EndTable =
    {
        -20, -10, -10,  -5,  -5, -10, -10, -20,  // rank 8
        -10,   0,   5,   5,   5,   5,   0, -10,  // rank 7
        -10,   5,  10,  10,  10,  10,   5, -10,  // rank 6
        -5,   5,  10,  15,  15,  10,   5,  -5,  // rank 5
        -5,   5,  10,  15,  15,  10,   5,  -5,  // rank 4
        -10,   5,  10,  10,  10,  10,   5, -10,  // rank 3
        -10,   0,   5,   5,   5,   5,   0, -10,  // rank 2
        -20, -10, -10,  -5,  -5, -10, -10, -20,  // rank 1
    };

    private static readonly int BaseValue = 900;

    public static int GetMgValue(int square) => StartTable[square] + BaseValue;

    public static int GetEgValue(int square) => EndTable[square] + BaseValue;
}