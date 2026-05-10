using System.Numerics;
using ChessBot.Core.Core;
using ChessBot.Core.Tables;
using ChessBot.Core.Utilities;

namespace ChessBot.Core.MoveGen;

public static class MoveGenerator
{
    public static List<Move> GenerateMoves(Board board)
    {
        ulong enemyAttacks = AttackMap(board, board.ToMove ^ 1);
        int kingSq = BitOperations.TrailingZeroCount(board.Bitboards[board.ToMove, (int)Piece.King]);
        bool inCheck = (enemyAttacks & (1UL << kingSq)) != 0;

        ulong[] pinMasks = ComputePinMasks(board, kingSq);
        ulong checkMask = inCheck
            ? ComputeCheckMask(board, kingSq)
            : 0xFFFFFFFFFFFFFFFFUL;

        if (inCheck)
            BitboardVisualizer.Bitboard = checkMask;

        List<Move> moves = new();

        GenerateKingMoves(board, moves, enemyAttacks);
        // Return early as double checked so only king can move
        if (checkMask == 0) return moves;

        GeneratePseudoLegalMoves(board, moves, Piece.Knight, checkMask, pinMasks);
        GeneratePseudoLegalMoves(board, moves, Piece.Rook, checkMask, pinMasks);
        GeneratePseudoLegalMoves(board, moves, Piece.Bishop, checkMask, pinMasks);
        GeneratePseudoLegalMoves(board, moves, Piece.Queen, checkMask, pinMasks);
        GeneratePawnMoves(board, moves, checkMask, pinMasks);

        return moves;
    }

    private static ulong ComputeCheckMask(Board board, int kingSq)
    {
        ulong mask = 0UL;
        int checkerCount = 0;

        ulong knightCheckers = KnightAttacks.Table[kingSq] & board.Bitboards[board.ToMove ^ 1, (int)Piece.Knight];
        if (knightCheckers != 0) { mask |= knightCheckers; checkerCount++; }

        ulong pawnCheckers = PawnAttacks.Table[board.ToMove, kingSq] &
                             board.Bitboards[board.ToMove ^ 1, (int)Piece.Pawn];
        if (pawnCheckers != 0) { mask |= pawnCheckers; checkerCount++; }

        ulong rookCheckers = MagicBitboards.GetRookMoves(kingSq, board.Occupied) &
                             (board.Bitboards[board.ToMove ^ 1, (int)Piece.Rook]
                                | board.Bitboards[board.ToMove ^ 1, (int)Piece.Queen]);
        while (rookCheckers != 0)
        {
            int checkerSq = Bits.LSB(ref rookCheckers);
            mask |= Masks.Between[kingSq, checkerSq] | (1UL << checkerSq);
            checkerCount++;
        }

        ulong bishopCheckers = MagicBitboards.GetBishopMoves(kingSq, board.Occupied) & (board.Bitboards[board.ToMove ^ 1, (int)Piece.Bishop] | board.Bitboards[board.ToMove ^ 1, (int)Piece.Queen]);
        while (bishopCheckers != 0)
        {
            int checkerSq = Bits.LSB(ref bishopCheckers);
            mask |= Masks.Between[kingSq, checkerSq] | (1UL << checkerSq);
            checkerCount++;
        }

        return checkerCount >= 2 ? 0UL : mask;
    }

    private static ulong[] ComputePinMasks(Board board, int kingSq)
    {
        ulong[] pinMasks = new ulong[64];
        Array.Fill(pinMasks, 0xFFFFFFFFFFFFFFFFUL);

        // Straight pins
        ulong rookPinners = XRayRookAttacks(kingSq, board.Occupied, board.FriendlyPieces)
                            & (board.Bitboards[board.ToMove ^ 1, (int)Piece.Rook]
                               | board.Bitboards[board.ToMove ^ 1, (int)Piece.Queen]);

        while (rookPinners != 0)
        {
            int pinnerSq = Bits.LSB(ref rookPinners);
            ulong ray = Masks.Between[kingSq, pinnerSq] | (1UL << pinnerSq);
            ulong pinned = ray & board.FriendlyPieces;

            if (BitOperations.PopCount(pinned) == 1)
                pinMasks[BitOperations.TrailingZeroCount(pinned)] = ray;
        }

        // Diagonal pins
        ulong bishopPinners = XRayBishopAttacks(kingSq, board.Occupied, board.FriendlyPieces)
                              & (board.Bitboards[board.ToMove ^ 1, (int)Piece.Bishop]
                                 | board.Bitboards[board.ToMove ^ 1, (int)Piece.Queen]);

        while (bishopPinners != 0)
        {
            int pinnerSq = Bits.LSB(ref bishopPinners);
            ulong ray = Masks.Between[kingSq, pinnerSq] | (1UL << pinnerSq);
            ulong pinned = ray & board.FriendlyPieces;

            if (BitOperations.PopCount(pinned) == 1)
                pinMasks[BitOperations.TrailingZeroCount(pinned)] = ray;
        }

        return pinMasks;
    }

    private static ulong XRayRookAttacks(int sq, ulong occupied, ulong friendly)
    {
        ulong attacks = MagicBitboards.GetRookMoves(sq, occupied);
        ulong occMasked = (occupied ^ (attacks & friendly)) & MagicBitboards.RookMasks[sq];
        return MagicBitboards.GetRookMoves(sq, occMasked) ^ attacks;
    }

    private static ulong XRayBishopAttacks(int sq, ulong occupied, ulong friendly)
    {
        ulong attacks = MagicBitboards.GetBishopMoves(sq, occupied);
        ulong occMasked = (occupied ^ (attacks & friendly)) & MagicBitboards.BishopMasks[sq];
        return MagicBitboards.GetBishopMoves(sq, occMasked) ^ attacks;
    }

