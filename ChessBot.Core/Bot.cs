using ChessBot.Core.Core;
using ChessBot.Core.MoveGen;
using ChessBot.Core.Search;
using ChessBot.Core.Utilities;

namespace ChessBot.Core;

public class Bot
{
    private readonly Searcher _searcher = new();
    private const int ThinkTimeMs = 1000;

    public string GetBestMove(string fen)
    {
        Board board = Fen.GetBoard(fen);

        string pos = fen.Substring(0, fen.Length - 4);
        if (board.HalfMoveClock <= 10 && OpeningBook.BookContains(pos))
            return OpeningBook.GetMove(pos).ToString();

        Thread searchThread = new(() => _searcher.StartSearch(board)) { IsBackground = true };
        searchThread.Start();
        Thread.Sleep(ThinkTimeMs);

        _searcher.StopSearch();
        searchThread.Join();

        return _searcher.GetFoundMove().ToString();
    }

    public string GetBestMove(string fen, int remainingTimeMs)
    {
        Board board = Fen.GetBoard(fen);

        string pos = fen.Substring(0, fen.Length - 4);
        if (board.HalfMoveClock <= 10 && OpeningBook.BookContains(pos))
            return OpeningBook.GetMove(pos).ToString();

        // Assume ~30 moves remaining, use 1/30th of remaining time
        int thinkTimeMs = remainingTimeMs / 30;

        // Clamp between 100ms minimum and 5s maximum
        thinkTimeMs = Math.Clamp(thinkTimeMs, 100, 5000);

        Thread searchThread = new(() => _searcher.StartSearch(board)) { IsBackground = true };
        searchThread.Start();
        Thread.Sleep(thinkTimeMs);

        _searcher.StopSearch();
        searchThread.Join();

        return _searcher.GetFoundMove().ToString();
    }
}