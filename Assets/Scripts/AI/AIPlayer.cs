using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI difficulty levels that control search depth and thinking time
/// </summary>
public enum AIDifficulty
{
    Easy = 1,      // Depth 1, fast moves
    Medium = 2,    // Depth 2, moderate thinking
    Hard = 3       // Depth 3, longer thinking time
}

/// <summary>
/// Manages AI player behavior, move calculation, and difficulty settings.
/// Integrates with the existing turn management system to provide computer opponents.
/// </summary>
public class AIPlayer : MonoBehaviour
{
    [Header("AI Settings")]
    public AIDifficulty difficulty = AIDifficulty.Medium;
    public float thinkingTimeMin = 1.0f;  // Minimum thinking time for realism
    public float thinkingTimeMax = 3.0f;  // Maximum thinking time
    public bool showThinkingIndicator = true;
    
    [Header("Placement Settings")]
    public float placementThinkingTimeMin = 0.5f;  // Faster thinking for placement
    public float placementThinkingTimeMax = 2.0f;  // Shorter max time for placement
    
    [Header("Debug")]
    public bool enableDebugLogging = true;
    public bool showEvaluationScores = false;
    
    private bool isThinking = false;
    private bool isPlacing = false;  // Track if currently in placement phase
    private Coroutine currentThinkingCoroutine;
    private MinimaxEngine minimaxEngine;
    private AIPlacementEngine placementEngine;
    
    public static AIPlayer Instance { get; private set; }
    
    // Events
    public System.Action<PieceColor> OnAIThinkingStarted;
    public System.Action<PieceColor> OnAIThinkingFinished;
    public System.Action<PieceColor, BoardPosition, BoardPosition> OnAIMoveDecided;
    
