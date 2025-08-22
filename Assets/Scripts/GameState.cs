using UnityEngine;

/// <summary>
/// Defines the different states the game can be in
/// </summary>
public enum GameState
{
    WaitingForConfiguration, // Scene loaded but waiting for game configuration from menu
    PiecePlacement,          // Players are placing pieces from trays onto the board
    Playing,                 // Normal gameplay - pieces can be moved and captured
    GameOver                 // Game has ended
}

/// <summary>
/// Manages the overall game state and transitions between phases
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Header("Game State")]
    public GameState currentState = GameState.WaitingForConfiguration;
    
    [Header("Events")]
    public System.Action<GameState> OnStateChanged;
    
    public static GameStateManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"GameStateManager: Initialized with state {currentState}");
        }
        else
        {
            Debug.LogWarning("GameStateManager: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Change the game state and notify listeners
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (currentState == newState)
        {
            Debug.LogWarning($"GameStateManager: Already in state {newState}");
            return;
        }
        
        GameState previousState = currentState;
        currentState = newState;
        
        Debug.Log($"GameStateManager: State changed from {previousState} to {newState}");
        
        // Notify listeners
        OnStateChanged?.Invoke(newState);
        
        // Handle state-specific logic
        HandleStateChange(previousState, newState);
    }
    
    /// <summary>
    /// Handle logic that needs to happen when states change
    /// </summary>
    private void HandleStateChange(GameState fromState, GameState toState)
    {
        switch (toState)
        {
            case GameState.WaitingForConfiguration:
                Debug.Log("GameStateManager: Waiting for game configuration from menu");
                break;
                
            case GameState.PiecePlacement:
                Debug.Log("GameStateManager: Entering piece placement phase");
                break;
                
            case GameState.Playing:
                Debug.Log("GameStateManager: Starting gameplay phase");
                // Turn order is now determined by TurnManager based on initial check states
                // The PlacementManager calls TurnManager.SetInitialCheckStates() which determines the starting player
                if (TurnManager.Instance != null)
                {
                    PieceColor startingPlayer = TurnManager.Instance.GetCurrentPlayer();
                    Debug.Log($"GameStateManager: Turn system ready - {startingPlayer} will start (determined by check states)");
                    
                    // Trigger turn changed event to notify UI and AI systems
                    TurnManager.Instance.OnTurnChanged?.Invoke(startingPlayer);
                }
                else
                {
                    Debug.LogError("GameStateManager: TurnManager.Instance is null - cannot initialize turn system");
                }
                break;
                
            case GameState.GameOver:
                Debug.Log("GameStateManager: Game ended");
                break;
        }
    }
    
    /// <summary>
    /// Check if pieces can be moved (only during Playing state)
    /// </summary>
    public bool CanMovePieces()
    {
        return currentState == GameState.Playing;
    }
    
    /// <summary>
    /// Check if pieces can be placed (only during PiecePlacement state)
    /// </summary>
    public bool CanPlacePieces()
    {
        return currentState == GameState.PiecePlacement;
    }
    
    /// <summary>
    /// Check if the game is active (not game over)
    /// </summary>
    public bool IsGameActive()
    {
        return currentState != GameState.GameOver;
    }
    
    /// <summary>
    /// Check if it's a specific player's turn to move (only during Playing state)
    /// </summary>
    public bool IsPlayerTurn(PieceColor player)
    {
        if (!CanMovePieces())
        {
            return false; // Not in playing state
        }
        
        return TurnManager.Instance != null && TurnManager.Instance.IsPlayerTurn(player);
    }
    
    /// <summary>
    /// Get the current player whose turn it is (only meaningful during Playing state)
    /// </summary>
    public PieceColor GetCurrentPlayer()
    {
        if (!CanMovePieces() || TurnManager.Instance == null)
        {
            return PieceColor.White; // Default fallback
        }
        
        return TurnManager.Instance.GetCurrentPlayer();
    }
    
    /// <summary>
    /// Check if a specific piece can be moved based on current game state and turn
    /// </summary>
    public bool CanMovePiece(ChessPiece piece)
    {
        if (piece == null) return false;
        
        // During placement phase, defer to PlacementManager rules
        if (CanPlacePieces()) return true;
        
        // During playing phase, check turn-based rules
        if (CanMovePieces())
        {
            return TurnManager.Instance != null && TurnManager.Instance.CanMovePiece(piece);
        }
        
        // Not in a valid state for moving pieces
        return false;
    }
    
    /// <summary>
    /// Initialize the game with configuration from the scene controller
    /// This should be called when transitioning from WaitingForConfiguration to the actual game
    /// </summary>
    public void InitializeGameWithConfiguration()
    {
        if (currentState != GameState.WaitingForConfiguration)
        {
            Debug.LogWarning($"GameStateManager: InitializeGameWithConfiguration called but current state is {currentState}, not WaitingForConfiguration");
            return;
        }
        
        Debug.Log("GameStateManager: Initializing game with configuration - transitioning to PiecePlacement");
        ChangeState(GameState.PiecePlacement);
    }
    
    /// <summary>
    /// Check if the game is waiting for configuration
    /// </summary>
    public bool IsWaitingForConfiguration()
    {
        return currentState == GameState.WaitingForConfiguration;
    }
}