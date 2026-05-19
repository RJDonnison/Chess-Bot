using System.Numerics;
using ChessBot.Core.Core;
using ChessBot.Core.Evaluation.PieceSquareTables;
using ChessBot.Core.Search;

namespace ChessBot.Core.Evaluation;

public class Evaluator
{
    // Cache for PST getter methods to avoid reflection and enable dispatch
    private static readonly PstGetter[] MgGetters = new PstGetter[]
    {
        PawnPieceSquare.GetMgValue,
        KnightPieceSquare.GetMgValue,
        BishopPieceSquare.GetMgValue,
        RookPieceSquare.GetMgValue,
        QueenPieceSquare.GetMgValue,
        KingPieceSquare.GetMgValue,
    };

    private static readonly PstGetter[] EgGetters = new PstGetter[]
    {
        PawnPieceSquare.GetEgValue,
        KnightPieceSquare.GetEgValue,
        BishopPieceSquare.GetEgValue,
        RookPieceSquare.GetEgValue,
        QueenPieceSquare.GetEgValue,
        KingPieceSquare.GetEgValue,
    };

    private delegate int PstGetter(int square);

    public static int Evaluate(Board board)
    {
        int gamePhase = CalculateGamePhase(board);
        
        int mgScore = 0;
        int egScore = 0;

        // Evaluate white pieces
        mgScore += EvaluateSideMg(board, Color.White);
        egScore += EvaluateSideEg(board, Color.White);
        
        // Evaluate black pieces (subtract from score)
        mgScore -= EvaluateSideMg(board, Color.Black);
        egScore -= EvaluateSideEg(board, Color.Black);

        // Interpolate between middlegame and endgame scores
        int interpolatedScore = (mgScore * (24 - gamePhase) + egScore * gamePhase) / 24;

        return board.ToMove == (int)Color.White ? interpolatedScore : -interpolatedScore;
    }

    private static int EvaluateSideMg(Board board, Color color)
    {
        int score = 0;
        bool isBlack = color == Color.Black;

        for (int piece = 0; piece <= (int)Piece.King; piece++)
        {
            ulong pieces = board.Bitboards[(int)color, piece];
            while (pieces != 0)
            {
                int square = BitOperations.TrailingZeroCount(pieces);
                int lookupSquare = isBlack ? MirrorSquare(square) : square;
                score += MgGetters[piece](lookupSquare);
                pieces &= pieces - 1;
            }
        }

        return score;
    }

    private static int EvaluateSideEg(Board board, Color color)
    {
        int score = 0;
        bool isBlack = color == Color.Black;

        for (int piece = 0; piece <= (int)Piece.King; piece++)
        {
            ulong pieces = board.Bitboards[(int)color, piece];
            while (pieces != 0)
            {
                int square = BitOperations.TrailingZeroCount(pieces);
                int lookupSquare = isBlack ? MirrorSquare(square) : square;
                score += EgGetters[piece](lookupSquare);
                pieces &= pieces - 1;
            }
        }

        return score;
    }

    private static int CalculateGamePhase(Board board)
    {
        // 0 = opening, 24 = endgame
        int totalPhase = 0;
        
        for (int color = 0; color < 2; color++)
        {
            totalPhase += BitOperations.PopCount(board.Bitboards[color, (int)Piece.Pawn]) * 1;      // Pawn
            totalPhase += BitOperations.PopCount(board.Bitboards[color, (int)Piece.Knight]) * 3;    // Knight
            totalPhase += BitOperations.PopCount(board.Bitboards[color, (int)Piece.Bishop]) * 3;    // Bishop
            totalPhase += BitOperations.PopCount(board.Bitboards[color, (int)Piece.Rook]) * 5;      // Rook
            totalPhase += BitOperations.PopCount(board.Bitboards[color, (int)Piece.Queen]) * 9;     // Queen
        }

        // Clamp to 0-24 range (24 * total_phase_pieces / total_opening_phase_pieces)
        return totalPhase > 0 ? Math.Min(24, (24 * totalPhase) / 96) : 24;
    }

    private static int MirrorSquare(int square) => (7 - square / 8) * 8 + square % 8;

    public static int GetPositionalValue(Piece piece, Color color, int square)
    {
        int lookupSquare = color == Color.Black ? MirrorSquare(square) : square;
        // TODO: remove call to MoveOrderer
        return MgGetters[(int)piece](lookupSquare) - MoveOrderer.PieceValues[(int)piece];
    }
}