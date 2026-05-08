namespace ChessBot.Core.Core;

public enum Piece
{
    Pawn,
    Knight,
    Bishop,
    Rook,
    Queen,
    King
}

public static class PieceUtils
{
    public static char PieceToChar(Piece? piece) => piece switch
    {
        Piece.Pawn => 'p',
        Piece.Knight => 'n',
        Piece.Bishop => 'b',
        Piece.Rook => 'r',
        Piece.Queen => 'q',
        Piece.King => 'k',

        _ => '.'
    };

}