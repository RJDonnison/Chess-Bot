using System.Numerics;
using ChessBot.Core.Core;

namespace ChessBot.Core.Evaluation;

public class Evaluator
{
    private static readonly int[] PieceValues =
    {
        100,  // Pawn   = 0
        320,  // Knight = 1
        330,  // Bishop = 2
        500,  // Rook   = 3
        900,  // Queen  = 4
        0,    // King   = 5
    };

    public int Evaluate(Board board)
    {
        int score = 0;

        for (int piece = 0; piece < 6; piece++)
        {
            ulong whiteBB = board.Bitboards[(int)Color.White, piece];
            ulong blackBB = board.Bitboards[(int)Color.Black, piece];

            score += BitOperations.PopCount(whiteBB) * PieceValues[piece];
            score -= BitOperations.PopCount(blackBB) * PieceValues[piece];
        }

        return board.ToMove == (int)Color.White ? score : -score;
    }
}