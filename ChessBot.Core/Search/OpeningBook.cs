using System.Reflection;
using ChessBot.Core.MoveGen;
using ChessBot.Core.Utilities;

namespace ChessBot.Core.Search;

public static class OpeningBook
{
    private static readonly Dictionary<string, WeightedMove[]> MovesDict;
    private static readonly Random Rand;

    static OpeningBook()
    {
        Rand = new Random();
        MovesDict = new Dictionary<string, WeightedMove[]>();

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ChessBot.Core.Data.OpeningBook.txt");
        using var reader = new StreamReader(stream!);
        string[] lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string currentPos = null;
        List<WeightedMove> currentMoves = new();

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("pos "))
            {
                if (currentPos != null && currentMoves.Count > 0)
                    MovesDict[currentPos] = currentMoves.ToArray();

                currentPos = trimmed.Substring(4); 
                currentMoves = new List<WeightedMove>();
            }
            else if (currentPos != null)
            {
                string[] parts = trimmed.Split(' ');
                if (parts.Length == 2 && int.TryParse(parts[1], out int weight))
                {
                    string moveString = parts[0];
                    int from = BoardHelper.StringToSquare(moveString.Substring(0, 2));
                    int to = BoardHelper.StringToSquare(moveString.Substring(2, 2));
                    currentMoves.Add(new WeightedMove
                    {
                        Move = new Move(from, to),
                        Weight = weight
                    });
                }
            }
        }

        if (currentPos != null && currentMoves.Count > 0)
            MovesDict[currentPos] = currentMoves.ToArray(); 
    }

    public static bool BookContains(string pos) => MovesDict.ContainsKey(pos);

    public static Move GetMove(string pos)
    {
        if (!BookContains(pos))
            throw new ArgumentException($"Invalid pos '{pos}'");
        
        WeightedMove[] moves = MovesDict[pos];

        int totalWeight = 0;
        foreach (var m in moves)
            totalWeight += m.Weight;

        int roll = Rand.Next(totalWeight);

        int cumulative = 0;
        foreach (var m in moves)
        {
            cumulative += m.Weight;
            if (roll < cumulative)
                return m.Move;
        }

        return moves[^1].Move;  
    }
    
    private struct WeightedMove
    {
        public int Weight;
        public Move Move;
    }
}