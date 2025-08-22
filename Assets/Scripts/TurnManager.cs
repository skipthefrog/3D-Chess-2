using UnityEngine;

/// <summary>
/// Manages turn-based gameplay to ensure players alternate turns properly.
/// Prevents any color from taking consecutive moves during the Playing phase.
/// </summary>
public class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    public PieceColor currentPlayer = PieceColor.White;
    public bool enableTurnValidation = true;
    
    [Header("Player Types")]
    public PlayerType whitePlayerType = PlayerType.Human;
    public PlayerType blackPlayerType = PlayerType.Human;
    
    [Header("Check State Tracking")]
    public bool whiteInCheck = false;
    public bool blackInCheck = false;
    
    [Header("Opening Move Protection")]
    private bool isFirstTurnAfterPlacement = true;
    private int totalMovesMade = 0;
    
    [Header("Initial Check States")]
    private bool initialWhiteInCheck = false;
    private bool initialBlackInCheck = false;
    private bool bothPlayersStartInCheck = false;
    
    [Header("Events")]
    public System.Action<PieceColor> OnTurnChanged;
    public System.Action<PieceColor, PieceColor> OnInvalidTurnAttempt; // attempted player, current player
    public System.Action<PieceColor> OnPlayerInCheck;
    public System.Action<PieceColor> OnCheckResolved;
    
    public static TurnManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"TurnManager: Instance created with starting player {currentPlayer}");
        }
        else
        {
            Debug.LogWarning("TurnManager: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Subscribe to game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        }
        
        // Subscribe to check detection events
        if (CheckDetectionManager.Instance != null)
        {
            CheckDetectionManager.Instance.OnKingInCheck += OnKingInCheckDetected;
            CheckDetectionManager.Instance.OnCheckResolved += OnCheckResolvedDetected;
        }
    }
    
    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
        
        if (CheckDetectionManager.Instance != null)
        {
            CheckDetectionManager.Instance.OnKingInCheck -= OnKingInCheckDetected;
            CheckDetectionManager.Instance.OnCheckResolved -= OnCheckResolvedDetected;
        }
    }
    
    /// <summary>
    /// Handle game state changes - initialize gameplay settings when entering Playing phase
    /// Turn order is now determined by initial check states rather than defaulting to White
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.Playing)
        {
            Debug.Log($"TurnManager: Game started - current player is {currentPlayer} (determined by initial check states)");
            // Don't override currentPlayer here - it's already set by DetermineOpeningTurnOrder()
            isFirstTurnAfterPlacement = true;
            totalMovesMade = 0;
            Debug.Log("TurnManager: Normal gameplay ready - all moves allowed from start");
            
            // FIXED: Trigger initial AI move if starting player is AI
            if (IsCurrentPlayerAI())
            {
                Debug.Log($"TurnManager: Starting player {currentPlayer} is AI - triggering initial move");
                TriggerAIMove();
            }
        }
        else if (newState == GameState.PiecePlacement)
        {
            Debug.Log("TurnManager: Returned to placement phase");
        }
        else if (newState == GameState.GameOver)
        {
            Debug.Log("TurnManager: Game over - turn system disabled");
        }
    }
    
    /// <summary>
    /// Check if it's the specified player's turn to move
    /// </summary>
    public bool IsPlayerTurn(PieceColor player)
    {
        if (!enableTurnValidation)
        {
            Debug.Log($"TurnManager: Turn validation disabled - allowing {player} to move");
            return true;
        }
        
        bool isValidTurn = currentPlayer == player;
        
        if (!isValidTurn)
        {
            Debug.LogWarning($"TurnManager: INVALID TURN - {player} tried to move but it's {currentPlayer}'s turn");
            OnInvalidTurnAttempt?.Invoke(player, currentPlayer);
        }
        else
        {
            Debug.Log($"TurnManager: Valid turn confirmed for {player}");
        }
        
        return isValidTurn;
    }
    
    /// <summary>
    /// Check if a specific piece can be moved (belongs to current player)
    /// </summary>
    public bool CanMovePiece(ChessPiece piece)
    {
        if (piece == null)
        {
            Debug.LogError("TurnManager: Cannot validate null piece");
            return false;
        }
        
        // During placement phase, use placement manager rules
        if (GameStateManager.Instance != null && GameStateManager.Instance.CanPlacePieces())
        {
            Debug.Log("TurnManager: Placement phase active - deferring to PlacementManager");
            return true; // Let PlacementManager handle placement rules
        }
        
        // During playing phase, enforce turn-based rules
        if (GameStateManager.Instance != null && GameStateManager.Instance.CanMovePieces())
        {
            bool canMove = IsPlayerTurn(piece.pieceColor);
            if (!canMove)
            {
                Debug.LogWarning($"TurnManager: Cannot move {piece.pieceColor} {piece.pieceType} - not their turn (current: {currentPlayer})");
            }
            else
            {
                Debug.Log($"TurnManager: {piece.pieceColor} {piece.pieceType} can move - correct turn");
            }
            return canMove;
        }
        
        // Invalid game state
        Debug.LogWarning($"TurnManager: Cannot move pieces in current game state: {GameStateManager.Instance?.currentState}");
        return false;
    }
    
    /// <summary>
    /// Switch to the next player's turn after a successful move
    /// </summary>
    public void NextTurn()
    {
        // Only switch turns during active gameplay
        if (GameStateManager.Instance == null || !GameStateManager.Instance.CanMovePieces())
        {
            Debug.LogWarning($"TurnManager: Cannot switch turns - not in playing state ({GameStateManager.Instance?.currentState})");
            return;
        }
        
        PieceColor previousPlayer = currentPlayer;
        currentPlayer = (currentPlayer == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        
        // Track moves for opening protection
        totalMovesMade++;
        if (isFirstTurnAfterPlacement && totalMovesMade >= 2)
        {
            isFirstTurnAfterPlacement = false;
            Debug.Log("TurnManager: Opening move protection disabled - king captures now allowed");
        }
        
        Debug.Log($"🔄 TurnManager: Turn switched from {previousPlayer} to {currentPlayer}");
        
        OnTurnChanged?.Invoke(currentPlayer);
        
        // If the new current player is AI, trigger their move
        if (IsCurrentPlayerAI())
        {
            TriggerAIMove();
        }
    }
    
    /// <summary>
    /// Manually set the current player (for testing or game setup)
    /// </summary>
    public void SetCurrentPlayer(PieceColor player)
    {
        PieceColor previousPlayer = currentPlayer;
        currentPlayer = player;
        
        Debug.Log($"TurnManager: Current player manually set from {previousPlayer} to {currentPlayer}");
        
        OnTurnChanged?.Invoke(currentPlayer);
        
        // If the new current player is AI, trigger their move
        if (IsCurrentPlayerAI())
        {
            TriggerAIMove();
        }
    }
    
    /// <summary>
    /// Get the current player
    /// </summary>
    public PieceColor GetCurrentPlayer()
    {
        return currentPlayer;
    }
    
    /// <summary>
    /// Get turn validation status for debugging
    /// </summary>
    public string GetTurnInfo()
    {
        if (GameStateManager.Instance == null)
        {
            return "TurnManager: GameStateManager not available";
        }
        
        string gameState = GameStateManager.Instance.currentState.ToString();
        string validation = enableTurnValidation ? "ON" : "OFF";
        
        return $"Turn: {currentPlayer}, State: {gameState}, Validation: {validation}";
    }
    
    /// <summary>
    /// Enable/disable turn validation (useful for testing)
    /// </summary>
    public void SetTurnValidation(bool enabled)
    {
        enableTurnValidation = enabled;
        Debug.Log($"TurnManager: Turn validation {(enabled ? "ENABLED" : "DISABLED")}");
    }
    
    /// <summary>
    /// Handle king in check event from CheckDetectionManager
    /// </summary>
    private void OnKingInCheckDetected(PieceColor kingColor)
    {
        if (kingColor == PieceColor.White)
        {
            whiteInCheck = true;
        }
        else
        {
            blackInCheck = true;
        }
        
        Debug.Log($"TurnManager: {kingColor} king is in CHECK - player must resolve check before ending turn");
        OnPlayerInCheck?.Invoke(kingColor);
    }
    
    /// <summary>
    /// Handle check resolved event from CheckDetectionManager
    /// </summary>
    private void OnCheckResolvedDetected(PieceColor kingColor)
    {
        if (kingColor == PieceColor.White)
        {
            whiteInCheck = false;
        }
        else
        {
            blackInCheck = false;
        }
        
        Debug.Log($"TurnManager: {kingColor} check resolved");
        OnCheckResolved?.Invoke(kingColor);
    }
    
    /// <summary>
    /// Check if the current player is in check
    /// </summary>
    public bool IsCurrentPlayerInCheck()
    {
        return (currentPlayer == PieceColor.White && whiteInCheck) || 
               (currentPlayer == PieceColor.Black && blackInCheck);
    }
    
    /// <summary>
    /// Check if a specific player is in check
    /// </summary>
    public bool IsPlayerInCheck(PieceColor player)
    {
        return (player == PieceColor.White && whiteInCheck) || 
               (player == PieceColor.Black && blackInCheck);
    }
    
    /// <summary>
    /// Get check state information for debugging
    /// </summary>
    public string GetCheckStateInfo()
    {
        return $"Check State: White={whiteInCheck}, Black={blackInCheck}, Current={currentPlayer} in check={IsCurrentPlayerInCheck()}";
    }
    
    /// <summary>
    /// Check if this is the first turn after piece placement
    /// </summary>
    public bool IsFirstTurnAfterPlacement()
    {
        return isFirstTurnAfterPlacement;
    }
    
    /// <summary>
    /// Check if king captures should be blocked (opening move protection)
    /// Only blocks king captures when both players start in check to prevent instant checkmate
    /// </summary>
    public bool ShouldBlockKingCapture()
    {
        // Block king captures if both players started in check (to prevent instant checkmate)
        if (bothPlayersStartInCheck && isFirstTurnAfterPlacement)
        {
            Debug.Log("🔄 TurnManager: Blocking king capture - both players started in check");
            return true;
        }
        
        // FIXED: Don't block king captures during normal opening when no players are in check
        // Standard gameplay should allow all legal moves from the start
        
        return false;
    }
    
    /// <summary>
    /// Get total moves made since game started
    /// </summary>
    public int GetTotalMovesMade()
    {
        return totalMovesMade;
    }
    
    /// <summary>
    /// Set the player type for a specific color
    /// </summary>
    public void SetPlayerType(PieceColor color, PlayerType playerType)
    {
        if (color == PieceColor.White)
        {
            whitePlayerType = playerType;
        }
        else
        {
            blackPlayerType = playerType;
        }
        
        Debug.Log($"TurnManager: {color} player type set to {playerType}");
    }
    
    /// <summary>
    /// Get the player type for a specific color
    /// </summary>
    public PlayerType GetPlayerType(PieceColor color)
    {
        return color == PieceColor.White ? whitePlayerType : blackPlayerType;
    }
    
    /// <summary>
    /// Check if the current player is an AI
    /// </summary>
    public bool IsCurrentPlayerAI()
    {
        return GetPlayerType(currentPlayer) == PlayerType.Computer;
    }
    
    /// <summary>
    /// Check if a specific player is an AI
    /// </summary>
    public bool IsPlayerAI(PieceColor color)
    {
        return GetPlayerType(color) == PlayerType.Computer;
    }
    
    /// <summary>
    /// Set up a game mode (Human vs Human, Human vs AI, etc.)
    /// </summary>
    public void SetGameMode(PlayerType whiteType, PlayerType blackType)
    {
        whitePlayerType = whiteType;
        blackPlayerType = blackType;
        
        string gameMode = $"{whiteType} vs {blackType}";
        Debug.Log($"TurnManager: Game mode set to {gameMode}");
        
        // Only trigger AI move if we're in playing state and it's an AI's turn
        if (GameStateManager.Instance != null && 
            GameStateManager.Instance.CanMovePieces() && 
            IsCurrentPlayerAI())
        {
            TriggerAIMove();
        }
    }
    
    /// <summary>
    /// Get a description of the current game mode
    /// </summary>
    public string GetGameModeDescription()
    {
        return $"{whitePlayerType} vs {blackPlayerType}";
    }
    
    /// <summary>
    /// Check if this is a Human vs AI game
    /// </summary>
    public bool IsHumanVsAI()
    {
        return (whitePlayerType == PlayerType.Human && blackPlayerType == PlayerType.Computer) ||
               (whitePlayerType == PlayerType.Computer && blackPlayerType == PlayerType.Human);
    }
    
    /// <summary>
    /// Check if this is an AI vs AI game
    /// </summary>
    public bool IsAIVsAI()
    {
        return whitePlayerType == PlayerType.Computer && blackPlayerType == PlayerType.Computer;
    }
    
    /// <summary>
    /// Trigger an AI move for the current player (if they are AI)
    /// </summary>
    private void TriggerAIMove()
    {
        if (!IsCurrentPlayerAI())
        {
            Debug.LogWarning("TurnManager: Attempted to trigger AI move for human player");
            return;
        }
        
        Debug.Log($"TurnManager: Triggering AI move for {currentPlayer}");
        
        // Request move from AI player
        if (AIPlayer.Instance != null)
        {
            AIPlayer.Instance.RequestMove(currentPlayer);
        }
        else
        {
            Debug.LogError("TurnManager: AIPlayer.Instance is null, cannot trigger AI move");
        }
    }
    
    /// <summary>
    /// Trigger an AI placement for the specified player (if they are AI)
    /// </summary>
    public void TriggerAIPlacement(PieceColor playerColor)
    {
        if (!IsPlayerAI(playerColor))
        {
            Debug.LogWarning($"TurnManager: Attempted to trigger AI placement for human player {playerColor}");
            return;
        }
        
        Debug.Log($"TurnManager: Triggering AI placement for {playerColor}");
        
        // Request placement from AI player
        if (AIPlayer.Instance != null)
        {
            AIPlayer.Instance.RequestPlacement(playerColor);
        }
        else
        {
            Debug.LogError("TurnManager: AIPlayer.Instance is null, cannot trigger AI placement");
        }
    }
    
    /// <summary>
    /// Set the initial check states from post-placement evaluation
    /// Called by PlacementManager after all pieces are placed
    /// </summary>
    public void SetInitialCheckStates(bool whiteInCheck, bool blackInCheck)
    {
        initialWhiteInCheck = whiteInCheck;
        initialBlackInCheck = blackInCheck;
        bothPlayersStartInCheck = whiteInCheck && blackInCheck;
        
        Debug.Log($"🔄 TurnManager: Initial check states set - White: {whiteInCheck}, Black: {blackInCheck}, Both: {bothPlayersStartInCheck}");
        
        // Determine opening turn order based on check states
        DetermineOpeningTurnOrder();
    }
    
    /// <summary>
    /// Determine who should take the first turn based on initial check states
    /// </summary>
    private void DetermineOpeningTurnOrder()
    {
        PieceColor startingPlayer;
        string reason;
        
        if (!initialWhiteInCheck && !initialBlackInCheck)
        {
            // Neither in check - standard opening (White starts)
            startingPlayer = PieceColor.White;
            reason = "standard opening (neither player in check)";
        }
        else if (initialWhiteInCheck && !initialBlackInCheck)
        {
            // White only in check - White must get out of check first
            startingPlayer = PieceColor.White;
            reason = "White in check and must resolve it";
        }
        else if (!initialWhiteInCheck && initialBlackInCheck)
        {
            // Black only in check - Black gets first turn to resolve check
            startingPlayer = PieceColor.Black;
            reason = "Black in check and gets first turn to resolve it";
        }
        else
        {
            // Both in check - White starts but cannot capture king
            startingPlayer = PieceColor.White;
            reason = "both players in check (White starts but cannot capture king)";
        }
        
        Debug.Log($"🔄 TurnManager: Opening turn order determined - {startingPlayer} starts first ({reason})");
        
        // Set the current player without triggering normal turn events
        currentPlayer = startingPlayer;
        
        // Log special conditions
        if (bothPlayersStartInCheck)
        {
            Debug.Log("🔄 ⚠️ TurnManager: SPECIAL CONDITION - Both players start in check, king capture protection active");
        }
        
        if (initialWhiteInCheck)
        {
            Debug.Log("🔄 ⚠️ TurnManager: White must resolve check before ending turn");
        }
        
        if (initialBlackInCheck)
        {
            Debug.Log("🔄 ⚠️ TurnManager: Black must resolve check before ending turn");
        }
    }
}