using System.Numerics;
using ChessBot.Core.Core;
using ChessBot.Core.Tables;
using ChessBot.Core.Utils;

namespace ChessBot.Core.MoveGen;

public static class MoveGenerator
{
    public static List<Move> GenerateMove(Board board)
    {
        return GeneratePseudoLegalMoves(board);
    }

    private static List<Move> GeneratePseudoLegalMoves(Board board)
    {
        List<Move> moves = new();

        // King moves
        // TODO: castling
        GeneratePseudoLegalMoves(board, moves, AttackTables.KingAttacks, Piece.King);

        // Knight moves
        GeneratePseudoLegalMoves(board, moves, AttackTables.KnightAttacks, Piece.Knight);

        // Pawn moves
        GeneratePawnMoves(board, moves);

        return moves;
    }

    private static void GeneratePseudoLegalMoves(Board board, List<Move> moves, ulong[] attackTable, Piece piece)
    {
        ulong bitboard = board.Bitboards[(int)board.ToMove, (int)piece];

        ulong friendly = board.ToMove == Color.White
            ? board.WhitePieces
            : board.BlackPieces;

        while (bitboard != 0)
        {
            int from = BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;

            ulong attacks = attackTable[from];

            attacks &= ~friendly;

            while (attacks != 0)
            {
                int to = BitOperations.TrailingZeroCount(attacks);
                attacks &= attacks - 1;

                moves.Add(new Move(from, to));
            }
        }
    }

    private static void GeneratePawnMoves(Board board, List<Move> moves)
    {
        ulong bitboard = board.Bitboards[(int)board.ToMove, (int)Piece.Pawn];

        ulong friendly = board.ToMove == Color.White
            ? board.WhitePieces
            : board.BlackPieces;
        
        bool isWhite = board.ToMove == Color.White;

        ulong promoRank = isWhite ? Masks.Rank8 : Masks.Rank1;
        ulong startRank = isWhite ? Masks.Rank2 : Masks.Rank7;

        while (bitboard != 0)
        {
            int from = BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;
            ulong pawn = 1UL << from;

            ulong singlePush = isWhite ? (pawn << 8) & board.Empty : (pawn >> 8) & board.Empty;
            ulong doublePush = (pawn & startRank) != 0
                ? isWhite ? (singlePush << 8) & board.Empty : (singlePush >> 8) & board.Empty 
                : 0UL;

            // TODO: En passant
            ulong captures = AttackTables.PawnAttacks[(int)board.ToMove, from] & ~friendly;

            ulong targets = singlePush | doublePush | captures;

            while (targets != 0)
            {
                int to = BitOperations.TrailingZeroCount(targets);
                targets &= targets - 1;
                ulong toBit = 1UL << to;

                if ((toBit & promoRank) != 0)
                {
                    moves.Add(new Move(from, to, Piece.Queen));
                    moves.Add(new Move(from, to, Piece.Rook));
                    moves.Add(new Move(from, to, Piece.Bishop));
                    moves.Add(new Move(from, to, Piece.Knight));
                }
                else 
                    moves.Add(new Move(from, to));
            }
        }
    }
}