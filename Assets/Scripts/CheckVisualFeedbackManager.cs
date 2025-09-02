using UnityEngine;

/// <summary>
/// Manages visual feedback for check conditions in 3D chess.
/// Coordinates with CheckDetectionManager to show/hide check indicators on kings.
/// Handles check warnings, attack line visualization, and UI notifications.
/// </summary>
public class CheckVisualFeedbackManager : MonoBehaviour
{
    [Header("Visual Feedback Settings")]
    public bool enableVisualFeedback = true;
    public bool showCheckIndicators = true;
    public bool showCheckMessages = true;
    
    [Header("UI Integration")]
    public bool enableUINotifications = true;
    
    public static CheckVisualFeedbackManager Instance { get; private set; }
    
    // Track current check states
    private bool whiteKingInCheck = false;
    private bool blackKingInCheck = false;
    
    // Public properties for state validation
    public bool WhiteKingInCheck => whiteKingInCheck;
    public bool BlackKingInCheck => blackKingInCheck;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("CheckVisualFeedbackManager: Instance created");
        }
        else
        {
            Debug.LogWarning("CheckVisualFeedbackManager: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        Debug.Log("CheckVisualFeedbackManager.Start: ENTRY");
        
        // Subscribe to check detection events
        if (CheckDetectionManager.Instance != null)
        {
            Debug.Log("CheckVisualFeedbackManager.Start: CheckDetectionManager found, subscribing to events");
            CheckDetectionManager.Instance.OnKingInCheck += OnKingInCheck;
            CheckDetectionManager.Instance.OnCheckResolved += OnCheckResolved;
        }
        else
        {
            Debug.LogWarning("CheckVisualFeedbackManager.Start: CheckDetectionManager.Instance is NULL - cannot subscribe to events");
        }
        
        // Subscribe to game end events
        if (GameEndDetectionManager.Instance != null)
        {
            Debug.Log("CheckVisualFeedbackManager.Start: GameEndDetectionManager found, subscribing to events");
            GameEndDetectionManager.Instance.OnCheckmate += OnCheckmate;
            GameEndDetectionManager.Instance.OnStalemate += OnStalemate;
            GameEndDetectionManager.Instance.OnGameEnd += OnGameEnd;
        }
        else
        {
            Debug.LogWarning("CheckVisualFeedbackManager.Start: GameEndDetectionManager.Instance is NULL");
        }
        
        // Subscribe to game state changes
        if (GameStateManager.Instance != null)
        {
            Debug.Log("CheckVisualFeedbackManager.Start: GameStateManager found, subscribing to events");
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        }
        else
        {
            Debug.LogWarning("CheckVisualFeedbackManager.Start: GameStateManager.Instance is NULL");
        }
        
        // Verify CheckStatusUI is available
        if (CheckStatusUI.Instance != null)
        {
            Debug.Log("CheckVisualFeedbackManager.Start: CheckStatusUI.Instance found");
        }
        else
        {
            Debug.LogWarning("CheckVisualFeedbackManager.Start: CheckStatusUI.Instance is NULL - UI notifications will not work");
        }
        
        Debug.Log("CheckVisualFeedbackManager.Start: EXIT - initialization complete");
        
        // Start a coroutine to retry initialization if needed
        StartCoroutine(DelayedInitializationCheck());
    }
    
    /// <summary>
    /// Coroutine to check and fix initialization issues after a delay
    /// </summary>
    private System.Collections.IEnumerator DelayedInitializationCheck()
    {
        // Wait 2 seconds for all components to be fully initialized
        yield return new UnityEngine.WaitForSeconds(2f);
        
        Debug.Log("CheckVisualFeedbackManager.DelayedInitializationCheck: Performing delayed check...");
        
        // Re-check CheckStatusUI
        if (CheckStatusUI.Instance == null)
        {
            Debug.LogError("CheckVisualFeedbackManager.DelayedInitializationCheck: CheckStatusUI.Instance is still NULL after delay!");
        }
        else
        {
            Debug.Log("CheckVisualFeedbackManager.DelayedInitializationCheck: CheckStatusUI.Instance is now available");
            
            // TestCheckUI() removed - was causing false check alerts during piece placement
            // The TestCheckUI() method remains available for manual debugging if needed
        }
        
        // Re-check CheckDetectionManager events
        if (CheckDetectionManager.Instance != null)
        {
            Debug.Log($"CheckVisualFeedbackManager.DelayedInitializationCheck: CheckDetectionManager event subscribers - OnKingInCheck: {CheckDetectionManager.Instance.OnKingInCheck?.GetInvocationList()?.Length ?? 0}");
        }
    }
    
    private void OnDestroy()
    {
        if (CheckDetectionManager.Instance != null)
        {
            CheckDetectionManager.Instance.OnKingInCheck -= OnKingInCheck;
            CheckDetectionManager.Instance.OnCheckResolved -= OnCheckResolved;
        }
        
        if (GameEndDetectionManager.Instance != null)
        {
            GameEndDetectionManager.Instance.OnCheckmate -= OnCheckmate;
            GameEndDetectionManager.Instance.OnStalemate -= OnStalemate;
            GameEndDetectionManager.Instance.OnGameEnd -= OnGameEnd;
        }
        
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }
    
    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.Playing)
        {
            Debug.Log("CheckVisualFeedbackManager: Game started - enabling visual feedback");
            enableVisualFeedback = true;
        }
        else if (newState == GameState.GameOver)
        {
            Debug.Log("CheckVisualFeedbackManager: Game ended - clearing all visual feedback");
            ClearAllCheckIndicators();
        }
    }
    
    /// <summary>
    /// Handle king in check event - show visual indicators
    /// </summary>
    private void OnKingInCheck(PieceColor kingColor)
    {
        Debug.Log($"🔴 CheckVisualFeedbackManager.OnKingInCheck: ENTRY - kingColor={kingColor}");
        Debug.Log($"🔴 CheckVisualFeedbackManager.OnKingInCheck: enableVisualFeedback={enableVisualFeedback}");
        Debug.Log($"🔴 CheckVisualFeedbackManager.OnKingInCheck: ThreatIndicatorManager.Instance={(ThreatIndicatorManager.Instance != null ? "EXISTS" : "NULL")}");
        
        if (!enableVisualFeedback) 
        {
            Debug.Log("🔴 CheckVisualFeedbackManager.OnKingInCheck: Visual feedback disabled, exiting");
            return;
        }
        
        // Update tracking
        if (kingColor == PieceColor.White)
        {
            whiteKingInCheck = true;
        }
        else
        {
            blackKingInCheck = true;
        }
        
        Debug.Log($"CheckVisualFeedbackManager.OnKingInCheck: Updated check state - White={whiteKingInCheck}, Black={blackKingInCheck}");
        
        // Show visual feedback on king (red pulsing indicator)
        ShowCheckIndicator(kingColor);
        
        // Show threat indicators on attacking pieces (orange indicators)
        if (ThreatIndicatorManager.Instance != null)
        {
            Debug.Log($"🔴 🟠 CheckVisualFeedbackManager.OnKingInCheck: Calling ShowThreatIndicators for {kingColor} king");
            try
            {
                ThreatIndicatorManager.Instance.ShowThreatIndicators(kingColor);
                Debug.Log($"🔴 🟠 CheckVisualFeedbackManager.OnKingInCheck: ShowThreatIndicators call completed successfully for {kingColor}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"🔴 ❌ CheckVisualFeedbackManager.OnKingInCheck: Exception calling ShowThreatIndicators for {kingColor}: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"🔴 ❌ CheckVisualFeedbackManager.OnKingInCheck: ThreatIndicatorManager.Instance is NULL - cannot show threat indicators for {kingColor}");
        }
        
        // CheckStatusUI now handles events directly through event subscription
        Debug.Log($"CheckVisualFeedbackManager.OnKingInCheck: CheckStatusUI will handle this via event subscription");
        
        if (showCheckMessages)
        {
            Debug.Log($"🚨 CHECK! {kingColor} king is under attack!");
        }
        
        Debug.Log($"CheckVisualFeedbackManager.OnKingInCheck: EXIT");
    }
    
    /// <summary>
    /// Handle check resolved event - hide visual indicators
    /// </summary>
    private void OnCheckResolved(PieceColor kingColor)
    {
        Debug.Log($"🟢 CheckVisualFeedbackManager.OnCheckResolved: ENTRY - kingColor={kingColor}");
        Debug.Log($"🟢 CheckVisualFeedbackManager.OnCheckResolved: enableVisualFeedback={enableVisualFeedback}");
        Debug.Log($"🟢 CheckVisualFeedbackManager.OnCheckResolved: ThreatIndicatorManager.Instance={(ThreatIndicatorManager.Instance != null ? "EXISTS" : "NULL")}");
        
        if (!enableVisualFeedback) 
        {
            Debug.Log("🟢 CheckVisualFeedbackManager.OnCheckResolved: Visual feedback disabled, exiting");
            return;
        }
        
        // Update tracking
        if (kingColor == PieceColor.White)
        {
            whiteKingInCheck = false;
        }
        else
        {
            blackKingInCheck = false;
        }
        
        Debug.Log($"CheckVisualFeedbackManager.OnCheckResolved: Updated check state - White={whiteKingInCheck}, Black={blackKingInCheck}");
        
        // Hide visual feedback on king
        Debug.Log($"CheckVisualFeedbackManager.OnCheckResolved: Calling HideCheckIndicator({kingColor})");
        HideCheckIndicator(kingColor);
        
        // Clear threat indicators only for pieces no longer attacking this king (selective clearing)
        if (ThreatIndicatorManager.Instance != null)
        {
            Debug.Log($"🟢 🟠 CheckVisualFeedbackManager.OnCheckResolved: Calling ClearThreatIndicatorsForResolvedKing for {kingColor} resolution");
            try
            {
                ThreatIndicatorManager.Instance.ClearThreatIndicatorsForResolvedKing(kingColor);
                Debug.Log($"🟢 🟠 CheckVisualFeedbackManager.OnCheckResolved: ClearThreatIndicatorsForResolvedKing call completed successfully for {kingColor}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"🟢 ❌ CheckVisualFeedbackManager.OnCheckResolved: Exception calling ClearThreatIndicatorsForResolvedKing for {kingColor}: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"🟢 ❌ CheckVisualFeedbackManager.OnCheckResolved: ThreatIndicatorManager.Instance is NULL - cannot clear threat indicators for {kingColor}");
        }
        
        // CheckStatusUI now handles check resolution events directly through event subscription
        Debug.Log("CheckVisualFeedbackManager.OnCheckResolved: CheckStatusUI will handle this via event subscription");
        
        if (showCheckMessages)
        {
            Debug.Log($"✅ {kingColor} check resolved");
        }
        
        Debug.Log($"CheckVisualFeedbackManager.OnCheckResolved: EXIT");
    }
    
    /// <summary>
    /// Handle checkmate event - show game end feedback
    /// </summary>
    private void OnCheckmate(PieceColor checkmatedPlayer)
    {
        PieceColor winner = (checkmatedPlayer == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        
        // CheckStatusUI handles checkmate via event subscription
        
        if (showCheckMessages)
        {
            Debug.Log($"🏁 CHECKMATE! {winner} wins! {checkmatedPlayer} is checkmated.");
        }
        
        // Keep check indicator visible to show the final check position
        ShowCheckIndicator(checkmatedPlayer);
    }
    
    /// <summary>
    /// Handle stalemate event - show draw feedback
    /// </summary>
    private void OnStalemate(PieceColor stalematedPlayer)
    {
        // CheckStatusUI handles stalemate via event subscription
        
        if (showCheckMessages)
        {
            Debug.Log($"🏁 STALEMATE! Game ends in a draw. {stalematedPlayer} has no legal moves but is not in check.");
        }
    }
    
    /// <summary>
    /// Handle general game end event
    /// </summary>
    private void OnGameEnd(string reason)
    {
        Debug.Log($"🏁 GAME OVER: {reason}");
        
        // Could integrate with UI system here to show game end screen
        if (enableUINotifications)
        {
            ShowGameEndNotification(reason);
        }
    }
    
    /// <summary>
    /// Show check indicator on the specified king
    /// </summary>
    public void ShowCheckIndicator(PieceColor kingColor)
    {
        if (!showCheckIndicators || ChessBoard.Instance == null) return;
        
        // Find the king piece
        ChessPiece king = FindKing(kingColor);
        if (king != null)
        {
            king.ShowCheckIndicator();
            Debug.Log($"CheckVisualFeedbackManager: Showing check indicator for {kingColor} king");
        }
        else
        {
            Debug.LogWarning($"CheckVisualFeedbackManager: Could not find {kingColor} king to show check indicator");
        }
    }
    
    /// <summary>
    /// Hide check indicator on the specified king
    /// </summary>
    private void HideCheckIndicator(PieceColor kingColor)
    {
        Debug.Log($"CheckVisualFeedbackManager.HideCheckIndicator: ENTRY - kingColor={kingColor}");
        Debug.Log($"CheckVisualFeedbackManager.HideCheckIndicator: showCheckIndicators={showCheckIndicators}, ChessBoard.Instance={(ChessBoard.Instance != null ? "EXISTS" : "NULL")}");
        
        if (!showCheckIndicators || ChessBoard.Instance == null) 
        {
            Debug.Log("CheckVisualFeedbackManager.HideCheckIndicator: Conditions not met, returning");
            return;
        }
        
        // Find the king piece
        ChessPiece king = FindKing(kingColor);
        Debug.Log($"CheckVisualFeedbackManager.HideCheckIndicator: Found king = {(king != null ? king.name : "NULL")}");
        
        if (king != null)
        {
            Debug.Log($"CheckVisualFeedbackManager.HideCheckIndicator: Calling king.HideCheckIndicator() on {king.name}");
            king.HideCheckIndicator();
            Debug.Log($"CheckVisualFeedbackManager: Successfully hid check indicator for {kingColor} king");
        }
        else
        {
            Debug.LogWarning($"CheckVisualFeedbackManager: Could not find {kingColor} king to hide check indicator");
        }
        
        Debug.Log($"CheckVisualFeedbackManager.HideCheckIndicator: EXIT");
    }
    
    /// <summary>
    /// Clear all check indicators from both kings and threat indicators
    /// </summary>
    public void ClearAllCheckIndicators()
    {
        HideCheckIndicator(PieceColor.White);
        HideCheckIndicator(PieceColor.Black);
        
        // Clear threat indicators
        if (ThreatIndicatorManager.Instance != null)
        {
            ThreatIndicatorManager.Instance.ClearThreatIndicators();
        }
        
        whiteKingInCheck = false;
        blackKingInCheck = false;
        
        Debug.Log("CheckVisualFeedbackManager: Cleared all check indicators and threat indicators");
    }
    
    /// <summary>
    /// Find a king piece of the specified color on the board
    /// </summary>
    private ChessPiece FindKing(PieceColor kingColor)
    {
        if (ChessBoard.Instance == null) return null;
        
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    BoardPosition pos = new BoardPosition(x, y, z);
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);
                    
                    if (piece != null && 
                        piece.pieceType == ChessPieceType.King && 
                        piece.pieceColor == kingColor)
                    {
                        return piece;
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Show game end notification (placeholder for UI integration)
    /// </summary>
    private void ShowGameEndNotification(string reason)
    {
        // This could be enhanced to integrate with a UI system
        // For now, just log the message
        Debug.Log($"CheckVisualFeedbackManager: Game End Notification - {reason}");
        
        // Future enhancement: Show UI popup, play sound effects, etc.
    }
    
    /// <summary>
    /// Manually refresh all check indicators based on current state
    /// </summary>
    public void RefreshCheckIndicators()
    {
        if (!enableVisualFeedback || CheckDetectionManager.Instance == null) return;
        
        // Force cache invalidation to ensure fresh state
        CheckDetectionManager.Instance.InvalidateCache();
        
        // Get current actual check states  
        bool whiteInCheck = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.White);
        bool blackInCheck = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.Black);
        
        Debug.Log($"🔄 CheckVisualFeedbackManager.RefreshCheckIndicators: Actual states - White: {whiteInCheck}, Black: {blackInCheck}");
        Debug.Log($"🔄 CheckVisualFeedbackManager.RefreshCheckIndicators: Tracked states - White: {whiteKingInCheck}, Black: {blackKingInCheck}");
        
        // Update visual feedback to match actual state
        if (whiteInCheck && !whiteKingInCheck)
        {
            // Should show white check indicator
            Debug.Log("CheckVisualFeedbackManager.RefreshCheckIndicators: White king should be in check but isn't shown - fixing");
            OnKingInCheck(PieceColor.White);
        }
        else if (!whiteInCheck && whiteKingInCheck)
        {
            // Should hide white check indicator
            Debug.Log("CheckVisualFeedbackManager.RefreshCheckIndicators: White king shown in check but shouldn't be - fixing");
            OnCheckResolved(PieceColor.White);
        }
        
        if (blackInCheck && !blackKingInCheck)
        {
            // Should show black check indicator
            Debug.Log("CheckVisualFeedbackManager.RefreshCheckIndicators: Black king should be in check but isn't shown - fixing");
            OnKingInCheck(PieceColor.Black);
        }
        else if (!blackInCheck && blackKingInCheck)
        {
            // Should hide black check indicator
            Debug.Log("CheckVisualFeedbackManager.RefreshCheckIndicators: Black king shown in check but shouldn't be - fixing");
            OnCheckResolved(PieceColor.Black);
        }
        
        Debug.Log($"CheckVisualFeedbackManager: Refresh complete - Actual: W:{whiteInCheck}/B:{blackInCheck}, Tracked: W:{whiteKingInCheck}/B:{blackKingInCheck}");
    }
    
    
    /// <summary>
    /// Get current check state for debugging
    /// </summary>
    public string GetCheckStateDebug()
    {
        return $"Visual Check State: White={whiteKingInCheck}, Black={blackKingInCheck}, Feedback Enabled={enableVisualFeedback}";
    }
}