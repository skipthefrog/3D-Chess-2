using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages checkmate and stalemate detection for 3D chess.
/// Determines when a player has no legal moves and whether the game should end.
/// Integrates with CheckDetectionManager and GameStateManager.
/// </summary>
public class GameEndDetectionManager : MonoBehaviour
{
    [Header("Game End Detection Settings")]
    public bool enableGameEndDetection = true;
    public bool debugMode = false;
    
    [Header("Events")]
    public System.Action<PieceColor> OnCheckmate;
    public System.Action<PieceColor> OnStalemate;
    public System.Action<string> OnGameEnd; // reason for game end
    
    public static GameEndDetectionManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameEndDetectionManager: Instance created");
        }
        else
        {
            Debug.LogWarning("GameEndDetectionManager: Multiple instances detected, destroying duplicate");
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
        }
        
        // Subscribe to turn manager events
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged += OnTurnChanged;
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
        }
        
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged -= OnTurnChanged;
        }
    }
    
    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.Playing)
        {
            Debug.Log("GameEndDetectionManager: Game started - enabling game end detection");
            enableGameEndDetection = true;
        }
        else if (newState == GameState.GameOver)
        {
            Debug.Log("GameEndDetectionManager: Game ended - disabling game end detection");
            enableGameEndDetection = false;
        }
    }
    
    private void OnKingInCheckDetected(PieceColor kingColor)
    {
        if (!enableGameEndDetection) return;
        
        // When a king is in check, immediately check for checkmate
        if (IsCheckmate(kingColor))
        {
            HandleCheckmate(kingColor);
        }
    }
    
    private void OnTurnChanged(PieceColor newCurrentPlayer)
    {
        if (!enableGameEndDetection) return;
        
        // When a turn changes, check if the new current player has any legal moves
        if (!HasLegalMoves(newCurrentPlayer))
        {
            // No legal moves - check if it's checkmate or stalemate
            if (CheckDetectionManager.Instance != null && CheckDetectionManager.Instance.IsKingInCheck(newCurrentPlayer))
            {
                HandleCheckmate(newCurrentPlayer);
            }
            else
            {
                HandleStalemate(newCurrentPlayer);
            }
        }
    }
    
    /// <summary>
    /// Check if a player has any legal moves available
    /// </summary>
    /// <param name="player">Player to check</param>
    /// <returns>True if player has legal moves</returns>
    public bool HasLegalMoves(PieceColor player)
    {
        if (!enableGameEndDetection || ChessBoard.Instance == null)
        {
            return true; // Assume player has moves if detection is disabled
        }
        
        int totalLegalMoves = 0;
        
        // Check all pieces of the specified color
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    BoardPosition pos = new BoardPosition(x, y, z);
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);
                    
                    if (piece != null && piece.pieceColor == player)
                    {
                        List<BoardPosition> legalMoves = piece.GetLegalMoves();
                        totalLegalMoves += legalMoves.Count;
                        
                        if (debugMode)
                        {
                            Debug.Log($"GameEndDetectionManager: {player} {piece.pieceType} at {pos} has {legalMoves.Count} legal moves");
                        }
                        
                        // Early exit if we find any legal move
                        if (legalMoves.Count > 0)
                        {
                            if (debugMode)
                            {
                                Debug.Log($"GameEndDetectionManager: {player} has legal moves (found {legalMoves.Count} moves for {piece.pieceType})");
                            }
                            return true;
                        }
                    }
                }
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"GameEndDetectionManager: {player} has NO legal moves (total: {totalLegalMoves})");
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a player is in checkmate (king in check + no legal moves)
    /// </summary>
    /// <param name="player">Player to check</param>
    /// <returns>True if player is in checkmate</returns>
    public bool IsCheckmate(PieceColor player)
    {
        if (!enableGameEndDetection)
        {
            return false;
        }
        
        // Checkmate requires both: king in check AND no legal moves
        bool kingInCheck = CheckDetectionManager.Instance != null && 
                          CheckDetectionManager.Instance.IsKingInCheck(player);
        bool hasNoLegalMoves = !HasLegalMoves(player);
        
        bool isCheckmate = kingInCheck && hasNoLegalMoves;
        
        if (debugMode)
        {
            Debug.Log($"GameEndDetectionManager: Checkmate check for {player} - King in check: {kingInCheck}, No legal moves: {hasNoLegalMoves}, Result: {isCheckmate}");
        }
        
        return isCheckmate;
    }
    
    /// <summary>
    /// Check if a player is in stalemate (king not in check + no legal moves)
    /// </summary>
    /// <param name="player">Player to check</param>
    /// <returns>True if player is in stalemate</returns>
    public bool IsStalemate(PieceColor player)
    {
        if (!enableGameEndDetection)
        {
            return false;
        }
        
        // Stalemate requires: king NOT in check AND no legal moves
        bool kingNotInCheck = CheckDetectionManager.Instance == null || 
                             !CheckDetectionManager.Instance.IsKingInCheck(player);
        bool hasNoLegalMoves = !HasLegalMoves(player);
        
        bool isStalemate = kingNotInCheck && hasNoLegalMoves;
        
        if (debugMode)
        {
            Debug.Log($"GameEndDetectionManager: Stalemate check for {player} - King not in check: {kingNotInCheck}, No legal moves: {hasNoLegalMoves}, Result: {isStalemate}");
        }
        
        return isStalemate;
    }
    
    /// <summary>
    /// Handle checkmate detection - end the game
    /// </summary>
    private void HandleCheckmate(PieceColor checkmatedPlayer)
    {
        PieceColor winner = (checkmatedPlayer == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        string reason = $"Checkmate! {winner} wins by checkmate against {checkmatedPlayer}";
        
        Debug.Log($"🏁 GameEndDetectionManager: CHECKMATE DETECTED - {reason}");
        
        // Fire events
        OnCheckmate?.Invoke(checkmatedPlayer);
        OnGameEnd?.Invoke(reason);
        
        // End the game
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameState.GameOver);
        }
        
        // Show checkmate message (could be enhanced with UI later)
        Debug.Log($"🏁 GAME OVER: {reason}");
    }
    
    /// <summary>
    /// Handle stalemate detection - end the game in a draw
    /// </summary>
    private void HandleStalemate(PieceColor stalematedPlayer)
    {
        string reason = $"Stalemate! Game ends in a draw - {stalematedPlayer} has no legal moves but is not in check";
        
        Debug.Log($"🏁 GameEndDetectionManager: STALEMATE DETECTED - {reason}");
        
        // Fire events
        OnStalemate?.Invoke(stalematedPlayer);
        OnGameEnd?.Invoke(reason);
        
        // End the game
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameState.GameOver);
        }
        
        // Show stalemate message (could be enhanced with UI later)
        Debug.Log($"🏁 GAME OVER: {reason}");
    }
    
    /// <summary>
    /// Manually check for game end conditions for the current player
    /// </summary>
    public void CheckForGameEnd()
    {
        if (!enableGameEndDetection || TurnManager.Instance == null)
        {
            return;
        }
        
        PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        
        if (debugMode)
        {
            Debug.Log($"GameEndDetectionManager: Checking game end conditions for {currentPlayer}");
        }
        
        if (IsCheckmate(currentPlayer))
        {
            HandleCheckmate(currentPlayer);
        }
        else if (IsStalemate(currentPlayer))
        {
            HandleStalemate(currentPlayer);
        }
        else if (debugMode)
        {
            Debug.Log($"GameEndDetectionManager: {currentPlayer} can continue playing");
        }
    }
    
    /// <summary>
    /// Get debug information about current game end state
    /// </summary>
    /// <returns>Debug string with game end state information</returns>
    public string GetDebugInfo()
    {
        if (!enableGameEndDetection)
        {
            return "Game end detection disabled";
        }
        
        if (TurnManager.Instance == null)
        {
            return "TurnManager not available";
        }
        
        PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        bool hasLegalMoves = HasLegalMoves(currentPlayer);
        bool kingInCheck = CheckDetectionManager.Instance != null && 
                          CheckDetectionManager.Instance.IsKingInCheck(currentPlayer);
        
        string info = $"Current Player: {currentPlayer}\n";
        info += $"Has Legal Moves: {hasLegalMoves}\n";
        info += $"King in Check: {kingInCheck}\n";
        
        if (!hasLegalMoves)
        {
            if (kingInCheck)
            {
                info += "STATUS: CHECKMATE!";
            }
            else
            {
                info += "STATUS: STALEMATE!";
            }
        }
        else
        {
            info += "STATUS: Game continues";
        }
        
        return info;
    }
    
    /// <summary>
    /// Count total legal moves for a player (useful for debugging)
    /// </summary>
    /// <param name="player">Player to count moves for</param>
    /// <returns>Total number of legal moves</returns>
    public int CountLegalMoves(PieceColor player)
    {
        if (!enableGameEndDetection || ChessBoard.Instance == null)
        {
            return -1; // Invalid count
        }
        
        int totalMoves = 0;
        
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    BoardPosition pos = new BoardPosition(x, y, z);
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);
                    
                    if (piece != null && piece.pieceColor == player)
                    {
                        totalMoves += piece.GetLegalMoves().Count;
                    }
                }
            }
        }
        
        return totalMoves;
    }
}