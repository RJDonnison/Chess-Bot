using ChessBot.Core.Core;
using ChessBot.Core.Evaluation;
using ChessBot.Core.MoveGen;

namespace ChessBot.Core.Search;

public static class MoveOrderer
{
    public static void OrderMoves(Span<Move> moves, Span<int> scores, Board board)
    {
        for (int i = 0; i < moves.Length; i++)
        {
            int score = 0;
            Piece movePiece = (Piece)board.GetPieceAt(moves[i].From)!;
            Piece? captured = board.GetPieceAt(moves[i].To);

            if (captured != null)
                score += 10 * Evaluator.PieceValues[(int)captured] - Evaluator.PieceValues[(int)movePiece];

            if (moves[i].Promotion != null)
                score += Evaluator.PieceValues[(int)moves[i].Promotion!];

            scores[i] = score;
        }
    }
    
    // O(n) get move
    public static Move PickMove(Span<Move> moves, Span<int> scores, int start)
    {
        int best = start;
        for (int i = start + 1; i < moves.Length; i++)
            if (scores[i] > scores[best])
                best = i;

        (moves[start], moves[best]) = (moves[best], moves[start]);
        (scores[start], scores[best]) = (scores[best], scores[start]);

        return moves[start];
    }
}