using ChessBot.Core.Core;

namespace ChessBot.Core.Utilities;

public static class Fen
{
    public static Board GetBoard(string fen)
    {
        Board board = new();
        string[] fenSections = fen.Split(' ');
        board.ToMove = fenSections[1] is "w" ? (int)Color.White : (int)Color.Black;

        string[] ranks = fenSections[0].Split('/');

        for (int rank = 0; rank < 8; rank++)
        {
            int file = 0;

            foreach (char c in ranks[rank])
            {
                if (char.IsDigit(c))
                {
                    file += c - '0';
                    continue;
                }

                int sq = (7 - rank) * 8 + file;
                ulong bb = Bits.SetBit(sq);

                Piece piece = FenToPiece[char.ToLower(c)];
                Color color = char.IsUpper(c) ? Color.White : Color.Black;
                board.Bitboards[(int)color, (int)piece] |= bb;

                file++;
            }
        }

        return board;
    }

    private static readonly Dictionary<char, Piece> FenToPiece = new()
    {
        ['p'] = Piece.Pawn,
        ['n'] = Piece.Knight,
        ['b'] = Piece.Bishop,
        ['r'] = Piece.Rook,
        ['q'] = Piece.Queen,
        ['k'] = Piece.King
    };
}