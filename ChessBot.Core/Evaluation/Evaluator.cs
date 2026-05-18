using System.Numerics;
using ChessBot.Core.Core;
using ChessBot.Core.Evaluation.PieceSquareTables;

namespace ChessBot.Core.Evaluation;

public class Evaluator
{
    // TODO: add a method to get any piece value 

    public static int Evaluate(Board board)
    {
        int score = 0;
        score += EvaluateSide(board, Color.White, CountPieces(board, Color.Black));
        score -= EvaluateSide(board, Color.Black, CountPieces(board, Color.White));

        return board.ToMove == (int)Color.White ? score : -score;
    }

    private static int EvaluateSide(Board board, Color color, int enemyPieces)
    {
        int score = 0;
        bool isBlack = color == Color.Black;

        ulong pawns = board.Bitboards[(int)color, (int)Piece.Pawn];
        while (pawns != 0)
        {
            int square = BitOperations.TrailingZeroCount(pawns);
            int lookupSquare = isBlack ? MirrorSquare(square) : square;
            score += PawnPieceSquare.GetValue(lookupSquare, enemyPieces);
            pawns &= pawns - 1;
        }

        ulong knights = board.Bitboards[(int)color, (int)Piece.Knight];
        while (knights != 0)
        {
            int square = BitOperations.TrailingZeroCount(knights);
            int lookupSquare = isBlack ? MirrorSquare(square) : square;
            score += KnightPieceSquare.GetValue(lookupSquare, enemyPieces);
            knights &= knights - 1;
        }

        ulong bishops = board.Bitboards[(int)color, (int)Piece.Bishop];
        while (bishops != 0)
        {
            int square = BitOperations.TrailingZeroCount(bishops);
            int lookupSquare = isBlack ? MirrorSquare(square) : square;
            score += BishopPieceSquare.GetValue(lookupSquare, enemyPieces);
            bishops &= bishops - 1;
        }

        ulong rooks = board.Bitboards[(int)color, (int)Piece.Rook];
        while (rooks != 0)
        {
            int square = BitOperations.TrailingZeroCount(rooks);
            int lookupSquare = isBlack ? MirrorSquare(square) : square;
            score += RookPieceSquare.GetValue(lookupSquare, enemyPieces);
            rooks &= rooks - 1;
        }

        ulong queens = board.Bitboards[(int)color, (int)Piece.Queen];
        while (queens != 0)
        {
            int square = BitOperations.TrailingZeroCount(queens);
            int lookupSquare = isBlack ? MirrorSquare(square) : square;
            score += QueenPieceSquare.GetValue(lookupSquare, enemyPieces);
            queens &= queens - 1;
        }

        return score;
    }

    private static int CountPieces(Board board, Color color)
    {
        ulong occupancy = 0;
        for (int piece = 0; piece < 5; piece++)
            occupancy |= board.Bitboards[(int)color, piece];
        return BitOperations.PopCount(occupancy);
    }

    private static int MirrorSquare(int square) => (7 - square / 8) * 8 + square % 8;
}