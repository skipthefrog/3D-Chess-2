# 3D Chess Scalability - Usage Guide

## Quick Start: How to Use the New Scalable System

### Overview
The game now supports dynamic board sizes (4x4x4, 6x6x6, 8x8x8) and player counts (2-6). This guide shows you how to configure and use these features.

---

## Basic Configuration

### 1. Setting Board Dimensions

```csharp
// Set board size at game initialization
if (BoardDimensionsManager.Instance != null)
{
    // Choose one:
    BoardDimensionsManager.Instance.SetDimensions(new Vector3Int(4, 4, 4)); // Small (2 players)
    BoardDimensionsManager.Instance.SetDimensions(new Vector3Int(6, 6, 6)); // Medium (4 players)
    BoardDimensionsManager.Instance.SetDimensions(new Vector3Int(8, 8, 8)); // Large (6 players)
}
```

### 2. Configuring Players

```csharp
// Set active players
if (PlayerManager.Instance != null)
{
    // 2-Player (Original)
    PlayerManager.Instance.SetPlayers(new List<PieceColor> {
        PieceColor.White,
        PieceColor.Black
    });

    // 4-Player
    PlayerManager.Instance.SetPlayers(new List<PieceColor> {
        PieceColor.White,
        PieceColor.Black,
        PieceColor.Red,
        PieceColor.Green
    });

    // 6-Player (Maximum)
    PlayerManager.Instance.SetPlayers(new List<PieceColor> {
        PieceColor.White,
        PieceColor.Black,
        PieceColor.Red,
        PieceColor.Green,
        PieceColor.Blue,
        PieceColor.Yellow
    });
}
```

### 3. Registering Piece Trays

```csharp
// Each tray should register itself (usually in PieceTray.Awake())
if (PieceTrayManager.Instance != null)
{
    PieceTrayManager.Instance.RegisterTray(pieceColor, this);
}

// Accessing trays from other code
PieceTray whiteTray = PieceTrayManager.Instance?.GetTray(PieceColor.White);
```

---

## Common Usage Patterns

### Accessing Board Dimensions

```csharp
// Get current board dimensions
Vector3Int dimensions = BoardDimensionsManager.Instance != null
    ? BoardDimensionsManager.Instance.GetDimensions()
    : new Vector3Int(4, 4, 4); // Fallback

// Iterate over all board positions
for (int x = 0; x < dimensions.x; x++)
{
    for (int y = 0; y < dimensions.y; y++)
    {
        for (int z = 0; z < dimensions.z; z++)
        {
            BoardPosition pos = new BoardPosition(x, y, z);
            // Your logic here
        }
    }
}

// Get total squares
int totalSquares = BoardDimensionsManager.Instance?.GetTotalSquares() ?? 64;
```

### Working with Multiple Players

```csharp
// Get all active players
List<PieceColor> allPlayers = PlayerManager.Instance?.GetAllPlayers()
    ?? new List<PieceColor> { PieceColor.White, PieceColor.Black };

// Get opponents of a player
List<PieceColor> opponents = PlayerManager.Instance?.GetOpponents(PieceColor.White)
    ?? new List<PieceColor> { PieceColor.Black };

// Get next player in turn order
PieceColor nextPlayer = PlayerManager.Instance?.GetNextPlayer(currentPlayer)
    ?? (currentPlayer == PieceColor.White ? PieceColor.Black : PieceColor.White);

// Check all opponents for threats
foreach (PieceColor opponent in opponents)
{
    if (IsPositionUnderAttack(position, opponent))
    {
        // Position is threatened by this opponent
    }
}
```

### Accessing Piece Trays

```csharp
// Get a specific player's tray
PieceTray playerTray = PieceTrayManager.Instance?.GetTray(playerColor)
    ?? (playerColor == PieceColor.White ? PieceTray.WhiteTray : PieceTray.BlackTray);

// Check if all trays are registered
bool allReady = PieceTrayManager.Instance?.AllTraysRegistered() ?? false;

// Get all trays
Dictionary<PieceColor, PieceTray> allTrays = PieceTrayManager.Instance?.GetAllTrays()
    ?? new Dictionary<PieceColor, PieceTray>();

// Get total pieces across all trays
int totalPieces = PieceTrayManager.Instance?.GetTotalPieceCount() ?? 0;
```

---

## Recommended Configurations

### Configuration 1: Classic 2-Player
**Best For**: Original gameplay, backward compatibility
```csharp
BoardDimensionsManager.Instance.SetDimensions(new Vector3Int(4, 4, 4));
PlayerManager.Instance.SetPlayers(new List<PieceColor> {
    PieceColor.White, PieceColor.Black
});
// Register 2 trays (White and Black)
```
**Board**: 64 squares
**Complexity**: Low
**AI Depth**: 3-4 (standard)

