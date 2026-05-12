using ChessBot.Core.Core;
using ChessBot.Core.MoveGen;
using ChessBot.Core.Search;
using ChessBot.Core.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace ChessBot.Api.Controllers;

[ApiController]
[Route("/")]
public class ChessBotController : ControllerBase
{
    private readonly Searcher _searcher = new();

    [HttpGet("/bestmove")]
    public IActionResult BestMove(string fen)
    {
        Board board = Fen.GetBoard(fen);

        Move bestMove = _searcher.GetBestMove(board);
        return Ok(new
        {
            bestmove = bestMove.ToString()
        });
    }

    [HttpGet("/debug")]
    public IActionResult Debug(string? sq, string fen)
    {
        // int sqInt = 0;
        // if (sq != null)
        // {
        //     int file = sq[0] - 'a';
        //     int rank = sq[1] - '1';
        //     sqInt = rank * 8 + file;
        // }
        //
        // Board board = Fen.GetBoard(fen);
        // if (sq == null)
        //     MoveGenerator.GenerateMoves(board);
        // else
        //     BitboardVisualizer.Bitboard = MagicBitboards.GetRookMoves(sqInt, board.Occupied) & ~board.FriendlyPieces;

        return Ok();
    }
}