using ChessBot.Core.Core;
using ChessBot.Core.Evaluation;
using ChessBot.Core.MoveGen;

namespace ChessBot.Core.Search;

public class Searcher
{
    private const int Depth = 4;
    private readonly Evaluator _evaluator = new();
    private readonly MoveGenerator _generator = new();
    private readonly RepetitionTable _repetitionTable = new();

    public Move GetBestMove(Board board)
    {
        Move bestMove = default;
        int max = int.MinValue;

        Span<Move> moves = _generator.GenerateMoves(board);
        foreach (var move in moves)
        {
            board.MakeMove(move);
            _repetitionTable.Push(board.ZobristKey);

            int score = -Negamax(board, Depth - 1);

            _repetitionTable.TryPop();
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
        if (board.Drawn || _repetitionTable.Contains(board.ZobristKey))
            return 0;

        if (depth == 0)
            return _evaluator.Evaluate(board);

        Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
        int moveCount = _generator.GenerateMoves(board, ref moves);
        if (moveCount == 0)
            return _generator.IsInCheck() ? -30000 + (Depth - depth) : 0;

        int max = int.MinValue;
        for (int i = 0; i < moveCount; i++)
        {
            board.MakeMove(moves[i]);
            _repetitionTable.Push(board.ZobristKey);

            int score = -Negamax(board, depth - 1);
            max = int.Max(score, max);

            _repetitionTable.TryPop();
            board.UnmakeMove(moves[i]);
        }

        return max;
    }
}