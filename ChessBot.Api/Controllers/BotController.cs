using ChessBot.Core;
using Microsoft.AspNetCore.Mvc;

namespace ChessBot.Api.Controllers;

[ApiController]
[Route("/")]
public class ChessBotController : ControllerBase
{
    private readonly Bot _bot = new();

    [HttpGet("/bestmove")]
    public async Task<IActionResult> BestMove(string fen)
    {
        var move = await Task.Run(() => _bot.GetBestMove(fen));
        return Ok(new { bestmove = move });
    }

    [HttpGet("/debug")]
    public IActionResult Debug(string? sq, string fen)
    {
        return Ok();
    }
}