using ChessBot.Core;
using Microsoft.AspNetCore.Mvc;

namespace ChessBot.Api.Controllers;

[ApiController]
[Route("/")]
public class ChessBotController(Bot bot) : ControllerBase
{
    private readonly Bot _bot = bot;

    [HttpGet("/bestmove")]
    public async Task<IActionResult> BestMove(string fen, int? time)
    {
        var move = time == null
            ? await Task.Run(() => _bot.GetBestMove(fen))
            : await Task.Run(() => _bot.GetBestMove(fen, time.Value));

        return Ok(new { bestmove = move });
    }


    [HttpGet("/debug")]
    public IActionResult Debug(string? sq, string fen)
    {
        return Ok();
    }
}