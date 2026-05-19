namespace ChessBot.Core.Evaluation.PieceSquareTables;

public static class PawnPieceSquare
{
    // Opening: reward center control, penalise edge/backward pawns, bonus for advanced ranks
    private static readonly int[] StartTable =
    {
        0,   0,   0,   0,   0,   0,   0,   0,  // rank 8 (promotion rank)
        50,  50,  50,  50,  50,  50,  50,  50,  // rank 7
        10,  10,  20,  30,  30,  20,  10,  10,  // rank 6
        5,   5,  10,  25,  25,  10,   5,   5,  // rank 5
        0,   0,   0,  20,  20,   0,   0,   0,  // rank 4
        5,  -5, -10,   0,   0, -10,  -5,   5,  // rank 3
        5,  10,  10, -20, -20,  10,  10,   5,  // rank 2
        0,   0,   0,   0,   0,   0,   0,   0,  // rank 1 (home rank)
    };

    // Endgame: push pawns forward aggressively, edge files less penalised
    private static readonly int[] EndTable =
    {
        0,   0,   0,   0,   0,   0,   0,   0,  // rank 8 (promotion rank)
        80,  80,  80,  80,  80,  80,  80,  80,  // rank 7
        50,  50,  50,  50,  50,  50,  50,  50,  // rank 6
        30,  30,  30,  30,  30,  30,  30,  30,  // rank 5
        20,  20,  20,  20,  20,  20,  20,  20,  // rank 4
        10,  10,  10,  10,  10,  10,  10,  10,  // rank 3
        5,   5,   5,   5,   5,   5,   5,   5,  // rank 2
        0,   0,   0,   0,   0,   0,   0,   0,  // rank 1 (home rank)
    };

    public static readonly int BaseValue = 100;

    public static int GetMgValue(int square) => StartTable[square] + BaseValue;

    public static int GetEgValue(int square) => EndTable[square] + BaseValue;
}