using ChessBot.Core.Core;
using ChessBot.Core.MoveGen;
using ChessBot.Core.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ChessBot.Api.Controllers;

[ApiController]
[Route("/")]
public class ChessBotController : ControllerBase
{
    [HttpGet("/bestmove")]
    public IActionResult Get(string fen)
    {
        Board board = Fen.GetBoard(fen);
        Console.WriteLine(board);
        List<Move> moves = MoveGenerator.GenerateMove(board);
        Random rng = new Random();

        Move randomMove = moves[rng.Next(moves.Count - 1)];
        Console.WriteLine(randomMove);
        return Ok(new
            {
                bestmove = randomMove.ToString()
            });
    }
}