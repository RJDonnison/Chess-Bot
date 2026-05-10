using ChessBot.Core.Core;
using ChessBot.Core.MoveGen;
using ChessBot.Core.Tables;
using ChessBot.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ChessBot.Api.Controllers;

[ApiController]
[Route("/")]
public class ChessBotController : ControllerBase
{
    [HttpGet("/bestmove")]
    public IActionResult BestMove(string fen)
    {
        Board board = Fen.GetBoard(fen);
        List<Move> moves = MoveGenerator.GenerateMove(board);
        Random rng = new Random();

        Move randomMove = moves[rng.Next(moves.Count - 1)];
        return Ok(new
        {
            bestmove = randomMove.ToString()
        });
    }
    
    [HttpGet("/debug")]
    public IActionResult Debug(string? sq, string fen)
    {
        int sqInt = 0;
        if (sq != null)
        {
            int file = sq[0] - 'a';
            int rank = sq[1] - '1';
            sqInt = rank * 8 + file;
        }

        Board board = Fen.GetBoard(fen);
        if (sq == null)
            MoveGenerator.GenerateMove(board);
        else
            BitboardVisualizer.Bitboard = MagicBitboards.GetBishopMoves(sqInt, board.Occupied) & ~board.FriendlyPieces;
        
        return Ok(BitboardVisualizer.GetBitboard());
    }
}