### Configuration 2: Small Multi-Player (4 Players)
**Best For**: Multi-player introduction, moderate complexity
```csharp
BoardDimensionsManager.Instance.SetDimensions(new Vector3Int(6, 6, 6));
PlayerManager.Instance.SetPlayers(new List<PieceColor> {
    PieceColor.White, PieceColor.Black, PieceColor.Red, PieceColor.Green
});
// Register 4 trays
```
**Board**: 216 squares
**Complexity**: Medium
**AI Depth**: 2-3 (recommended)

### Configuration 3: Maximum Multi-Player (6 Players)
**Best For**: High complexity, tournament play
```csharp
BoardDimensionsManager.Instance.SetDimensions(new Vector3Int(8, 8, 8));
PlayerManager.Instance.SetPlayers(new List<PieceColor> {
    PieceColor.White, PieceColor.Black, PieceColor.Red,
    PieceColor.Green, PieceColor.Blue, PieceColor.Yellow
});
// Register 6 trays
```
**Board**: 512 squares
**Complexity**: High
**AI Depth**: 1-2 (performance)

---

## Integration Examples

### Example 1: Game Initialization Script

```csharp
public class GameSetup : MonoBehaviour
{
    [Header("Game Configuration")]
    public int boardSize = 4; // 4, 6, or 8
    public int playerCount = 2; // 2-6

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        // Step 1: Set board dimensions
        Vector3Int dimensions = new Vector3Int(boardSize, boardSize, boardSize);
        BoardDimensionsManager.Instance?.SetDimensions(dimensions);

        // Step 2: Configure players
        List<PieceColor> players = GetPlayersForCount(playerCount);
        PlayerManager.Instance?.SetPlayers(players);

        // Step 3: Trays will auto-register themselves

        Debug.Log($"Game initialized: {boardSize}x{boardSize}x{boardSize} board, {playerCount} players");
    }

    List<PieceColor> GetPlayersForCount(int count)
    {
        List<PieceColor> allColors = new List<PieceColor> {
            PieceColor.White, PieceColor.Black, PieceColor.Red,
            PieceColor.Green, PieceColor.Blue, PieceColor.Yellow
        };

        return allColors.GetRange(0, Mathf.Clamp(count, 2, 6));
    }
}
```

### Example 2: Dynamic Board Scanning

```csharp
public List<ChessPiece> GetAllPiecesOfColor(PieceColor color)
{
    List<ChessPiece> pieces = new List<ChessPiece>();

    // Get dynamic board dimensions
    Vector3Int dimensions = BoardDimensionsManager.Instance != null
        ? BoardDimensionsManager.Instance.GetDimensions()
        : new Vector3Int(4, 4, 4);

    // Scan entire board
    for (int x = 0; x < dimensions.x; x++)
    {
        for (int y = 0; y < dimensions.y; y++)
        {
            for (int z = 0; z < dimensions.z; z++)
            {
                BoardPosition pos = new BoardPosition(x, y, z);
                ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);

                if (piece != null && piece.pieceColor == color)
                {
                    pieces.Add(piece);
                }
            }
        }
    }

    return pieces;
}
```

### Example 3: Multi-Player Turn Cycling

```csharp
public void AdvanceToNextPlayer()
{
    PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();

    // Get next player using PlayerManager
    PieceColor nextPlayer;
    if (PlayerManager.Instance != null)
    {
        nextPlayer = PlayerManager.Instance.GetNextPlayer(currentPlayer);
    }
    else
    {
        // Fallback for 2-player
        nextPlayer = (currentPlayer == PieceColor.White) ? PieceColor.Black : PieceColor.White;
    }

    TurnManager.Instance.SetCurrentPlayer(nextPlayer);
    Debug.Log($"Turn advanced: {currentPlayer} -> {nextPlayer}");
}
```

### Example 4: Multi-Opponent Threat Detection

```csharp
public bool IsPositionThreatenedByAnyOpponent(BoardPosition position, PieceColor defenderColor)
{
    // Get all opponents of the defender
    List<PieceColor> opponents;
    if (PlayerManager.Instance != null)
    {
        opponents = PlayerManager.Instance.GetOpponents(defenderColor);
    }
    else
    {
        // Fallback for 2-player
        PieceColor opponent = (defenderColor == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        opponents = new List<PieceColor> { opponent };
    }

    // Check if any opponent threatens this position
    foreach (PieceColor opponentColor in opponents)
    {
        if (CheckDetectionManager.Instance.IsPositionUnderAttack(position, opponentColor))
        {
            Debug.Log($"Position {position} is threatened by {opponentColor}");
            return true;
        }
    }

    return false;
}
```

---

## Best Practices

### 1. Always Check for Null
```csharp
// Good
Vector3Int dimensions = BoardDimensionsManager.Instance != null
    ? BoardDimensionsManager.Instance.GetDimensions()
    : new Vector3Int(4, 4, 4);

// Bad (can throw NullReferenceException)
Vector3Int dimensions = BoardDimensionsManager.Instance.GetDimensions();
```

