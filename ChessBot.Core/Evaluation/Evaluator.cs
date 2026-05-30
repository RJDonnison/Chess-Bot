using System.Numerics;
using ChessBot.Core.Core;
using ChessBot.Core.Evaluation.PieceSquareTables;
using ChessBot.Core.Search;
using ChessBot.Core.Utilities;

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

        if (gamePhase > 18 && interpolatedScore > 300)
        {
            int friendlyKingSq = BitOperations.TrailingZeroCount(board.Bitboards[board.ToMove, (int)Piece.King]);
            int enemyKingSq = BitOperations.TrailingZeroCount(board.Bitboards[board.ToMove ^ 1, (int)Piece.King]);
        
            interpolatedScore += ForceKingToCorner(friendlyKingSq, enemyKingSq, gamePhase);
        }

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
        int phase = 0;

        for (int color = 0; color < 2; color++)
        {
            phase += BitOperations.PopCount(board.Bitboards[color, (int)Piece.Knight]);
            phase += BitOperations.PopCount(board.Bitboards[color, (int)Piece.Bishop]);
            phase += BitOperations.PopCount(board.Bitboards[color, (int)Piece.Rook]) * 2;
            phase += BitOperations.PopCount(board.Bitboards[color, (int)Piece.Queen]) * 4;
        }

        return Math.Clamp(phase, 0, 24);
    }

    private static int MirrorSquare(int square) => (7 - square / 8) * 8 + square % 8;

    private static int ForceKingToCorner(int kingSq, int enemyKingSq, int gamePhase)
    {
        int eval = 0;

        int enemyKingRank = BoardHelper.Rank(enemyKingSq);
        int enemyKingFile = BoardHelper.File(enemyKingSq);

        int enemyKingDstToCenterFile = int.Max(3 - enemyKingFile, enemyKingFile - 4);
        int enemyKingDstToCenterRank = int.Max(3 - enemyKingRank, enemyKingRank - 4);
        int enemyKingDstFromCenter = enemyKingDstToCenterRank + enemyKingDstToCenterFile;
        eval += enemyKingDstFromCenter * 4;

        int friendlyKingRank = BoardHelper.Rank(kingSq);
        int friendlyKingFile = BoardHelper.File(kingSq);

        int dstBetweenKingFiles = int.Abs(friendlyKingFile - enemyKingFile);
        int dstBetweenKingRanks = int.Abs(friendlyKingRank - enemyKingRank);
        int dstBetweenKings = dstBetweenKingFiles + dstBetweenKingRanks;
        eval += (14 - dstBetweenKings) * 2;

        return eval * gamePhase / 24;
    }

    public static int GetPositionalValue(Piece piece, Color color, int square)
    {
        int lookupSquare = color == Color.Black ? MirrorSquare(square) : square;
        // TODO: remove call to MoveOrderer
        return MgGetters[(int)piece](lookupSquare) - MoveOrderer.PieceValues[(int)piece];
    }
}