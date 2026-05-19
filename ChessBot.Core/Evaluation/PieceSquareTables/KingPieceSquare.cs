namespace ChessBot.Core.Evaluation.PieceSquareTables;

public class KingPieceSquare
{
    // Opening: king safety in corners (kingside/queenside castling), avoid center
    private static readonly int[] StartTable =
    {
        -30, -40, -40, -50, -50, -40, -40, -30,  // rank 8
        -30, -40, -40, -50, -50, -40, -40, -30,  // rank 7
        -30, -40, -40, -50, -50, -40, -40, -30,  // rank 6
        -20, -30, -30, -40, -40, -30, -30, -20,  // rank 5
        -10, -20, -20, -30, -30, -20, -20, -10,  // rank 4
        10,  0,   0,  -10, -10,   0,   0,  10,  // rank 3
        20,  30,  10,   0,   0,  10,  30,  20,  // rank 2
        20,  20,   0,   0,   0,   0,  20,  20,  // rank 1
    };

    // Endgame: king activity, centralization is crucial, edges are bad
    private static readonly int[] EndTable =
    {
        -50, -40, -30, -20, -20, -30, -40, -50,  // rank 8
        -40, -30, -20, -10, -10, -20, -30, -40,  // rank 7
        -30, -20, -10,   0,   0, -10, -20, -30,  // rank 6
        -20, -10,   0,  10,  10,   0, -10, -20,  // rank 5
        -20, -10,   0,  10,  10,   0, -10, -20,  // rank 4
        -30, -20, -10,   0,   0, -10, -20, -30,  // rank 3
        -40, -30, -20, -10, -10, -20, -30, -40,  // rank 2
        -50, -40, -30, -20, -20, -30, -40, -50,  // rank 1
    };

    private static readonly int BaseValue = 0;  // King has no material value for endgame purposes

    public static int GetMgValue(int square) => StartTable[square] + BaseValue;

    public static int GetEgValue(int square) => EndTable[square] + BaseValue;
}
