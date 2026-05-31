using System.Numerics;
using ChessBot.Core.Core;
using ChessBot.Core.Evaluation.PieceSquareTables;
using ChessBot.Core.MoveGen;
using ChessBot.Core.MoveGen.Magic;
using ChessBot.Core.Search;
using ChessBot.Core.Utilities;

namespace ChessBot.Core.Evaluation;

public class Evaluator
{
    // Bonuses by rank for passed pawns
    private static readonly int[] PassedPawnBonus = { 0, 15, 15, 25, 40, 60, 90, 0 };

    // Penalties
    private const int DoubledPawnPenalty = -10;
    private const int IsolatedPawnPenalty = -8;

    private const int MobilityBonus = 2;

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

        mgScore += EvaluateSideMg(board, Color.White);
        egScore += EvaluateSideEg(board, Color.White);
        mgScore -= EvaluateSideMg(board, Color.Black);
        egScore -= EvaluateSideEg(board, Color.Black);

        // Interpolate between middlegame and endgame scores
        int interpolatedScore = (mgScore * (24 - gamePhase) + egScore * gamePhase) / 24;

        int pawnScore = 0;
        pawnScore += EvaluatePawnStructure(board, Color.White);
        pawnScore -= EvaluatePawnStructure(board, Color.Black);
        interpolatedScore += pawnScore;

        int mobilityScore = EvaluateMobility(board);
        interpolatedScore += mobilityScore;

        if (gamePhase > 18 && interpolatedScore > 300)
        {
            int friendlyKingSq = BitOperations.TrailingZeroCount(board.Bitboards[board.ToMove, (int)Piece.King]);
            int enemyKingSq = BitOperations.TrailingZeroCount(board.Bitboards[board.ToMove ^ 1, (int)Piece.King]);

            interpolatedScore += ForceKingToCorner(friendlyKingSq, enemyKingSq, gamePhase);
        }

        return board.ToMove == (int)Color.White ? interpolatedScore : -interpolatedScore;
    }

    private static int EvaluatePawnStructure(Board board, Color color)
    {
        int bonus = 0;
        ulong enemyPawns = board.Bitboards[(int)color ^ 1, (int)Piece.Pawn];
        ulong pawns = board.Bitboards[(int)color, (int)Piece.Pawn];

        while (pawns != 0)
        {
            int sq = BitOperations.TrailingZeroCount(pawns);
            int file = sq % 8;
            int rank = sq / 8;

            // Passed pawn 
            ulong passedMask = Masks.PassedPawnMask[(int)color, sq];
            if ((enemyPawns & passedMask) == 0)
            {
                int advancedRank = color == Color.White ? rank : 7 - rank;
                bonus += PassedPawnBonus[advancedRank];
            }

            // Doubled pawn 
            ulong fileMask = Masks.FileMask[file];
            if (BitOperations.PopCount(pawns & fileMask) > 1)
                bonus += DoubledPawnPenalty / 2; // Penalty applied to both pawns

            // Isolated pawn 
            if ((pawns & Masks.IsolatedPawnMask[file]) == 0)
                bonus += IsolatedPawnPenalty;

            pawns &= pawns - 1;
        }

        return bonus;
    }

    private static int EvaluateMobility(Board board)
    {
        // Count pseudo-legal moves 
        int whiteMobility = CountMobility(board, Color.White);
        int blackMobility = CountMobility(board, Color.Black);

        return (whiteMobility - blackMobility) * MobilityBonus;
    }

    private static int CountMobility(Board board, Color color)
    {
        int mobility = 0;

        for (int piece = 0; piece < 6; piece++)
        {
            ulong bitboard = board.Bitboards[(int)color, piece];

            while (bitboard != 0)
            {
                int from = BitOperations.TrailingZeroCount(bitboard);
                bitboard &= bitboard - 1;

                ulong targets = (Piece)piece switch
                {
                    Piece.Knight => KnightAttacks.Table[from],
                    Piece.Rook => MagicBitboards.GetRookMoves(from, board.Occupied),
                    Piece.Bishop => MagicBitboards.GetBishopMoves(from, board.Occupied),
                    Piece.Queen => MagicBitboards.GetQueenMoves(from, board.Occupied),
                    _ => 0UL // Pawns and king excluded
                };
                mobility += BitOperations.PopCount(targets & ~board.FriendlyPieces);
            }
        }

        return mobility;
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