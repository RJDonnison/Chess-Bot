using System;
using System.Runtime.InteropServices.JavaScript;
using ChessBot.Core;

Console.WriteLine("ChessBot WASM loaded");

public partial class Chess
{
    private static readonly Bot _bot = new();
    
    [JSExport]
    public static string GetBestMove(string fen)
    {
        return _bot.GetBestMove(fen);
    }
}
