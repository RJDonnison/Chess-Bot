using System.Numerics;
using ChessBot.Core.Core;
using ChessBot.Core.MoveGen.Magic;
using ChessBot.Core.Utilities;

namespace ChessBot.Core.MoveGen;

public class MoveGenerator
{
    public const int MaxMoves = 218;

    private Board _board = null!;
    private bool _inCheck;
    private ulong _enemyAttacks;
    private ulong[] _pinMasks = null!;
    private ulong _checkMask;
    private int _kingSq;
    private int _currMoveIndex;
    private bool _capturesOnly;

    // Note, this will only return correct value after GenerateMoves() has been called in the current position
    public bool IsInCheck() => _inCheck;

    public Span<Move> GenerateMoves(Board board)
    {
        Span<Move> moves = new Move[MaxMoves];
        GenerateMoves(board, ref moves);
        return moves;
    }

    // Note, can use stackalloc Move[MaxMoves] and pass to moves for performance
    public int GenerateMoves(Board board, ref Span<Move> moves, bool capturesOnly = false)
    {
        _board = board;
        _capturesOnly = capturesOnly;
        Init();

        GenerateKingMoves(moves);
        // Skip other moves as double checked so only king can move
        if (_checkMask != 0)
        {
            GenerateLegalMoves(moves, Piece.Knight);
            GenerateLegalMoves(moves, Piece.Rook);
            GenerateLegalMoves(moves, Piece.Bishop);
            GenerateLegalMoves(moves, Piece.Queen);
            GeneratePawnMoves(moves);
        }

        moves = moves.Slice(0, _currMoveIndex);
        return moves.Length;
    }

    private void Init()
    {
        _currMoveIndex = 0;
        _enemyAttacks = AttackMap(_board, _board.ToMove ^ 1);
        _kingSq = BitOperations.TrailingZeroCount(_board.Bitboards[_board.ToMove, (int)Piece.King]);
        _inCheck = (_enemyAttacks & (1UL << _kingSq)) != 0;

        _pinMasks = ComputePinMasks();
        _checkMask = _inCheck
            ? ComputeCheckMask()
            : 0xFFFFFFFFFFFFFFFFUL;
    }


    private ulong ComputeCheckMask()
    {
        ulong mask = 0UL;
        int checkerCount = 0;

        ulong knightCheckers = KnightAttacks.Table[_kingSq] & _board.Bitboards[_board.ToMove ^ 1, (int)Piece.Knight];
        if (knightCheckers != 0) { mask |= knightCheckers; checkerCount++; }

        ulong pawnCheckers = PawnAttacks.Table[_board.ToMove, _kingSq] &
                             _board.Bitboards[_board.ToMove ^ 1, (int)Piece.Pawn];
        if (pawnCheckers != 0) { mask |= pawnCheckers; checkerCount++; }

        ulong rookCheckers = MagicBitboards.GetRookMoves(_kingSq, _board.Occupied) &
                             (_board.Bitboards[_board.ToMove ^ 1, (int)Piece.Rook]
                                | _board.Bitboards[_board.ToMove ^ 1, (int)Piece.Queen]);
        while (rookCheckers != 0)
        {
            int checkerSq = Bits.LSB(ref rookCheckers);
            mask |= Masks.Between[_kingSq, checkerSq] | (1UL << checkerSq);
            checkerCount++;
        }

        ulong bishopCheckers = MagicBitboards.GetBishopMoves(_kingSq, _board.Occupied) & (_board.Bitboards[_board.ToMove ^ 1, (int)Piece.Bishop] | _board.Bitboards[_board.ToMove ^ 1, (int)Piece.Queen]);
        while (bishopCheckers != 0)
        {
            int checkerSq = Bits.LSB(ref bishopCheckers);
            mask |= Masks.Between[_kingSq, checkerSq] | (1UL << checkerSq);
            checkerCount++;
        }

        return checkerCount >= 2 ? 0UL : mask;
    }

