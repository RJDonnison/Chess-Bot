using ChessBot.Core.Core;
using ChessBot.Core.Evaluation;
using ChessBot.Core.MoveGen;

namespace ChessBot.Core.Search;

public class Searcher
{
    private const int Depth = 3;
    private readonly Evaluator _evaluator = new();
    private readonly MoveGenerator _generator = new();

    public Move GetBestMove(Board board)
    {
        Move bestMove = default;
        int max = int.MinValue;

        List<Move> moves = _generator.GenerateMoves(board);
        foreach (var move in moves)
        {
            board.MakeMove(move);
            int score = -Negamax(board, Depth - 1);
            board.UnmakeMove(move);

            if (score > max)
            {
                max = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private int Negamax(Board board, int depth)
    {
        if (depth == 0)
            return _evaluator.Evaluate(board);

        List<Move> moves = _generator.GenerateMoves(board);
        if (moves.Count == 0)
            return board.IsInCheck() ? -30000 + (Depth - depth) : 0;

        int max = int.MinValue;
        foreach (var move in moves)
        {
            board.MakeMove(move);
            int score = -Negamax(board, depth - 1);
            max = int.Max(score, max);
            board.UnmakeMove(move);
        }

        return max;
    }
}