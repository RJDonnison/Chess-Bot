using System.Numerics;
using System.Text;
using ChessBot.Core.MoveGen;
using ChessBot.Core.Utilities;

namespace ChessBot.Core.Core;

public class Board
{
    public int ToMove { get; set; } = (int)Color.White;
    public int? EnPassantSquare { get; set; }
    public byte CastlingRights { get; set; }

    public ulong ZobristKey;

    public int HalfMoveClock = 0;
    public bool Drawn => HalfMoveClock >= 100;

    public ulong[,] Bitboards { get; } = new ulong[2, 6];

    // Mailbox for O(1) piece lookups
    private readonly Piece?[] _squarePieces = new Piece?[64];

    // Cached composite bitboards for O(1) access
    private ulong _whitePieces;
    private ulong _blackPieces;
    private ulong _occupied;

    public ulong WhitePieces => _whitePieces;
    public ulong BlackPieces => _blackPieces;

    public ulong FriendlyPieces => ToMove == (int)Color.White ? _whitePieces : _blackPieces;
    public ulong EnemyPieces => ToMove == (int)Color.White ? _blackPieces : _whitePieces;

    public ulong Occupied => _occupied;
    public ulong Empty => ~_occupied;

    private Stack<BoardState> _history = new();

    public Piece? GetPieceAt(int sq) => _squarePieces[sq];

    public void MakeMove(Move move)
    {
        HalfMoveClock++;

        Piece piece = _squarePieces[move.From]!.Value;
        ulong fromBit = 1UL << move.From;
        ulong toBit = 1UL << move.To;

        // Remove piece from square
        Bitboards[ToMove, (int)piece] ^= fromBit;
        ZobristKey ^= ZobristTables.Pieces[ToMove, (int)piece, move.From];

        // Update Mailbox
        _squarePieces[move.From] = null;

        // Update composite bitboards
        if (ToMove == (int)Color.White)
            _whitePieces ^= fromBit;
        else
            _blackPieces ^= fromBit;

        Piece? captured = _squarePieces[move.To];

        // En passant capture
        if (piece == Piece.Pawn && move.To == EnPassantSquare)
        {
            int capturedPawnSq = move.To + (ToMove == (int)Color.White ? -8 : 8);
            Bitboards[ToMove ^ 1, (int)Piece.Pawn] ^= 1UL << capturedPawnSq;
            ZobristKey ^= ZobristTables.Pieces[ToMove ^ 1, (int)Piece.Pawn, capturedPawnSq];

            // Update Mailbox
            _squarePieces[capturedPawnSq] = null;

            // Update composite bitboards
            if (ToMove == (int)Color.White)
                _blackPieces ^= 1UL << capturedPawnSq;
            else
                _whitePieces ^= 1UL << capturedPawnSq;

            captured = Piece.Pawn;
        }

        // Remove captured normal piece from to square
        if (captured != null && move.To != EnPassantSquare)
        {
            Bitboards[ToMove ^ 1, (int)captured] ^= toBit;
            ZobristKey ^= ZobristTables.Pieces[ToMove ^ 1, (int)captured, move.To];

            // Update composite bitboards
            if (ToMove == (int)Color.White)
                _blackPieces ^= toBit;
            else
                _whitePieces ^= toBit;
        }

        BoardState currentState = new BoardState(piece, captured, EnPassantSquare, CastlingRights);
        _history.Push(currentState);

        // Add piece to new square
        Piece targetPiece = move.Promotion ?? piece;
        Bitboards[ToMove, (int)targetPiece] ^= toBit;
        ZobristKey ^= ZobristTables.Pieces[ToMove, (int)targetPiece, move.To];

        // Update Mailbox
        _squarePieces[move.To] = targetPiece;

        // Update composite bitboards
        if (ToMove == (int)Color.White)
            _whitePieces ^= toBit;
        else
            _blackPieces ^= toBit;

        // Update en passant square
        if (EnPassantSquare != null)
            ZobristKey ^= ZobristTables.EnPassantFile[EnPassantSquare.Value % 8];

        EnPassantSquare = piece == Piece.Pawn && Math.Abs(move.To - move.From) == 16
            ? (move.From + move.To) / 2
            : null;

        if (EnPassantSquare != null)
            ZobristKey ^= ZobristTables.EnPassantFile[EnPassantSquare.Value % 8];

        // Castling 
        if (piece == Piece.King && Math.Abs(move.To - move.From) == 2)
        {
            (int rookFrom, int rookTo) = move.To switch
            {
                6 => (7, 5),   // White kingside
                2 => (0, 3),   // White queenside
                62 => (63, 61),  // Black kingside
                58 => (56, 59),  // Black queenside
                _ => throw new Exception("Invalid castle")
            };

            Bitboards[ToMove, (int)Piece.Rook] ^= (1UL << rookFrom) | (1UL << rookTo);
            ZobristKey ^= ZobristTables.Pieces[ToMove, (int)Piece.Rook, rookFrom];
            ZobristKey ^= ZobristTables.Pieces[ToMove, (int)Piece.Rook, rookTo];

            // Update Mailbox
            _squarePieces[rookFrom] = null;
            _squarePieces[rookTo] = Piece.Rook;

            // Update composite bitboards
            if (ToMove == (int)Color.White)
            {
                _whitePieces ^= (1UL << rookFrom) | (1UL << rookTo);
            }
            else
            {
                _blackPieces ^= (1UL << rookFrom) | (1UL << rookTo);
            }
        }

        ZobristKey ^= ZobristTables.CastlingRights[CastlingRights];

        if (piece == Piece.King)
            CastlingRights &= (byte)(ToMove == (int)Color.White ? 0b0011 : 0b1100);

        CastlingRights &= (byte)~(move.From switch
        {
            7 => 0b1000,
            0 => 0b0100,
            63 => 0b0010,
            56 => 0b0001,
            _ => 0
        });

        CastlingRights &= (byte)~(move.To switch
        {
            7 => 0b1000,
            0 => 0b0100,
            63 => 0b0010,
            56 => 0b0001,
            _ => 0
        });

        ZobristKey ^= ZobristTables.CastlingRights[CastlingRights];

        // Update ToMove
        ToMove ^= 1;
        ZobristKey ^= ZobristTables.SideToMove;

        // Update _occupied
        _occupied = _whitePieces | _blackPieces;
    }

