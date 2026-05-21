namespace ChessBot.Core.Evaluation.PieceSquareTables;

public class KnightPieceSquare
{
    // Opening: centralise, penalise rim, avoid corners
    private static readonly int[] StartTable =
    {
        -50, -40, -30, -30, -30, -30, -40, -50,  // rank 8
        -40, -20,   0,   0,   0,   0, -20, -40,  // rank 7
        -30,   0,  10,  15,  15,  10,   0, -30,  // rank 6
        -30,   5,  15,  20,  20,  15,   5, -30,  // rank 5
        -30,   0,  15,  20,  20,  15,   0, -30,  // rank 4
        -30,   5,  10,  15,  15,  10,   5, -30,  // rank 3
        -40, -20,   0,   5,   5,   0, -20, -40,  // rank 2
        -50, -40, -30, -30, -30, -30, -40, -50,  // rank 1
    };

    // Endgame: centralisation still key, rim penalty softens slightly
    private static readonly int[] EndTable =
    {
        -50, -40, -30, -30, -30, -30, -40, -50,  // rank 8
        -40, -20,   0,   0,   0,   0, -20, -40,  // rank 7
        -30,   0,  15,  20,  20,  15,   0, -30,  // rank 6
        -30,   5,  20,  25,  25,  20,   5, -30,  // rank 5
        -30,   0,  20,  25,  25,  20,   0, -30,  // rank 4
        -30,   5,  15,  20,  20,  15,   5, -30,  // rank 3
        -40, -20,   0,   0,   0,   0, -20, -40,  // rank 2
        -50, -40, -30, -30, -30, -30, -40, -50,  // rank 1
    };

    public static readonly int BaseValue = 300;

    public static int GetMgValue(int square) => StartTable[square] + BaseValue;

    public static int GetEgValue(int square) => EndTable[square] + BaseValue;
}