using Microsoft.AspNetCore.Mvc;

namespace ChessBot.Api.Controllers;

[ApiController]
[Route("/")]
public class ChessBotController : ControllerBase
{
    [HttpGet ("/bestmove")]
    public IActionResult Get(string fen)
    {
        Console.WriteLine(fen);
        return Ok("ChessBot is running!");
    }
}