    private ulong[] ComputePinMasks()
    {
        ulong[] pinMasks = new ulong[64];
        Array.Fill(pinMasks, 0xFFFFFFFFFFFFFFFFUL);

        // Straight pins
        ulong rookPinners = XRayRookAttacks(_kingSq, _board.Occupied, _board.FriendlyPieces)
                            & (_board.Bitboards[_board.ToMove ^ 1, (int)Piece.Rook]
                               | _board.Bitboards[_board.ToMove ^ 1, (int)Piece.Queen]);

        while (rookPinners != 0)
        {
            int pinnerSq = Bits.LSB(ref rookPinners);
            ulong ray = Masks.Between[_kingSq, pinnerSq] | (1UL << pinnerSq);
            ulong pinned = ray & _board.FriendlyPieces;

            if (BitOperations.PopCount(pinned) == 1)
                pinMasks[BitOperations.TrailingZeroCount(pinned)] = ray;
        }

        // Diagonal pins
        ulong bishopPinners = XRayBishopAttacks(_kingSq, _board.Occupied, _board.FriendlyPieces)
                              & (_board.Bitboards[_board.ToMove ^ 1, (int)Piece.Bishop]
                                 | _board.Bitboards[_board.ToMove ^ 1, (int)Piece.Queen]);

        while (bishopPinners != 0)
        {
            int pinnerSq = Bits.LSB(ref bishopPinners);
            ulong ray = Masks.Between[_kingSq, pinnerSq] | (1UL << pinnerSq);
            ulong pinned = ray & _board.FriendlyPieces;

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

    private void GenerateLegalMoves(Span<Move> moves, Piece piece)
    {
        ulong bitboard = _board.Bitboards[_board.ToMove, (int)piece];

        while (bitboard != 0)
        {
            int from = BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;

            ulong targets = piece switch
            {
                Piece.Knight => KnightAttacks.Table[from],
                Piece.Rook => MagicBitboards.GetRookMoves(from, _board.Occupied),
                Piece.Bishop => MagicBitboards.GetBishopMoves(from, _board.Occupied),
                Piece.Queen => MagicBitboards.GetQueenMoves(from, _board.Occupied),
                _ => 0UL
            };
            targets &= ~_board.FriendlyPieces & _checkMask & _pinMasks[from];
            if (_capturesOnly) targets &= _board.EnemyPieces;

            while (targets != 0)
            {
                int to = BitOperations.TrailingZeroCount(targets);
                targets &= targets - 1;

                moves[_currMoveIndex++] = new Move(from, to);
            }
        }
    }

    private void GenerateKingMoves(Span<Move> moves)
    {
        int from = BitOperations.TrailingZeroCount(
            _board.Bitboards[_board.ToMove, (int)Piece.King]);
        ulong targets = KingAttacks.Table[from]
                        & ~_board.FriendlyPieces
                        & ~_enemyAttacks;
        if (_capturesOnly) targets &= _board.EnemyPieces;

        while (targets != 0)
        {
            int to = Bits.LSB(ref targets);
            moves[_currMoveIndex++] = new Move(from, to);
        }

        if (_capturesOnly) return;

        // Castling
        if (_board.ToMove == (int)Color.White)
        {
            // White kingside
            if ((_board.CastlingRights & 0b1000) != 0)
            {
                bool empty = (_board.Occupied & Masks.Between[4, 7]) == 0;
                bool safe = (_enemyAttacks & Masks.Between[3, 7]) == 0;
                if (empty && safe) moves[_currMoveIndex++] = new Move(4, 6);
            }

            // White queenside
            if ((_board.CastlingRights & 0b0100) != 0)
            {
                bool empty = (_board.Occupied & Masks.Between[4, 0]) == 0;
                bool safe = (_enemyAttacks & Masks.Between[5, 1]) == 0;
                if (empty && safe) moves[_currMoveIndex++] = new Move(4, 2);
            }

            return;
        }

        // Black kingside
        if ((_board.CastlingRights & 0b0010) != 0)
        {
            bool empty = (_board.Occupied & Masks.Between[60, 63]) == 0;
            bool safe = (_enemyAttacks & Masks.Between[59, 63]) == 0;
            if (empty && safe) moves[_currMoveIndex++] = new Move(60, 62);
        }

        // Black queenside
        if ((_board.CastlingRights & 0b0001) != 0)
        {
            bool empty = (_board.Occupied & Masks.Between[60, 56]) == 0;
            bool safe = (_enemyAttacks & Masks.Between[61, 57]) == 0;
            if (empty && safe) moves[_currMoveIndex++] = new Move(60, 58);
        }
    }

    private void GeneratePawnMoves(Span<Move> moves)
    {
        ulong bitboard = _board.Bitboards[_board.ToMove, (int)Piece.Pawn];

        bool isWhite = _board.ToMove == (int)Color.White;
        ulong startRank = isWhite ? Masks.Rank2 : Masks.Rank7;
        ulong promoRank = isWhite ? Masks.Rank8 : Masks.Rank1;

        while (bitboard != 0)
        {
            int from = BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;
            ulong pawn = 1UL << from;

            ulong singlePush = isWhite ? (pawn << 8) & _board.Empty : (pawn >> 8) & _board.Empty;
            ulong doublePush = (pawn & startRank) != 0
                ? isWhite ? (singlePush << 8) & _board.Empty : (singlePush >> 8) & _board.Empty
                : 0UL;

            if (_capturesOnly) { singlePush = 0; doublePush = 0; }

            ulong captures = PawnAttacks.Table[_board.ToMove, from] & _board.EnemyPieces;
            ulong targets = singlePush | doublePush | captures;
            targets &= _checkMask & _pinMasks[from];

            while (targets != 0)
            {
                int to = BitOperations.TrailingZeroCount(targets);
                targets &= targets - 1;
                ulong toBit = 1UL << to;

                if ((toBit & promoRank) != 0)
                {
                    moves[_currMoveIndex++] = new Move(from, to, Piece.Queen);
                    moves[_currMoveIndex++] = new Move(from, to, Piece.Rook);
                    moves[_currMoveIndex++] = (new Move(from, to, Piece.Bishop));
                    moves[_currMoveIndex++] = (new Move(from, to, Piece.Knight));
                }
                else
                    moves[_currMoveIndex++] = (new Move(from, to));
            }

            // EnPassant
            if (_board.EnPassantSquare == null)
                continue;

            ulong epTargets = PawnAttacks.Table[_board.ToMove, from] & (1UL << (int)_board.EnPassantSquare);

            while (epTargets != 0)
            {
                int to = BitOperations.TrailingZeroCount(epTargets);
                epTargets &= epTargets - 1;

                int capturedPawnSq = isWhite ? to - 8 : to + 8;

                if (_checkMask != 0xFFFFFFFFFFFFFFFFUL && (_checkMask & (1UL << capturedPawnSq)) == 0)
                    continue;

                if ((_pinMasks[from] & (1UL << to)) == 0)
                    continue;

                int kingSq = BitOperations.TrailingZeroCount(_board.Bitboards[_board.ToMove, (int)Piece.King]);

                if (kingSq / 8 == from / 8)
                {
                    ulong simulatedOccupied = _board.Occupied ^ (1UL << from) ^ (1UL << capturedPawnSq);
                    ulong rookAttacks = MagicBitboards.GetRookMoves(kingSq, simulatedOccupied);
                    ulong enemyHorizontalSliders = _board.Bitboards[_board.ToMove ^ 1, (int)Piece.Rook] |
                                                   _board.Bitboards[_board.ToMove ^ 1, (int)Piece.Queen];

                    if ((rookAttacks & enemyHorizontalSliders) != 0)
                    {
                        int sliderSq = BitOperations.TrailingZeroCount(rookAttacks & enemyHorizontalSliders);
                        if (sliderSq / 8 == kingSq / 8)
                            continue;
                    }
                }

                moves[_currMoveIndex++] = new Move(from, to, isEnPassant: true);
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