    public void MakeNullMove()
    {
        ToMove ^= 1;
        ZobristKey ^= ZobristTables.SideToMove;

        if (EnPassantSquare != null)
        {
            ZobristKey ^= ZobristTables.EnPassantFile[EnPassantSquare.Value % 8];
            EnPassantSquare = null;
        }

        HalfMoveClock++;
        _history.Push(new BoardState(default, null, null, CastlingRights));
    }

    public void UnmakeMove(Move move)
    {
        HalfMoveClock--;
        // Update ToMove
        ToMove ^= 1;
        ZobristKey ^= ZobristTables.SideToMove;

        ZobristKey ^= ZobristTables.CastlingRights[CastlingRights];
        if (EnPassantSquare != null)
            ZobristKey ^= ZobristTables.EnPassantFile[EnPassantSquare.Value % 8];

        // Restore state
        BoardState currentState = _history.Pop();
        EnPassantSquare = currentState.EnPassantSquare;
        CastlingRights = currentState.CastlingRights;

        ZobristKey ^= ZobristTables.CastlingRights[CastlingRights];
        if (EnPassantSquare != null)
            ZobristKey ^= ZobristTables.EnPassantFile[EnPassantSquare.Value % 8];

        ulong fromBit = 1UL << move.From;
        ulong toBit = 1UL << move.To;

        // Remove piece from square
        Piece pieceOnTarget = move.Promotion ?? currentState.Moved;
        Bitboards[ToMove, (int)pieceOnTarget] ^= toBit;
        ZobristKey ^= ZobristTables.Pieces[ToMove, (int)pieceOnTarget, move.To];

        // Update Mailbox and composite bitboards
        _squarePieces[move.To] = null;
        if (ToMove == (int)Color.White)
            _whitePieces ^= toBit;
        else
            _blackPieces ^= toBit;

        // Castling
        if (currentState.Moved == Piece.King && Math.Abs(move.To - move.From) == 2)
        {
            (int rookFrom, int rookTo) = move.To switch
            {
                6 => (7, 5),   // White kingside
                2 => (0, 3),   // White queenside
                62 => (63, 61),  // Black kingside
                58 => (56, 59),  // Black queenside
                _ => throw new Exception("Invalid castle")
            };

            Bitboards[ToMove, (int)Piece.Rook] ^= (1UL << rookFrom) | (1UL << rookTo);
            ZobristKey ^= ZobristTables.Pieces[ToMove, (int)Piece.Rook, rookTo];
            ZobristKey ^= ZobristTables.Pieces[ToMove, (int)Piece.Rook, rookFrom];

            // Update Mailbox and composite bitboards
            _squarePieces[rookFrom] = Piece.Rook;
            _squarePieces[rookTo] = null;
            if (ToMove == (int)Color.White)
            {
                _whitePieces ^= (1UL << rookFrom) | (1UL << rookTo);
            }
            else
            {
                _blackPieces ^= (1UL << rookFrom) | (1UL << rookTo);
            }
        }

        // Added captured piece back to square
        if (currentState.Moved == Piece.Pawn && move.To == currentState.EnPassantSquare)
        {
            int capturedPawnSq = move.To + (ToMove == (int)Color.White ? -8 : 8);
            Bitboards[ToMove ^ 1, (int)Piece.Pawn] ^= 1UL << capturedPawnSq;
            ZobristKey ^= ZobristTables.Pieces[ToMove ^ 1, (int)Piece.Pawn, capturedPawnSq];

            // Update Mailbox and composite bitboards
            _squarePieces[capturedPawnSq] = Piece.Pawn;
            if (ToMove == (int)Color.White)
                _blackPieces ^= 1UL << capturedPawnSq;
            else
                _whitePieces ^= 1UL << capturedPawnSq;
        }

        if (currentState.Captured != null && move.To != currentState.EnPassantSquare)
        {
            Bitboards[ToMove ^ 1, (int)currentState.Captured] ^= 1UL << move.To;
            ZobristKey ^= ZobristTables.Pieces[ToMove ^ 1, (int)currentState.Captured, move.To];

            // Update Mailbox and composite bitboards
            _squarePieces[move.To] = currentState.Captured;
            if (ToMove == (int)Color.White)
                _blackPieces ^= toBit;
            else
                _whitePieces ^= toBit;
        }

        // Add piece to square
        Bitboards[ToMove, (int)currentState.Moved] ^= fromBit;
        ZobristKey ^= ZobristTables.Pieces[ToMove, (int)currentState.Moved, move.From];

        // Update Mailbox and composite bitboards
        _squarePieces[move.From] = currentState.Moved;
        if (ToMove == (int)Color.White)
            _whitePieces ^= fromBit;
        else
            _blackPieces ^= fromBit;

        // Update _occupied
        _occupied = _whitePieces | _blackPieces;
    }

