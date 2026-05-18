using ChessBot.Core.Core;
using ChessBot.Core.Evaluation;
using ChessBot.Core.MoveGen;

namespace ChessBot.Core.Search;

public static class MoveOrderer
{
    public static readonly int[] PieceValues =
    {
        100,  // Pawn   = 0
        300,  // Knight = 1
        320,  // Bishop = 2
        500,  // Rook   = 3
        900,  // Queen  = 4
        0,    // King   = 5
    };

    // Todo: work with new piece tables
    public static void OrderMoves(Span<Move> moves, Board board)
    {
        Span<int> scores = stackalloc int[moves.Length];
        
        for (int i = 0; i < moves.Length; i++)
        {
            int score = 0;
            Piece movePiece = (Piece)board.GetPieceAt(moves[i].From)!;
            Piece? captured = board.GetPieceAt(moves[i].To);

            if (captured != null)
                score += 10 * PieceValues[(int)captured] - PieceValues[(int)movePiece];

            if (moves[i].Promotion != null)
                score += PieceValues[(int)moves[i].Promotion!];

            scores[i] = score;
        }
        
        scores.Sort(moves);
        
        moves.Reverse();
    }
}