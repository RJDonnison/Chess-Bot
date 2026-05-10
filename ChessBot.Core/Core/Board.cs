using System.Numerics;
using System.Text;
using ChessBot.Core.MoveGen;
using ChessBot.Core.Utilities;

namespace ChessBot.Core.Core;

public class Board
{
    public int ToMove { get; set; } = (int)Color.White;
    public ulong[,] Bitboards { get; } = new ulong[2, 6];

    public ulong WhitePieces => Bitboards[(int)Color.White, (int)Piece.Pawn] | Bitboards[(int)Color.White, (int)Piece.Knight] | Bitboards[(int)Color.White, (int)Piece.Bishop] | Bitboards[(int)Color.White, (int)Piece.Rook] | Bitboards[(int)Color.White, (int)Piece.Queen] | Bitboards[(int)Color.White, (int)Piece.King];
    public ulong BlackPieces => Bitboards[(int)Color.Black, (int)Piece.Pawn] | Bitboards[(int)Color.Black, (int)Piece.Knight] | Bitboards[(int)Color.Black, (int)Piece.Bishop] | Bitboards[(int)Color.Black, (int)Piece.Rook] | Bitboards[(int)Color.Black, (int)Piece.Queen] | Bitboards[(int)Color.Black, (int)Piece.King];

    public ulong FriendlyPieces => ToMove == (int)Color.White ? WhitePieces : BlackPieces;
    public ulong EnemyPieces => ToMove == (int)Color.White ? BlackPieces : WhitePieces;

    public ulong Occupied => WhitePieces | BlackPieces;
    public ulong Empty => ~Occupied;
    
    public int? EnPassantSquare { get; set; }
    public byte CastlingRights { get; set; }

    private Stack<BoardState> _history = new();
    
    public void MakeMove(Move move)
    {
        Piece piece = (Piece)GetPieceAt(move.From)!;
        ulong fromBit = 1UL << move.From;
        ulong toBit = 1UL << move.To;
        
        // Remove piece from square
        Bitboards[ToMove, (int)piece] ^= fromBit;
        
        Piece? captured = GetPieceAt(move.To);
        BoardState currentState = new BoardState(piece, captured, move.To, EnPassantSquare, CastlingRights);
        _history.Push(currentState);
        
        // Remove captured piece from to square
        if (captured != null)
            Bitboards[ToMove ^ 1, (int)captured] ^= toBit;

        // Add piece to new square
        Piece targetPiece = move.Promotion ?? piece;
        Bitboards[ToMove, (int)targetPiece] ^= toBit;
            
        // Update ToMove
        ToMove ^= 1;
    }

    public void UnmakeMove(Move move)
    {
        // Update ToMove
        ToMove ^= 1;
        
        // Restore state
        BoardState currentState = _history.Pop();
        EnPassantSquare = currentState.EnPassantSquare;
        CastlingRights = currentState.CastlingRights;
        
        Piece piece = (Piece)GetPieceAt(move.To)!;
        ulong fromBit = 1UL << move.From;
        ulong toBit = 1UL << move.To; 
       
       // Remove piece from square
       Bitboards[ToMove, (int)piece] ^= toBit; 
       
       // Added captured piece back to square
       if (currentState.Captured != null) 
           Bitboards[ToMove ^ 1, (int)currentState.Captured] ^= 1UL << currentState.CapturedSquare;
       
       // Add piece to square
       Bitboards[ToMove, (int)currentState.Moved] ^= fromBit;
    }

    public Piece? GetPieceAt(int sq)
    {
        if (((1UL << sq) & (Bitboards[(int)Color.White, (int)Piece.Pawn] | Bitboards[(int)Color.Black, (int)Piece.Pawn])) != 0)
            return Piece.Pawn;
        
        if (((1UL << sq) & (Bitboards[(int)Color.White, (int)Piece.Rook] | Bitboards[(int)Color.Black, (int)Piece.Rook])) != 0)
            return Piece.Rook;
        
        if (((1UL << sq) & (Bitboards[(int)Color.White, (int)Piece.Bishop] | Bitboards[(int)Color.Black, (int)Piece.Bishop])) != 0)
            return Piece.Bishop;
        
        if (((1UL << sq) & (Bitboards[(int)Color.White, (int)Piece.Knight] | Bitboards[(int)Color.Black, (int)Piece.Knight])) != 0)
            return Piece.Knight;
        
        if (((1UL << sq) & (Bitboards[(int)Color.White, (int)Piece.King] | Bitboards[(int)Color.Black, (int)Piece.King])) != 0)
            return Piece.King;
        
        if (((1UL << sq) & (Bitboards[(int)Color.White, (int)Piece.Queen] | Bitboards[(int)Color.Black, (int)Piece.Queen])) != 0)
            return Piece.Queen;

        return null;
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
    int CapturedSquare,
    int? EnPassantSquare,
    byte CastlingRights
);