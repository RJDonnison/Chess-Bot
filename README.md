# Chess Bot

A C# chess engine built with bitboards and accessible via a REST API.

**Try it:** [Chess Bot](https://chess.reujdon.dev)

---

## Current Features

### Core Engine
- **Bitboard Board Representation**
- **Magic Bitboards**
- **Legal Move Generation**
- **Opening Book*
- **Repetition Detection**

### Search
- **Alpha-Beta Pruning**
- **Transposition Tables**
- **Move Ordering**
- **PVS**
- **Mate Search**
- **Asperation Windows**
- **Null Move Search**
- **Itterative deeping**
- **Quiescence Search**

### Evaluation
- **Material Evaluation**
- **Piece-Square Tables**
- **Pawn Structure**

---

## Elo Speculation

Based on current implementation, I estimate the bot to play at approximately **1600-1900 Elo**. This is an informal estimate.

---

## Usage

### Prerequisites
- .NET 10.0 SDK or later

### Building & Running

1. **Clone the repository:**
   ```bash
   git clone https://github.com/RJDonnison/ChessBot.git
   cd ChessBot
   ```

2. **Run the API server:**
   ```bash
   dotnet run --project ChessBot.Api
   ```

   The API will start on `http://localhost:5000`

### Testing with a UI

To test without building a custom UI, you can use the provided [Chess Bot Interface](https://github.com/RJDonnison/chess-bot-interface).

### API Endpoints

#### GET `/bestmove`
Returns the best move for a given position.

**Parameters:**
- `fen` (required) - The board position in FEN notation

**Example:**
```bash
curl "http://localhost:5000/bestmove?fen=rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR%20w%20KQkq%20-%200%201"
```

**Response:**
```json
{
  "bestmove": "e2e4"
}
```

### Integration Example

```csharp
// Import the ChessBot.Core library
using ChessBot.Core;

var bot = new Bot();
string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
string bestMove = bot.GetBestMove(fen);
Console.WriteLine($"Best move: {bestMove}");
```

---

## Project Structure

- **ChessBot.Core** - Core engine logic
- **ChessBot.Api** - ASP.NET API wrapper
- **ChessBot.Core.Tests** - Unit tests for Perft move generation tests

---

## Future Features (Roadmap)

- [ ] WebAssembly (WASM) - Browser-native engine compilation
- [ ] Multi-threading - Parallel search implementation
- [ ] ML evaluation - Neural network-based position evaluation
- [ ] History heuristic - Move ordering improvement using historical data
- [ ] King safety evaluation - Enhanced king attack detection and safety scoring
- [ ] Lazy evaluation - Deferred evaluation for performance

---

## License

See [LICENSE](LICENSE) for details.
