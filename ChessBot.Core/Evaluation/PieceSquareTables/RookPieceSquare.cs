namespace ChessBot.Core.Evaluation.PieceSquareTables;

public class RookPieceSquare
{
    // Opening: 7th rank pressure, open files, avoid a/h files early
    private static readonly int[] StartTable =
    {
        0,   0,   0,   0,   0,   0,   0,   0,  // rank 8
        5,  10,  10,  10,  10,  10,  10,   5,  // rank 7
        -5,   0,   0,   0,   0,   0,   0,  -5,  // rank 6
        -5,   0,   0,   0,   0,   0,   0,  -5,  // rank 5
        -5,   0,   0,   0,   0,   0,   0,  -5,  // rank 4
        -5,   0,   0,   0,   0,   0,   0,  -5,  // rank 3
        -5,   0,   0,   5,   5,   0,   0,  -5,  // rank 2
        0,   0,   0,   5,   5,   0,   0,   0,  // rank 1
    };

    // Endgame: 7th rank dominant, centralise, all files equal value
    private static readonly int[] EndTable =
    {
        10,  10,  10,  10,  10,  10,  10,  10,  // rank 8
        15,  15,  15,  15,  15,  15,  15,  15,  // rank 7
        0,   5,   5,   5,   5,   5,   5,   0,  // rank 6
        0,   5,   5,   5,   5,   5,   5,   0,  // rank 5
        0,   5,   5,   5,   5,   5,   5,   0,  // rank 4
        0,   5,   5,   5,   5,   5,   5,   0,  // rank 3
        0,   5,   5,   5,   5,   5,   5,   0,  // rank 2
        0,   0,   5,   5,   5,   5,   0,   0,  // rank 1
    };

    private static readonly int BaseValue = 500;

    public static int GetMgValue(int square) => StartTable[square] + BaseValue;

    public static int GetEgValue(int square) => EndTable[square] + BaseValue;
}