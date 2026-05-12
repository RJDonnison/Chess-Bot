using ChessBot.Core.Core;
using ChessBot.Core.Evaluation;
using ChessBot.Core.MoveGen;

namespace ChessBot.Core.Search;

using static MoveOrderer;

public class Searcher
{
    private const int Depth = 5;
    private const int Infinity = 30000;
    private const int MateScore = 29000;
    
    private readonly Evaluator _evaluator = new();
    private readonly MoveGenerator _generator = new();
    private readonly RepetitionTable _repetitionTable = new();

    public Move GetBestMove(Board board) => Search(board, Depth);

    private Move Search(Board board, int depth)
    {
        Move bestMove = default;
        int alpha = -Infinity;

        Span<Move> moves = _generator.GenerateMoves(board);
        Span<int> scores = stackalloc int[MoveGenerator.MaxMoves];
        
        OrderMoves(moves, scores, board);
        for (int i = 0; i < moves.Length; i++)
        {
            var move = PickMove(moves, scores, i);
            
            board.MakeMove(move);
            _repetitionTable.Push(board.ZobristKey);

            int score = -Search(board, depth - 1, 1, -Infinity, -alpha);

            _repetitionTable.TryPop();
            board.UnmakeMove(move);

            if (score > alpha)
            {
                alpha = score;
                bestMove = move;
            }
        } 
        
        return bestMove;
    }

    private int Search(Board board, int depth, int ply, int alpha, int beta)
    {
        if (board.Drawn || _repetitionTable.Contains(board.ZobristKey))
            return 0;

        if (depth == 0)
            return _evaluator.Evaluate(board);

        Span<Move> moves = stackalloc Move[MoveGenerator.MaxMoves];
        Span<int> scores = stackalloc int[MoveGenerator.MaxMoves];
        
        int moveCount = _generator.GenerateMoves(board, ref moves);
        if (moveCount == 0)
            return _generator.IsInCheck() ? -MateScore + ply : 0;
        
        OrderMoves(moves, scores, board);
        for (int i = 0; i < moveCount; i++)
        {
            var move = PickMove(moves, scores, i);
            board.MakeMove(move);
            _repetitionTable.Push(board.ZobristKey);

            int score = -Search(board, depth - 1, ply + 1, -beta, -alpha);
            
            _repetitionTable.TryPop();
            board.UnmakeMove(move);
            if (score >= beta)
                return beta;
            alpha = int.Max(alpha, score);
        }

        return alpha;
    }
}