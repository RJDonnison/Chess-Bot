using ChessBot.Core.Core;
using ChessBot.Core.Evaluation;
using ChessBot.Core.MoveGen;

namespace ChessBot.Core.Search;

using static MoveOrderer;

public class Searcher
{
    private const int Depth = 7;
    private const int Infinity = 30000;
    private const int MateScore = 29000;

    private Board _board = null!;
    
    private readonly MoveGenerator _generator = new();
    private readonly RepetitionTable _repetitionTable = new();
    private readonly TranspositionTable _tt = new();

    public Move GetBestMove(Board board)
    {
        _board = board;
        Move bestMove = default;
        int alpha = -Infinity;

        Span<Move> moves = _generator.GenerateMoves(_board);

        OrderMoves(moves, _board);
        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];

            _board.MakeMove(move);
            _repetitionTable.Push(_board.ZobristKey);

            int score = -Search(Depth - 1, 1, -Infinity, -alpha);

            _repetitionTable.TryPop();
            _board.UnmakeMove(move);

            if (score > alpha)
            {
                alpha = score;
                bestMove = move;
            }
        }

        return bestMove;
    }
    
    private int Search(int depth, int ply, int alpha, int beta)
    {
        if (_board.Drawn || _repetitionTable.Contains(_board.ZobristKey))
            return 0;

        if (depth == 0)
            return SearchCapturesOnly(alpha, beta);
        
        int? ttScore = _tt.TryGetScore(_board.ZobristKey, depth, alpha, beta);
        if (ttScore.HasValue)
            return ttScore.Value;

        Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];

        int moveCount = _generator.GenerateMoves(_board, ref moves);
        if (moveCount == 0)
            return _generator.IsInCheck() ? -MateScore + ply : 0;

        Move ttMove = _tt.GetBestMove(_board.ZobristKey);
        OrderMoves(moves[..moveCount], _board, ttMove);
        
        int originalAlpha = alpha;
        Move bestMove = default;
        for (int i = 0; i < moveCount; i++)
        {
            var move = moves[i];
            _board.MakeMove(move);
            _repetitionTable.Push(_board.ZobristKey);

            int score = -Search(depth - 1, ply + 1, -beta, -alpha);

            _repetitionTable.TryPop();
            _board.UnmakeMove(move);
            if (score >= beta)
            {
                _tt.Store(_board.ZobristKey, score, depth, move, TranspositionTable.Lowerbound); 
                return beta;
            }

            if (score > alpha)
            {
                alpha = score;
                bestMove = move;
            }
        }
        
        int flag = alpha > originalAlpha ? TranspositionTable.Exact : TranspositionTable.Upperbound;
        _tt.Store(_board.ZobristKey, alpha, depth, bestMove, flag);
        return alpha;
    }

    private int SearchCapturesOnly(int alpha, int beta)
    {
        int evaluation = Evaluator.Evaluate(_board);
        if (evaluation >= beta)
            return beta;
        alpha = int.Max(alpha, evaluation);

        Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];

        int moveCount = _generator.GenerateMoves(_board, ref moves, true);
        
        OrderMoves(moves[..moveCount], _board);
        
        for (int i = 0; i < moveCount; i++)
        {
            var move = moves[i];
            _board.MakeMove(move);
            _repetitionTable.Push(_board.ZobristKey);

            int score = -SearchCapturesOnly(-beta, -alpha);

            _repetitionTable.TryPop();
            _board.UnmakeMove(move);
            if (score >= beta)
                return beta;
            alpha = int.Max(alpha, score);
        }

        return alpha;
    }
}
