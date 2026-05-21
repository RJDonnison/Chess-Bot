using ChessBot.Core.Core;

namespace ChessBot.Core.Utilities;

public static class Fen
{
    public static Board GetBoard(string fen)
    {
        Board board = new();
        string[] fenSections = fen.Split(' ');
        board.ToMove = fenSections[1] is "w" ? (int)Color.White : (int)Color.Black;

        board.HalfMoveClock = int.Parse(fenSections[4]);

        board.CastlingRights = 0;
        if (fenSections[2].Contains('K')) board.CastlingRights |= 0b1000;
        if (fenSections[2].Contains('Q')) board.CastlingRights |= 0b0100;
        if (fenSections[2].Contains('k')) board.CastlingRights |= 0b0010;
        if (fenSections[2].Contains('q')) board.CastlingRights |= 0b0001;

        if (!fenSections[3].Contains('-'))
            board.EnPassantSquare = BoardHelper.StringToSquare(fenSections[3]);

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

        board.RebuildMailbox();
        board.ZobristKey = ZobristTables.CalculateZobristKey(board);
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