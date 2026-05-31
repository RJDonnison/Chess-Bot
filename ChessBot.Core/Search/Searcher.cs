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

    private const int MaxPly = 64;
    private readonly Move[,] _killers = new Move[MaxPly, 2];

    private readonly MoveGenerator _generator = new();
    private readonly RepetitionTable _repetitionTable = new();
    private readonly TranspositionTable _tt = new();

    public void StartSearch(Board board)
    {
        _board = board;
        _abortSearch = false;
        _bestMove = default;
        _bestScore = -Infinity;
        Array.Clear(_killers, 0, _killers.Length);

        int previousScore = 0;

        for (int depth = 1; depth <= 100; depth++)
        {
            if (depth <= 4)
                SearchRoot(depth, -Infinity, Infinity);
            else
            {
                int delta = 25;
                int alpha = previousScore - delta;
                int beta = previousScore + delta;

                while (true)
                {
                    if (_abortSearch) break;

                    int score = SearchRoot(depth, alpha, beta);

                    if (score <= alpha)
                    {
                        alpha -= delta;
                        delta *= 2;
                    }
                    else if (score >= beta)
                    {
                        beta += delta;
                        delta *= 2;
                    }
                    else break;
                }
            }

            previousScore = _bestScore;

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
        return _bestMove;
    }

    private int SearchRoot(int depth, int alpha, int beta)
    {
        Span<Move> moves = _generator.GenerateMoves(_board);
        OrderMoves(moves, _board, _bestMove);

        for (int i = 0; i < moves.Length; i++)
        {
            if (_abortSearch) break;

            _board.MakeMove(moves[i]);
            _repetitionTable.Push(_board.ZobristKey);

            int score;
            if (i == 0)
                score = -Search(depth - 1, 1, -beta, -alpha);
            else
            {
                // Null window scout
                score = -Search(depth - 1, 1, -alpha - 1, -alpha);
                // Re-search with full window if it beat alpha unexpectedly
                if (score > alpha)
                    score = -Search(depth - 1, 1, -beta, -alpha);
            }

            _repetitionTable.TryPop();
            _board.UnmakeMove(moves[i]);

            if (score > alpha)
            {
                alpha = score;
                // Only commit if search wasn't aborted mid-move
                if (!_abortSearch)
                {
                    _bestScore = score;
                    _bestMove = moves[i];
                }
            }
        }

        return _bestScore;
    }

    private int Search(int depth, int ply, int alpha, int beta)
    {
        if (_abortSearch)
            return 0;

        if (_board.Drawn || _repetitionTable.Contains(_board.ZobristKey))
            return 0;

        // Mate distance pruning
        alpha = Math.Max(alpha, -MateScore + ply);
        beta = Math.Min(beta, MateScore - ply);

        // If the window has collapsed, no point searching more
        if (alpha >= beta) return alpha;

        if (depth <= 0)
            return SearchCapturesOnly(alpha, beta);

        int? ttScore = _tt.TryGetScore(_board.ZobristKey, depth, ply, alpha, beta);
        if (ttScore.HasValue)
            return ttScore.Value;

        Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];

        int moveCount = _generator.GenerateMoves(_board, ref moves);
        if (moveCount == 0)
            return _generator.IsInCheck() ? -MateScore + ply : 0;

        Move ttMove = _tt.GetBestMove(_board.ZobristKey);
        Move killer1 = ply < MaxPly ? _killers[ply, 0] : default;
        Move killer2 = ply < MaxPly ? _killers[ply, 1] : default;

        OrderMoves(moves[..moveCount], _board, ttMove, killer1, killer2);

        bool inCheck = _generator.IsInCheck(); // Used for extension, and null move pruning

        // Null move pruning
        if (!inCheck && depth >= 3 && ply > 0 && HasNonPawnMaterial())
        {
            int reduction = 3;

            _board.MakeNullMove();
            _repetitionTable.Push(_board.ZobristKey);

            int nullScore = -Search(depth - 1 - reduction, ply + 1, -beta, -beta + 1);

            _repetitionTable.TryPop();
            _board.UnmakeNullMove();

            if (_abortSearch) return 0;

            // Current pos is so good opponent cant recover even with free move
            if (nullScore >= beta)
                return beta;
        }

        int originalAlpha = alpha;
        Move bestMove = default;
        for (int i = 0; i < moveCount; i++)
        {
            var move = moves[i];
            _board.MakeMove(move);
            _repetitionTable.Push(_board.ZobristKey);

            int extension = inCheck ? 1 : 0;

            int score;
            if (i == 0)
                score = -Search(depth + extension - 1, ply + 1, -beta, -alpha);
            else
            {
                // Null window scout
                score = -Search(depth + extension - 1, ply + 1, -alpha - 1, -alpha);
                // Re-search with full window if it beat alpha unexpectedly
                if (score > alpha && score < beta)
                    score = -Search(depth + extension - 1, ply + 1, -beta, -alpha);
            }

            _repetitionTable.TryPop();
            _board.UnmakeMove(move);
            if (score >= beta)
            {
                bool isCapture = _board.GetPieceAt(moves[i].To) != null;
                if (!isCapture && ply < MaxPly)
                {
                    _killers[ply, 1] = _killers[ply, 0];
                    _killers[ply, 0] = moves[i];
                }

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

    private bool HasNonPawnMaterial()
    {
        int color = _board.ToMove;
        return _board.Bitboards[color, (int)Piece.Knight] != 0 ||
               _board.Bitboards[color, (int)Piece.Bishop] != 0 ||
               _board.Bitboards[color, (int)Piece.Rook] != 0 ||
               _board.Bitboards[color, (int)Piece.Queen] != 0;
    }

    private bool IsMate(int score) => Math.Abs(score) >= MateScore - 500;

    private int MovesToMate(int score) => (MateScore - Math.Abs(score) + 1) / 2;
}
