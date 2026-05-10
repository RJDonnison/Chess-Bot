using ChessBot.Core.Core;
using ChessBot.Core.MoveGen;
using ChessBot.Core.Utilities;
using Xunit.Abstractions;

namespace ChessBot.Core.Tests;

public class PerftTests
{
    private readonly ITestOutputHelper _output;

    public PerftTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> PerftData =>
    [
        // Starting position
        ["rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 1, 20L],
        ["rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 2, 400L],
        ["rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 3, 8_902L],
        ["rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 4, 197_281L],
        ["rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", 5, 4_865_609L],

        // Kiwipete - stress tests castling, captures, promotions
        ["r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 1, 48L],
        ["r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 2, 2_039L],
        ["r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 3, 97_862L],
        ["r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 4, 4_085_603L],

        // Position 3 - en passant edge cases
        ["8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 1, 14L],
        ["8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 2, 191L],
        ["8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 3, 2_812L],
        ["8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 4, 43_238L],
        ["8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 5, 674_624L],

        // Position 4 - promotions
        ["r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 1, 6L],
        ["r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 2, 264L],
        ["r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 3, 9_467L],
        ["r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 4, 422_333L],

        // Position 5
        ["rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 1, 44L],
        ["rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 2, 1_486L],
        ["rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 3, 62_379L],
        ["rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 4, 2_103_487L],
    ];

    [Theory]
    [MemberData(nameof(PerftData))]
    public void PerftTest(string fen, int depth, long expected)
    {
        Board board = Fen.GetBoard(fen);

        long result = Perft(board, depth);

        if (result != expected)
            PerftDivide(board, depth);

        Assert.Equal(expected, result);
    }

    private long Perft(Board board, int depth)
    {
        if (depth == 0) return 1;

        long nodes = 0;
        List<Move> moves = MoveGenerator.GenerateMoves(board);

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            nodes += Perft(board, depth - 1);

            board.UnmakeMove(move);
        }

        return nodes;
    }

    private void PerftDivide(Board board, int depth)
    {
        long total = 0;
        List<Move> moves = MoveGenerator.GenerateMoves(board);

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            long nodes = Perft(board, depth - 1);
            total += nodes;
            Console.WriteLine($"{BoardHelper.MoveToString(move)}: {nodes}");

            board.UnmakeMove(move);
        }

        Console.WriteLine($"\nTotal: {total}");
    }


}