    // Placement Events
    public System.Action<PieceColor> OnAIPlacementStarted;
    public System.Action<PieceColor> OnAIPlacementFinished;
    public System.Action<PieceColor, ChessPiece, BoardPosition> OnAIPlacementDecided;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            minimaxEngine = new MinimaxEngine(enableDebugLogging);
            placementEngine = new AIPlacementEngine(difficulty, enableDebugLogging);
            Debug.Log("AIPlayer: Instance created with minimax and placement engines");
        }
        else
        {
            Debug.LogWarning("AIPlayer: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Request the AI to make a move for the specified player color
    /// </summary>
    public void RequestMove(PieceColor playerColor)
    {
        if (isThinking)
        {
            Debug.LogWarning($"AIPlayer: Already thinking, ignoring move request for {playerColor}");
            return;
        }
        
        // Validate game state
        if (GameStateManager.Instance == null || !GameStateManager.Instance.CanMovePieces())
        {
            Debug.LogError("AIPlayer: Cannot make move - not in playing state");
            return;
        }
        
        // Validate it's the AI's turn
        if (TurnManager.Instance == null || TurnManager.Instance.GetCurrentPlayer() != playerColor)
        {
            Debug.LogError($"AIPlayer: Cannot make move - not {playerColor}'s turn");
            return;
        }
        
        // Validate this player is set to AI
        if (!TurnManager.Instance.IsPlayerAI(playerColor))
        {
            Debug.LogError($"AIPlayer: {playerColor} is not set as AI player");
            return;
        }
        
        Debug.Log($"🤖 AIPlayer: Move requested for {playerColor} at difficulty {difficulty}");
        
        // Start thinking process
        currentThinkingCoroutine = StartCoroutine(ThinkAndMoveCoroutine(playerColor));
    }
    
    /// <summary>
    /// Request the AI to place a piece for the specified player color during placement phase
    /// </summary>
    public void RequestPlacement(PieceColor playerColor)
    {
        if (isThinking || isPlacing)
        {
            Debug.LogWarning($"AIPlayer: Already thinking/placing, ignoring placement request for {playerColor}");
            return;
        }
        
        // Validate game state
        if (GameStateManager.Instance == null || !GameStateManager.Instance.CanPlacePieces())
        {
            Debug.LogError("AIPlayer: Cannot place pieces - not in placement phase");
            return;
        }
        
        // Validate this player is set to AI (check with TurnManager or PlacementManager)
        if (TurnManager.Instance != null && !TurnManager.Instance.IsPlayerAI(playerColor))
        {
            Debug.LogError($"AIPlayer: {playerColor} is not set as AI player");
            return;
        }
        
        Debug.Log($"🤖 AIPlayer: Placement requested for {playerColor} at difficulty {difficulty}");
        
        // Start placement thinking process
        currentThinkingCoroutine = StartCoroutine(ThinkAndPlaceCoroutine(playerColor));
    }
    
    /// <summary>
    /// Main AI thinking and move execution coroutine
    /// </summary>
    private IEnumerator ThinkAndMoveCoroutine(PieceColor playerColor)
    {
        isThinking = true;
        OnAIThinkingStarted?.Invoke(playerColor);
        
        if (enableDebugLogging)
            Debug.Log($"🧠 AIPlayer: {playerColor} AI started thinking...");
        
        // Calculate the best move using minimax algorithm
        float thinkingStartTime = Time.time;
        AIMove bestMove = null;
        
        // Use minimax engine to find the best move
        int searchDepth = GetSearchDepth();
        float timeLimit = thinkingTimeMax;
        
        bestMove = minimaxEngine.FindBestMove(playerColor, searchDepth, timeLimit);
        
        if (enableDebugLogging && bestMove != null)
        {
            Debug.Log($"MinimaxEngine evaluated {minimaxEngine.GetNodesEvaluated()} nodes");
            if (showEvaluationScores)
                Debug.Log($"Best move evaluation: {bestMove.evaluationScore:F2}");
        }
        
        // Ensure minimum thinking time for realism
        float thinkingTime = Time.time - thinkingStartTime;
        float remainingThinkingTime = Mathf.Max(0, thinkingTimeMin - thinkingTime);
        
        if (remainingThinkingTime > 0)
        {
            yield return new WaitForSeconds(remainingThinkingTime);
        }
        
        // Ensure we don't exceed maximum thinking time
        float totalThinkingTime = Time.time - thinkingStartTime;
        if (totalThinkingTime > thinkingTimeMax)
        {
            Debug.LogWarning($"AIPlayer: Thinking time exceeded maximum ({totalThinkingTime:F2}s > {thinkingTimeMax}s)");
        }
        
        isThinking = false;
        OnAIThinkingFinished?.Invoke(playerColor);
        
        if (bestMove != null)
        {
            if (enableDebugLogging)
                Debug.Log($"🎯 AIPlayer: {playerColor} decided to move {bestMove.piece.pieceType} from {bestMove.fromPosition} to {bestMove.toPosition}");
            
            OnAIMoveDecided?.Invoke(playerColor, bestMove.fromPosition, bestMove.toPosition);
            
            // Execute the move
            ExecuteMove(bestMove);
        }
        else
        {
            Debug.LogError($"AIPlayer: No valid moves found for {playerColor}!");
        }
        
        currentThinkingCoroutine = null;
    }
    
    /// <summary>
    /// Main AI thinking and placement execution coroutine
    /// </summary>
    private IEnumerator ThinkAndPlaceCoroutine(PieceColor playerColor)
    {
        isPlacing = true;
        OnAIPlacementStarted?.Invoke(playerColor);
        
        if (enableDebugLogging)
            Debug.Log($"🧠 AIPlayer: {playerColor} AI started thinking about placement...");
        
        // Calculate the best placement using placement engine
        float thinkingStartTime = Time.time;
        AIPlacement bestPlacement = null;
        
        // Use placement engine to find the best placement
        bestPlacement = placementEngine.FindBestPlacement(playerColor);
        
        if (enableDebugLogging && bestPlacement != null)
        {
            Debug.Log($"AIPlacementEngine found placement: {bestPlacement}");
            if (showEvaluationScores)
                Debug.Log($"Best placement evaluation: {bestPlacement.evaluationScore:F2}");
        }
        
        // Ensure minimum thinking time for realism
        float thinkingTime = Time.time - thinkingStartTime;
        float remainingThinkingTime = Mathf.Max(0, placementThinkingTimeMin - thinkingTime);
        
        if (remainingThinkingTime > 0)
        {
            yield return new WaitForSeconds(remainingThinkingTime);
        }
        
        // Ensure we don't exceed maximum thinking time
        float totalThinkingTime = Time.time - thinkingStartTime;
        if (totalThinkingTime > placementThinkingTimeMax)
        {
            Debug.LogWarning($"AIPlayer: Placement thinking time exceeded maximum ({totalThinkingTime:F2}s > {placementThinkingTimeMax}s)");
        }
        
        isPlacing = false;
        OnAIPlacementFinished?.Invoke(playerColor);
        
        if (bestPlacement != null)
        {
            if (enableDebugLogging)
                Debug.Log($"🎯 AIPlayer: {playerColor} decided to place {bestPlacement.piece.pieceType} at {bestPlacement.position}");
            
            OnAIPlacementDecided?.Invoke(playerColor, bestPlacement.piece, bestPlacement.position);
            
            // Execute the placement
            ExecutePlacement(bestPlacement);
        }
        else
        {
            Debug.LogError($"AIPlayer: No valid placements found for {playerColor}!");
        }
        
        currentThinkingCoroutine = null;
    }
    
    /// <summary>
    /// Execute the AI's chosen move
    /// </summary>
    private void ExecuteMove(AIMove move)
    {
        if (move == null || move.piece == null)
        {
            Debug.LogError("AIPlayer: Cannot execute null move");
            return;
        }
        
        Debug.Log($"🎮 AIPlayer: Executing move {move.piece.pieceType} {move.fromPosition} → {move.toPosition}");
        
        // Use the existing chess board move system
        if (ChessBoard.Instance != null)
        {
            ChessBoard.Instance.MovePiece(move.fromPosition, move.toPosition);
        }
        else
        {
            Debug.LogError("AIPlayer: ChessBoard.Instance is null, cannot execute move");
        }
    }
    
    /// <summary>
    /// Execute the AI's chosen placement
    /// </summary>
    private void ExecutePlacement(AIPlacement placement)
    {
        if (placement == null || placement.piece == null)
        {
            Debug.LogError("AIPlayer: Cannot execute null placement");
            return;
        }
        
        Debug.Log($"🎮 AIPlayer: Executing placement {placement.piece.pieceType} at {placement.position}");
        
        // Use the existing placement manager system
        if (PlacementManager.Instance != null)
        {
            // Simulate the placement process: select piece, then place it
            PlacementManager.Instance.OnTrayPieceClicked(placement.piece);
            PlacementManager.Instance.OnPlacementTargetClicked(placement.position);
            
            Debug.Log($"✅ AIPlayer: Successfully executed placement of {placement.piece.pieceType} at {placement.position}");
        }
        else
        {
            Debug.LogError("AIPlayer: PlacementManager.Instance is null, cannot execute placement");
        }
    }
    
    /// <summary>
    /// Set the AI difficulty level
    /// </summary>
    public void SetDifficulty(AIDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
        
        // Adjust thinking times based on difficulty
        switch (difficulty)
        {
            case AIDifficulty.Easy:
                thinkingTimeMin = 0.5f;
                thinkingTimeMax = 1.5f;
                break;
            case AIDifficulty.Medium:
                thinkingTimeMin = 1.0f;
                thinkingTimeMax = 3.0f;
                break;
            case AIDifficulty.Hard:
                thinkingTimeMin = 2.0f;
                thinkingTimeMax = 5.0f;
                break;
        }
        
        Debug.Log($"AIPlayer: Difficulty set to {difficulty} (thinking time: {thinkingTimeMin}-{thinkingTimeMax}s, depth: {GetSearchDepth()})");
        
        // Update minimax engine debug settings
        if (minimaxEngine != null)
        {
            minimaxEngine = new MinimaxEngine(enableDebugLogging);
        }
        
        // Update placement engine with new difficulty
        if (placementEngine != null)
        {
            placementEngine = new AIPlacementEngine(difficulty, enableDebugLogging);
        }
    }
    
    /// <summary>
    /// Get the current search depth based on difficulty
    /// </summary>
    public int GetSearchDepth()
    {
        return (int)difficulty;
    }
    
    /// <summary>
    /// Check if the AI is currently thinking (move or placement)
    /// </summary>
    public bool IsThinking()
    {
        return isThinking || isPlacing;
    }
    
    /// <summary>
    /// Check if the AI is currently placing pieces
    /// </summary>
    public bool IsPlacing()
    {
        return isPlacing;
    }
    
    /// <summary>
    /// Stop the AI thinking process (for cancellation)
    /// </summary>
    public void StopThinking()
    {
        if (currentThinkingCoroutine != null)
        {
            StopCoroutine(currentThinkingCoroutine);
            currentThinkingCoroutine = null;
        }
        
        isThinking = false;
        isPlacing = false;
        Debug.Log("AIPlayer: Thinking/placement process stopped");
    }
}

/// <summary>
/// Represents a potential move the AI can make
/// </summary>
[System.Serializable]
public class AIMove
{
    public ChessPiece piece;
    public BoardPosition fromPosition;
    public BoardPosition toPosition;
    public float evaluationScore;
    
    public override string ToString()
    {
        return $"{piece?.pieceType} {fromPosition} → {toPosition} (score: {evaluationScore:F2})";
    }
}