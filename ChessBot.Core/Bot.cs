using ChessBot.Core.Core;
using ChessBot.Core.MoveGen;
using ChessBot.Core.Search;
using ChessBot.Core.Utilities;

namespace ChessBot.Core;

public class Bot
{
    private readonly Searcher _searcher = new Searcher();

    public string GetBestMove(string fen)
    { 
        Board board = Fen.GetBoard(fen);

        string pos = fen.Substring(0, fen.Length - 4);
        if (board.HalfMoveClock <= 10 && OpeningBook.BookContains(pos))
            return OpeningBook.GetMove(pos).ToString();
        
        return _searcher.GetBestMove(board).ToString();
    }
}