    public void UnmakeNullMove()
    {
        BoardState state = _history.Pop();
        EnPassantSquare = state.EnPassantSquare;
        CastlingRights = state.CastlingRights;

        ToMove ^= 1;
        ZobristKey ^= ZobristTables.SideToMove;
        
        if (EnPassantSquare != null)
            ZobristKey ^= ZobristTables.EnPassantFile[EnPassantSquare.Value % 8];

        HalfMoveClock--;
    }

    public void RebuildMailbox()
    {
        Array.Fill(_squarePieces, null);

        _whitePieces = 0;
        _blackPieces = 0;

        for (int color = 0; color < 2; color++)
        {
            for (int piece = 0; piece < 6; piece++)
            {
                ulong bb = Bitboards[color, piece];

                while (bb != 0)
                {
                    int sq = BitOperations.TrailingZeroCount(bb);
                    _squarePieces[sq] = (Piece)piece;

                    if (color == (int)Color.White)
                        _whitePieces |= 1UL << sq;
                    else
                        _blackPieces |= 1UL << sq;

                    bb &= bb - 1;
                }
            }
        }

        _occupied = _whitePieces | _blackPieces;
    }

    public override string ToString()
    {
        char[] squares = new char[64];

        Array.Fill(squares, '.');

        for (int color = 0; color < 2; color++)
        {
            for (int piece = 0; piece < 6; piece++)
            {
                ulong bb = Bitboards[color, piece];

                while (bb != 0)
                {
                    int sq = BitOperations.TrailingZeroCount(bb);

                    char ch = BoardHelper.PieceToChar((Piece)piece);
                    squares[sq] = (Color)color == Color.White ? char.ToUpper(ch) : ch;

                    bb &= bb - 1;
                }
            }
        }

        StringBuilder sb = new();

        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                int sq = rank * 8 + file;

                sb.Append(squares[sq]);
                sb.Append(' ');
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}

record BoardState(
    Piece Moved,
    Piece? Captured,
    int? EnPassantSquare,
    byte CastlingRights
);
