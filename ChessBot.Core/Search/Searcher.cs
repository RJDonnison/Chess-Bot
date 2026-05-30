using ChessBot.Core.Core;
using ChessBot.Core.Evaluation;
using ChessBot.Core.MoveGen;

namespace ChessBot.Core.Search;

using static MoveOrderer;

public class Searcher
{
    private const int Infinity = 30000;
    private const int MateScore = 29000;

    private Board _board = null!;
    private Move _bestMove;
    private int _bestScore;
    private bool _abortSearch;
    
    private readonly MoveGenerator _generator = new();
    private readonly RepetitionTable _repetitionTable = new();
    private readonly TranspositionTable _tt = new();

    public void StartSearch(Board board)
    {
        _board = board;
        _abortSearch = false;
        _bestMove = default;
        _bestScore = -Infinity;
        
        for (int depth = 1; depth <= 100; depth++)
        {
            int alpha = -Infinity;

            Span<Move> moves = _generator.GenerateMoves(_board);
            OrderMoves(moves, _board, _bestMove);

            for (int i = 0; i < moves.Length; i++)
            {
                if (_abortSearch) break;

                _board.MakeMove(moves[i]);
                _repetitionTable.Push(_board.ZobristKey);

                int score = -Search(depth - 1, ply: 1, -Infinity, -alpha);

                _repetitionTable.TryPop();
                _board.UnmakeMove(moves[i]);

                if (score > alpha)
                {
                    alpha = score;
                    _bestScore = score;
                    _bestMove = moves[i];
                }
            }

            if (!_abortSearch)
                Console.WriteLine($"Depth: {depth} | Score: {_bestScore}");

            // Stop if we've found a mate or search stopped
            if (_abortSearch)
                break;
            if (Math.Abs(_bestScore) >= MateScore - 500)
                break;
        }
    }

    public void StopSearch()
    {
        _abortSearch = true;
    }
    
    public Move GetFoundMove()
    {
        Console.WriteLine($"Search complete | Best: {_bestMove} | Score: {_bestScore}");
        return _bestMove;
    }
    
    private int Search(int depth, int ply, int alpha, int beta)
    {
        if (_abortSearch)
            return 0;
        
        if (_board.Drawn || _repetitionTable.Contains(_board.ZobristKey))
            return 0;

        if (depth == 0)
            return SearchCapturesOnly(alpha, beta);
        
        int? ttScore = _tt.TryGetScore(_board.ZobristKey, depth, ply, alpha, beta);
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
                _tt.Store(_board.ZobristKey, score, depth, ply, move, TranspositionTable.Lowerbound); 
                return beta;
            }

            if (score > alpha)
            {
                alpha = score;
                bestMove = move;
            }
        }
        
        int flag = alpha > originalAlpha ? TranspositionTable.Exact : TranspositionTable.Upperbound;
        _tt.Store(_board.ZobristKey, alpha, depth, ply, bestMove, flag);
        return alpha;
    }

    private int SearchCapturesOnly(int alpha, int beta)
    {
        if (_abortSearch)
            return 0;
        
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
