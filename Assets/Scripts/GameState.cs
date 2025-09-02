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
            // SINGLETON STABILITY: Persist through scene changes
            DontDestroyOnLoad(gameObject);
            Debug.Log($"GameStateManager: Singleton created with DontDestroyOnLoad protection, state: {currentState}");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("GameStateManager: Duplicate instance detected, destroying...");
            Destroy(gameObject);
        }
        else
        {
            // This is the existing singleton, ensure it's still protected
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameStateManager: Existing singleton reaffirmed");
            
            // CRITICAL FIX: Reset state for new game scene transitions
            if (currentState == GameState.Playing || currentState == GameState.GameOver)
            {
                Debug.Log($"GameStateManager: Resetting state from {currentState} to WaitingForConfiguration for new game");
                currentState = GameState.WaitingForConfiguration;
            }
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
    /// Check if pieces can be moved (only during Playing state and no animations in progress)
    /// </summary>
    public bool CanMovePieces()
    {
        // Basic state check
        if (currentState != GameState.Playing)
            return false;
            
        // Check if any animations are in progress (chaos or piece animations)
        if (IsAnyAnimationInProgress())
            return false;
            
        return true;
    }
    
    /// <summary>
    /// Check if pieces can be moved with immediate, uncached animation check
    /// Use this when you need real-time feedback after animation completion
    /// </summary>
    public bool CanMovePiecesImmediate()
    {
        // Basic state check
        if (currentState != GameState.Playing)
            return false;
            
        // Force immediate check bypassing cache
        bool chaosInProgress = ChaosRotationManager.Instance != null && 
                              ChaosRotationManager.Instance.IsChaosAnimationInProgress();
        bool piecesAnimating = TurnManager.Instance != null && 
                              TurnManager.Instance.AnyPiecesStillAnimating();
        
        if (chaosInProgress || piecesAnimating)
        {
            Debug.Log($"🎬 GameStateManager.CanMovePiecesImmediate: Animations in progress - Chaos: {chaosInProgress}, Pieces: {piecesAnimating}");
            return false;
        }
            
        return true;
    }
    
    /// <summary>
    /// Check if pieces can be placed (only during PiecePlacement state)
    /// </summary>
    public bool CanPlacePieces()
    {
        return currentState == GameState.PiecePlacement;
    }
    
    // Cache animation state to prevent excessive polling
    private bool lastAnimationState = false;
    private float lastAnimationCheckTime = 0f;
    private const float ANIMATION_CHECK_INTERVAL = 0.1f; // Check at most every 0.1 seconds
    
    /// <summary>
    /// Check if any animations are currently in progress (piece or chaos animations)
    /// Cached to prevent excessive polling that can cause infinite loops
    /// </summary>
    public bool IsAnyAnimationInProgress()
    {
        float currentTime = Time.time;
        
        // Rate limiting: only check animation state periodically
        if (currentTime - lastAnimationCheckTime < ANIMATION_CHECK_INTERVAL)
        {
            return lastAnimationState;
        }
        
        lastAnimationCheckTime = currentTime;
        
        // Check for chaos animations
        bool chaosInProgress = ChaosRotationManager.Instance != null && 
                              ChaosRotationManager.Instance.IsChaosAnimationInProgress();
        
        // Check for piece movement animations
        bool piecesAnimating = TurnManager.Instance != null && 
                              TurnManager.Instance.AnyPiecesStillAnimating();
        
        bool anyAnimationInProgress = chaosInProgress || piecesAnimating;
        
        // Log animation state changes for debugging
        if (anyAnimationInProgress != lastAnimationState)
        {
            Debug.Log($"🎬 GameStateManager: Animation state changed - Chaos: {chaosInProgress}, Pieces: {piecesAnimating}, Overall: {anyAnimationInProgress}");
        }
        
        lastAnimationState = anyAnimationInProgress;
        return anyAnimationInProgress;
    }
    
    /// <summary>
    /// Force clear animation cache - call this when animations complete to ensure immediate state updates
    /// </summary>
    public void ClearAnimationCache()
    {
        if (lastAnimationState)
        {
            Debug.Log($"🎬 GameStateManager: Animation cache cleared - forcing immediate state update");
        }
        lastAnimationState = false;
        lastAnimationCheckTime = 0f;
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