### 2. Provide Fallbacks
All manager calls should have fallback logic for 2-player mode:
```csharp
// Get opponents with fallback
List<PieceColor> opponents = PlayerManager.Instance?.GetOpponents(playerColor)
    ?? new List<PieceColor> { GetBinaryOpponent(playerColor) };
```

### 3. Validate Configuration
```csharp
void ValidateGameSetup()
{
    // Check managers exist
    if (BoardDimensionsManager.Instance == null)
    {
        Debug.LogError("BoardDimensionsManager not initialized!");
    }

    if (PlayerManager.Instance == null)
    {
        Debug.LogError("PlayerManager not initialized!");
    }

    if (PieceTrayManager.Instance == null)
    {
        Debug.LogWarning("PieceTrayManager not initialized!");
    }

    // Check configuration matches
    int playerCount = PlayerManager.Instance?.GetPlayerCount() ?? 2;
    int boardSize = BoardDimensionsManager.Instance?.GetDimensions().x ?? 4;

    Debug.Log($"Configuration: {playerCount} players on {boardSize}x{boardSize}x{boardSize} board");
}
```

### 4. Clean Up on Scene Changes
```csharp
void OnDestroy()
{
    // Managers are DontDestroyOnLoad, but you might want to reset them
    // between games if needed
}
```

---

## Troubleshooting

### Issue: Board is still 4x4x4 despite configuration
**Solution**: Ensure `BoardDimensionsManager.SetDimensions()` is called BEFORE `ChessBoard.InitializeBoard()`

### Issue: Turn cycling doesn't include all players
**Solution**: Verify `PlayerManager.SetPlayers()` was called with all active player colors

### Issue: Tray access returns null
**Solution**: Ensure each `PieceTray` calls `RegisterTray()` in its `Awake()` method

### Issue: NullReferenceException from manager
**Solution**: Always check `Manager.Instance != null` before accessing, include fallback logic

### Issue: AI performs poorly on large boards
**Solution**: Reduce minimax depth for 6x6x6 and 8x8x8 boards (use depth 2-3 instead of 4)

---

## Performance Tips

### For Larger Boards (6x6x6, 8x8x8):

1. **Reduce AI Depth**:
```csharp
int depth = boardSize switch
{
    4 => 4,  // Standard depth for 4x4x4
    6 => 3,  // Reduced for 6x6x6
    8 => 2,  // Minimal for 8x8x8
    _ => 3
};
```

2. **Enable Early Exit in Searches**:
```csharp
// Already implemented in HasLegalMoves()
if (legalMoves.Count > 0)
{
    return true; // Exit as soon as we find one legal move
}
```

3. **Use Cached Results**:
```csharp
// MinimaxEngine already caches move generation and evaluations
// Avoid redundant board scans where possible
```

---

## Testing Your Configuration

```csharp
public void TestConfiguration()
{
    // Test 1: Verify board dimensions
    Vector3Int dims = BoardDimensionsManager.Instance.GetDimensions();
    Debug.Log($"Board Dimensions: {dims}");

    // Test 2: Verify players
    List<PieceColor> players = PlayerManager.Instance.GetAllPlayers();
    Debug.Log($"Active Players: {string.Join(", ", players)}");

    // Test 3: Verify trays
    foreach (PieceColor color in players)
    {
        PieceTray tray = PieceTrayManager.Instance.GetTray(color);
        Debug.Log($"{color} Tray: {(tray != null ? "Registered" : "Missing")}");
    }

    // Test 4: Verify turn cycling
    PieceColor current = players[0];
    for (int i = 0; i < players.Count; i++)
    {
        PieceColor next = PlayerManager.Instance.GetNextPlayer(current);
        Debug.Log($"Turn cycle: {current} -> {next}");
        current = next;
    }
}
```

---

## API Reference Summary

### BoardDimensionsManager
- `SetDimensions(Vector3Int)` - Set board size
- `GetDimensions()` - Get current size
- `GetTotalSquares()` - Calculate total positions
- `IsValidDimension(int)` - Check if size is supported

### PlayerManager
- `SetPlayers(List<PieceColor>)` - Configure players
- `GetAllPlayers()` - Get player list
- `GetOpponents(PieceColor)` - Get opponents
- `GetNextPlayer(PieceColor)` - Next in turn order
- `GetPlayerCount()` - Count active players

### PieceTrayManager
- `RegisterTray(PieceColor, PieceTray)` - Register tray
- `UnregisterTray(PieceColor)` - Remove tray
- `GetTray(PieceColor)` - Access tray
- `HasTray(PieceColor)` - Check registration
- `GetAllTrays()` - Get all trays
- `AllTraysRegistered()` - Validation check

---

## Additional Resources

- **Test Plan**: See `SCALABILITY_TEST_PLAN.md`
- **Implementation Details**: See `IMPLEMENTATION_SUMMARY.md`
- **Code Documentation**: All managers include XML documentation comments

---

*For questions or issues, consult the implementation summary or test plan documents.*