    private static void GeneratePseudoLegalMoves(Board board, List<Move> moves, Piece piece, ulong checkMask, ulong[] pinMasks)
    {
        ulong bitboard = board.Bitboards[board.ToMove, (int)piece];

        while (bitboard != 0)
        {
            int from = BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;

            ulong targets = piece switch
            {
                Piece.Knight => KnightAttacks.Table[from],
                Piece.Rook => MagicBitboards.GetRookMoves(from, board.Occupied),
                Piece.Bishop => MagicBitboards.GetBishopMoves(from, board.Occupied),
                Piece.Queen => MagicBitboards.GetQueenMoves(from, board.Occupied),
                _ => 0UL
            };
            targets &= ~board.FriendlyPieces & checkMask & pinMasks[from];

            while (targets != 0)
            {
                int to = BitOperations.TrailingZeroCount(targets);
                targets &= targets - 1;

                moves.Add(new Move(from, to));
            }
        }
    }

    private static void GenerateKingMoves(Board board, List<Move> moves, ulong enemyAttacks)
    {
        int from = BitOperations.TrailingZeroCount(
            board.Bitboards[board.ToMove, (int)Piece.King]);
        ulong targets = KingAttacks.Table[from]
                        & ~board.FriendlyPieces
                        & ~enemyAttacks;

        while (targets != 0)
        {
            int to = Bits.LSB(ref targets);
            moves.Add(new Move(from, to));
        }
        
        // Castling
        if (board.ToMove == (int)Color.White)
        {
            // White kingside
            if ((board.CastlingRights & 0b1000) != 0)
            {
                bool empty = (board.Occupied & Masks.Between[4,7]) == 0;
                bool safe = (enemyAttacks & Masks.Between[4,6]) == 0;
                if (empty && safe) moves.Add(new Move(4, 6));

            }

            // White queenside
            if ((board.CastlingRights & 0b0100) != 0)
            {
                bool empty = (board.Occupied & Masks.Between[4,0]) == 0;
                bool safe = (enemyAttacks & Masks.Between[4,2]) == 0;
                if (empty && safe) moves.Add(new Move(4,2));
            }
            
            return;
        }

        // Black kingside
        if ((board.CastlingRights & 0b0010) != 0)
        {
            bool empty = (board.Occupied & Masks.Between[60,63]) == 0;
            bool safe = (enemyAttacks & Masks.Between[60,62]) == 0;
            if (empty && safe) moves.Add(new Move(60,62)); 
        }

        // Black queenside
        if ((board.CastlingRights & 0b0001) != 0)
        {
            bool empty = (board.Occupied & Masks.Between[60,56]) == 0;
            bool safe = (enemyAttacks & Masks.Between[60,58]) == 0;
            if (empty && safe) moves.Add(new Move(60,58)); 
        }
    }

    private static void GeneratePawnMoves(Board board, List<Move> moves, ulong checkMask, ulong[] pinMasks)
    {
        ulong bitboard = board.Bitboards[board.ToMove, (int)Piece.Pawn];

        bool isWhite = board.ToMove == (int)Color.White;
        ulong startRank = isWhite ? Masks.Rank2 : Masks.Rank7;
        ulong promoRank = isWhite ? Masks.Rank8 : Masks.Rank1;

        while (bitboard != 0)
        {
            int from = BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;
            ulong pawn = 1UL << from;

            ulong singlePush = isWhite ? (pawn << 8) & board.Empty : (pawn >> 8) & board.Empty;
            ulong doublePush = (pawn & startRank) != 0
                ? isWhite ? ((singlePush & checkMask) << 8) & board.Empty : ((singlePush & checkMask) >> 8) & board.Empty
                : 0UL;

            ulong captures = PawnAttacks.Table[board.ToMove, from] & board.EnemyPieces;
            ulong targets = singlePush | doublePush | captures;
            // EnPassant
            if (board.EnPassantSquare != null)
                targets |= captures & (1UL << (int)board.EnPassantSquare);
            
            targets &= checkMask & pinMasks[from];

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

    private static ulong AttackMap(Board board, int color)
    {
        ulong attacks = 0;

        // None sliding 
        ulong pawns = board.Bitboards[color, (int)Piece.Pawn];
        ulong knights = board.Bitboards[color, (int)Piece.Knight];
        ulong kings = board.Bitboards[color, (int)Piece.King];

        while (pawns != 0) { int sq = Bits.LSB(ref pawns); attacks |= PawnAttacks.Table[color, sq]; }
        while (knights != 0) { int sq = Bits.LSB(ref knights); attacks |= KnightAttacks.Table[sq]; }
        while (kings != 0) { int sq = Bits.LSB(ref kings); attacks |= KingAttacks.Table[sq]; }

        // Sliding attacks
        ulong defenderKing = board.Bitboards[color ^ 1, (int)Piece.King];
        ulong occNoKing = board.Occupied & ~defenderKing;
        ulong rooks = board.Bitboards[color, (int)Piece.Rook] | board.Bitboards[color, (int)Piece.Queen];
        ulong bishops = board.Bitboards[color, (int)Piece.Bishop] | board.Bitboards[color, (int)Piece.Queen];

        while (rooks != 0) { int sq = Bits.LSB(ref rooks); attacks |= MagicBitboards.GetRookMoves(sq, occNoKing); }
        while (bishops != 0) { int sq = Bits.LSB(ref bishops); attacks |= MagicBitboards.GetBishopMoves(sq, occNoKing); }

        return attacks;
    }
}