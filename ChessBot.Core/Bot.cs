using ChessBot.Core.Core;
using ChessBot.Core.MoveGen;
using ChessBot.Core.Utilities;

namespace ChessBot.Core;

public class Bot
{
    public string GetBestMove(string fen)
    { 
        Board board = Fen.GetBoard(fen);
        List<Move> moves = MoveGenerator.GenerateMoves(board);
        Random rng = new Random();

        Move randomMove = moves[rng.Next(moves.Count - 0)];
        return randomMove.ToString();
    }
}