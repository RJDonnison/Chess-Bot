using System.Numerics;
using ChessBot.Core.Core;
using ChessBot.Core.Tables;
using ChessBot.Core.Utils;

namespace ChessBot.Core.MoveGen;

public static class MoveGenerator
{
    public static List<Move> GenerateMove(Board board)
    {
        ulong enemyAttacks = AttackMap(board, (Color)((int)board.ToMove ^ 1));
        int kingSq = BitOperations.TrailingZeroCount(board.Bitboards[(int)board.ToMove, (int)Piece.King]);
        bool inCheck = (enemyAttacks & (1UL << kingSq)) != 0;

        ulong[] pinMasks = ComputePinMasks(board, kingSq);
        ulong checkMask = inCheck
            ? ComputeCheckMask(board, kingSq)
            : 0xFFFFFFFFFFFFFFFFUL;

        if (inCheck)
            BitboardVisualizer.Bitboard = checkMask;

        List<Move> moves = new();

        GenerateKingMoves(board, moves, enemyAttacks);
        if (checkMask == 0) return moves;

        GenerateMoves(board, moves, Piece.Knight, checkMask, pinMasks);
        GenerateMoves(board, moves, Piece.Rook, checkMask, pinMasks);
        GenerateMoves(board, moves, Piece.Bishop, checkMask, pinMasks);
        GenerateMoves(board, moves, Piece.Queen, checkMask, pinMasks);
        GeneratePawnMoves(board, moves, checkMask, pinMasks);

        return moves;
    }

    private static ulong ComputeCheckMask(Board board, int kingSq)
    {
        ulong mask = 0UL;
        int checkerCount = 0;

        ulong knightCheckers = KnightAttacks.Table[kingSq] & board.Bitboards[(int)board.ToMove ^ 1, (int)Piece.Knight];
        if (knightCheckers != 0) { mask |= knightCheckers; checkerCount++; }

        ulong pawnCheckers = PawnAttacks.Table[(int)board.ToMove, kingSq] &
                             board.Bitboards[(int)board.ToMove ^ 1, (int)Piece.Pawn];
        if (pawnCheckers != 0) { mask |= pawnCheckers; checkerCount++; }

        ulong rookCheckers = MagicBitboards.GetRookMoves(kingSq, board.Occupied) &
                             (board.Bitboards[(int)board.ToMove ^ 1, (int)Piece.Rook]
                                | board.Bitboards[(int)board.ToMove ^ 1, (int)Piece.Queen]);
        while (rookCheckers != 0)
        {
            int checkerSq = Bits.LSB(ref rookCheckers);
            mask |= Masks.Between[kingSq, checkerSq] | (1UL << checkerSq);
            checkerCount++;
        }

        ulong bishopCheckers = MagicBitboards.GetBishopMoves(kingSq, board.Occupied) & (board.Bitboards[(int)board.ToMove ^ 1, (int)Piece.Bishop] | board.Bitboards[(int)board.ToMove ^ 1, (int)Piece.Queen]);
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

        int them = (int)board.ToMove ^ 1;

        // Straight pins
        ulong rookPinners = XRayRookAttacks(kingSq, board.Occupied, board.FriendlyPieces)
                            & (board.Bitboards[them, (int)Piece.Rook]
                               | board.Bitboards[them, (int)Piece.Queen]);

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
                              & (board.Bitboards[them, (int)Piece.Bishop]
                                 | board.Bitboards[them, (int)Piece.Queen]);

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
        ulong blockers = attacks & friendly;
        return MagicBitboards.GetRookMoves(sq, occupied ^ blockers) ^ attacks;
    }

    private static ulong XRayBishopAttacks(int sq, ulong occupied, ulong friendly)
    {
        ulong attacks = MagicBitboards.GetBishopMoves(sq, occupied);
        ulong blockers = attacks & friendly;
        return MagicBitboards.GetBishopMoves(sq, occupied ^ blockers) ^ attacks;
    }

    private static void GenerateMoves(Board board, List<Move> moves, Piece piece, ulong checkMask, ulong[] pinMasks)
    {
        ulong bitboard = board.Bitboards[(int)board.ToMove, (int)piece];

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
            board.Bitboards[(int)board.ToMove, (int)Piece.King]);
        ulong targets = KingAttacks.Table[from]
                        & ~board.FriendlyPieces
                        & ~enemyAttacks;

        while (targets != 0)
        {
            int to = Bits.LSB(ref targets);
            moves.Add(new Move(from, to));
        }
    }

    private static void GeneratePawnMoves(Board board, List<Move> moves, ulong checkMask, ulong[] pinMasks)
    {
        ulong bitboard = board.Bitboards[(int)board.ToMove, (int)Piece.Pawn];

        bool isWhite = board.ToMove == Color.White;
        ulong startRank = isWhite ? Masks.Rank2 : Masks.Rank7;
        ulong promoRank = board.ToMove == Color.White ? Masks.Rank8 : Masks.Rank1;

        while (bitboard != 0)
        {
            int from = BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;
            ulong pawn = 1UL << from;

            ulong singlePush = isWhite ? (pawn << 8) & board.Empty : (pawn >> 8) & board.Empty;
            ulong doublePush = (pawn & startRank) != 0
                ? isWhite ? ((singlePush & checkMask) << 8) & board.Empty : ((singlePush & checkMask) >> 8) & board.Empty
                : 0UL;

            ulong captures = PawnAttacks.Table[(int)board.ToMove, from] & board.EnemyPieces;
            // TODO: En passant
            ulong targets = singlePush | doublePush | captures;
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

    private static ulong AttackMap(Board board, Color color)
    {
        int cIdx = (int)color;
        ulong attacks = 0;

        // None sliding 
        ulong pawns = board.Bitboards[cIdx, (int)Piece.Pawn];
        ulong knights = board.Bitboards[cIdx, (int)Piece.Knight];
        ulong kings = board.Bitboards[cIdx, (int)Piece.King];

        while (pawns != 0) { int sq = Bits.LSB(ref pawns); attacks |= PawnAttacks.Table[cIdx, sq]; }
        while (knights != 0) { int sq = Bits.LSB(ref knights); attacks |= KnightAttacks.Table[sq]; }
        while (kings != 0) { int sq = Bits.LSB(ref kings); attacks |= KingAttacks.Table[sq]; }

        // Sliding attacks
        ulong defenderKing = board.Bitboards[cIdx ^ 1, (int)Piece.King];
        ulong occNoKing = board.Occupied & ~defenderKing;
        ulong rooks = board.Bitboards[cIdx, (int)Piece.Rook] | board.Bitboards[cIdx, (int)Piece.Queen];
        ulong bishops = board.Bitboards[cIdx, (int)Piece.Bishop] | board.Bitboards[cIdx, (int)Piece.Queen];

        while (rooks != 0) { int sq = Bits.LSB(ref rooks); attacks |= MagicBitboards.GetRookMoves(sq, occNoKing); }
        while (bishops != 0) { int sq = Bits.LSB(ref bishops); attacks |= MagicBitboards.GetBishopMoves(sq, occNoKing); }

        return attacks;
    }
}