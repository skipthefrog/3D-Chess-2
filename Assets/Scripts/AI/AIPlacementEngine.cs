using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placement strategy types for AI players
/// </summary>
public enum PlacementStrategy
{
    Random,         // Easy: Random valid placement
    Defensive,      // Medium: Focus on king safety and back-rank pieces
    Balanced,       // Medium: Mix of offensive and defensive positioning
    Aggressive      // Hard: Forward positioning and center control
}

/// <summary>
/// Strategic placement engine for AI players during the setup phase.
/// Provides intelligent piece placement based on 3D chess principles.
/// </summary>
public class AIPlacementEngine
{
    [Header("Placement Settings")]
    private PlacementStrategy strategy;
    private AIDifficulty difficulty;
    private bool enableDebugLogging;
    
    // Position evaluation weights
    private const float CENTER_CONTROL_WEIGHT = 2.0f;
    private const float KING_SAFETY_WEIGHT = 3.0f;
    private const float PIECE_COORDINATION_WEIGHT = 1.5f;
    private const float EDGE_PENALTY = -0.5f;
    
    public AIPlacementEngine(AIDifficulty aiDifficulty, bool debugLogging = false)
    {
        difficulty = aiDifficulty;
        enableDebugLogging = debugLogging;
        strategy = GetStrategyForDifficulty(aiDifficulty);
        
        if (enableDebugLogging)
            Debug.Log($"AIPlacementEngine: Created with difficulty {difficulty}, strategy {strategy}");
    }
    
    /// <summary>
    /// Get the placement strategy based on AI difficulty
    /// </summary>
    private PlacementStrategy GetStrategyForDifficulty(AIDifficulty difficulty)
    {
        return difficulty switch
        {
            AIDifficulty.Easy => PlacementStrategy.Random,
            AIDifficulty.Medium => Random.value < 0.5f ? PlacementStrategy.Defensive : PlacementStrategy.Balanced,
            AIDifficulty.Hard => PlacementStrategy.Aggressive,
            _ => PlacementStrategy.Balanced
        };
    }
    
    /// <summary>
    /// Find the best placement for an AI player
    /// </summary>
    public AIPlacement FindBestPlacement(PieceColor playerColor)
    {
        if (enableDebugLogging)
            Debug.Log($"AIPlacementEngine: Finding best placement for {playerColor} using {strategy} strategy");
        
        // Get available pieces from tray
        List<ChessPiece> availablePieces = GetAvailablePieces(playerColor);
        if (availablePieces.Count == 0)
        {
            Debug.LogWarning($"AIPlacementEngine: No available pieces for {playerColor}");
            return null;
        }
        
        // Get valid placement positions for this color
        List<BoardPosition> validPositions = GetValidPlacementPositions(playerColor);
        if (validPositions.Count == 0)
        {
            Debug.LogWarning($"AIPlacementEngine: No valid positions for {playerColor}");
            return null;
        }
        
        // Find the best piece-position combination
        AIPlacement bestPlacement = null;
        float bestScore = float.MinValue;
        
        foreach (ChessPiece piece in availablePieces)
        {
            foreach (BoardPosition position in validPositions)
            {
                float score = EvaluatePlacement(piece, position, playerColor);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPlacement = new AIPlacement
                    {
                        piece = piece,
                        position = position,
                        evaluationScore = score
                    };
                }
            }
        }
        
        if (enableDebugLogging && bestPlacement != null)
        {
            Debug.Log($"AIPlacementEngine: Best placement: {bestPlacement.piece.pieceType} at {bestPlacement.position} (score: {bestPlacement.evaluationScore:F2})");
        }
        
        return bestPlacement;
    }
    
    /// <summary>
    /// Get all available pieces from the player's tray
    /// </summary>
    private List<ChessPiece> GetAvailablePieces(PieceColor playerColor)
    {
        List<ChessPiece> pieces = new List<ChessPiece>();
        
        PieceTray tray = (playerColor == PieceColor.White) ? PieceTray.WhiteTray : PieceTray.BlackTray;
        if (tray != null)
        {
            // Get all pieces from the tray
            ChessPiece[] trayPieces = tray.GetComponentsInChildren<ChessPiece>();
            foreach (ChessPiece piece in trayPieces)
            {
                if (piece.pieceColor == playerColor)
                {
                    pieces.Add(piece);
                }
            }
        }
        
        return pieces;
    }
    
    /// <summary>
    /// Get all valid placement positions for the given color
    /// </summary>
    private List<BoardPosition> GetValidPlacementPositions(PieceColor playerColor)
    {
        List<BoardPosition> validPositions = new List<BoardPosition>();
        
        if (ChessBoard.Instance == null) return validPositions;
        
        int validX = PlacementManager.GetValidXForColor(playerColor);
        
        // Check all positions in the valid X-layer
        for (int y = 0; y < 4; y++)
        {
            for (int z = 0; z < 4; z++)
            {
                BoardPosition position = new BoardPosition(validX, y, z);
                
                if (ChessBoard.Instance.CanPlacePieceAt(position, playerColor))
                {
                    validPositions.Add(position);
                }
            }
        }
        
        return validPositions;
    }
    
    /// <summary>
    /// Evaluate the quality of placing a specific piece at a specific position
    /// </summary>
    private float EvaluatePlacement(ChessPiece piece, BoardPosition position, PieceColor playerColor)
    {
        float score = 0f;
        
        switch (strategy)
        {
            case PlacementStrategy.Random:
                score = Random.Range(0f, 1f);
                break;
                
            case PlacementStrategy.Defensive:
                score = EvaluateDefensivePlacement(piece, position, playerColor);
                break;
                
            case PlacementStrategy.Balanced:
                score = EvaluateBalancedPlacement(piece, position, playerColor);
                break;
                
            case PlacementStrategy.Aggressive:
                score = EvaluateAggressivePlacement(piece, position, playerColor);
                break;
        }
        
        // Add small random factor to prevent identical games
        score += Random.Range(-0.1f, 0.1f);
        
        return score;
    }
    
    /// <summary>
    /// Evaluate defensive placement focusing on king safety and back-rank pieces
    /// </summary>
    private float EvaluateDefensivePlacement(ChessPiece piece, BoardPosition position, PieceColor playerColor)
    {
        float score = 0f;
        
        // King safety: prefer corners and edges for king
        if (piece.pieceType == ChessPieceType.King)
        {
            bool isCorner = (position.y == 0 || position.y == 3) && (position.z == 0 || position.z == 3);
            bool isEdge = position.y == 0 || position.y == 3 || position.z == 0 || position.z == 3;
            
            if (isCorner) score += KING_SAFETY_WEIGHT * 2f;
            else if (isEdge) score += KING_SAFETY_WEIGHT;
        }
        
        // Major pieces in back positions (closer to x=0 for white, x=3 for black)
        if (piece.pieceType == ChessPieceType.Queen || piece.pieceType == ChessPieceType.Rook)
        {
            // In 3D chess, "back" means the layer closest to the player
            score += 1.5f; // Prefer keeping major pieces protected
        }
        
        // Minor pieces can be more forward
        if (piece.pieceType == ChessPieceType.Bishop || piece.pieceType == ChessPieceType.Knight)
        {
            score += 1.0f;
        }
        
        return score;
    }
    
    /// <summary>
    /// Evaluate balanced placement with mixed offensive and defensive considerations
    /// </summary>
    private float EvaluateBalancedPlacement(ChessPiece piece, BoardPosition position, PieceColor playerColor)
    {
        float score = 0f;
        
        // Center control bonus
        float centerDistance = GetDistanceFromCenter(position);
        score += (4f - centerDistance) * CENTER_CONTROL_WEIGHT * 0.5f;
        
        // Piece-specific positioning
        switch (piece.pieceType)
        {
            case ChessPieceType.King:
                // Moderate king safety
                bool isProtected = position.y > 0 && position.y < 3 && position.z > 0 && position.z < 3;
                if (isProtected) score += KING_SAFETY_WEIGHT * 0.7f;
                break;
                
            case ChessPieceType.Queen:
                // Queens prefer central positions but not too exposed
                score += (4f - centerDistance) * 1.2f;
                break;
                
            case ChessPieceType.Rook:
                // Rooks prefer corners and edges for file/rank control
                bool onEdge = position.y == 0 || position.y == 3 || position.z == 0 || position.z == 3;
                if (onEdge) score += 1.0f;
                break;
                
            case ChessPieceType.Bishop:
                // Bishops prefer positions with diagonal control
                score += 1.0f + Random.Range(0f, 0.5f);
                break;
                
            case ChessPieceType.Knight:
                // Knights prefer central positions for maximum mobility
                score += (4f - centerDistance) * 1.0f;
                break;
        }
        
        return score;
    }
    
    /// <summary>
    /// Evaluate aggressive placement focusing on forward positioning and center control
    /// </summary>
    private float EvaluateAggressivePlacement(ChessPiece piece, BoardPosition position, PieceColor playerColor)
    {
        float score = 0f;
        
        // Strong center control emphasis
        float centerDistance = GetDistanceFromCenter(position);
        score += (4f - centerDistance) * CENTER_CONTROL_WEIGHT;
        
        // Piece-specific aggressive positioning
        switch (piece.pieceType)
        {
            case ChessPieceType.King:
                // Still prioritize king safety but less conservatively
                bool isReasonablySafe = !(position.y == 1.5f && position.z == 1.5f); // Avoid dead center
                if (isReasonablySafe) score += KING_SAFETY_WEIGHT * 0.5f;
                break;
                
            case ChessPieceType.Queen:
                // Queens in central, commanding positions
                score += (4f - centerDistance) * 2.0f;
                break;
                
            case ChessPieceType.Rook:
                // Rooks in positions to control files/ranks
                score += 1.5f;
                break;
                
            case ChessPieceType.Bishop:
                // Bishops in positions for long diagonal control
                score += (4f - centerDistance) * 1.3f;
                break;
                
            case ChessPieceType.Knight:
                // Knights in forward, central positions
                score += (4f - centerDistance) * 1.5f;
                break;
        }
        
        return score;
    }
    
    /// <summary>
    /// Calculate distance from board center (1.5, 1.5 in Y-Z plane)
    /// </summary>
    private float GetDistanceFromCenter(BoardPosition position)
    {
        float centerY = 1.5f;
        float centerZ = 1.5f;
        
        return Mathf.Sqrt(Mathf.Pow(position.y - centerY, 2) + Mathf.Pow(position.z - centerZ, 2));
    }
}

/// <summary>
/// Represents an AI placement decision
/// </summary>
[System.Serializable]
public class AIPlacement
{
    public ChessPiece piece;
    public BoardPosition position;
    public float evaluationScore;
    
    public override string ToString()
    {
        return $"{piece?.pieceType} at {position} (score: {evaluationScore:F2})";
    }
}