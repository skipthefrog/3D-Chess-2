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
        Debug.Log($"🤖 AIPlacementEngine.FindBestPlacement: ENTRY - Finding placement for {playerColor} using {strategy} strategy");
        
        // Get available pieces from tray
        Debug.Log($"🤖 AIPlacementEngine: Step 1 - Getting available pieces for {playerColor}");
        List<ChessPiece> availablePieces = GetAvailablePieces(playerColor);
        Debug.Log($"🤖 AIPlacementEngine: Found {availablePieces.Count} available pieces");
        
        if (availablePieces.Count == 0)
        {
            Debug.LogError($"🚨 AIPlacementEngine: FAILURE - No available pieces for {playerColor}");

            // Enhanced diagnostics for piece availability failure
            PieceTray tray = PieceTray.GetTrayForColor(playerColor);
            if (tray == null)
            {
                Debug.LogError($"🚨 AIPlacementEngine: {playerColor} tray is NULL!");
            }
            else
            {
                Debug.LogError($"🚨 AIPlacementEngine: {playerColor} tray exists but has no pieces or validation failed");
                ChessPiece[] trayPieces = tray.GetComponentsInChildren<ChessPiece>();
                Debug.LogError($"🚨 AIPlacementEngine: Raw tray piece count: {trayPieces.Length}");
                foreach (ChessPiece piece in trayPieces)
                {
                    Debug.LogError($"  - {piece.pieceColor} {piece.pieceType} at position {piece.CurrentPosition}");
                }
            }
            return null;
        }
        
        // Get valid placement positions for this color
        Debug.Log($"🤖 AIPlacementEngine: Step 2 - Getting valid placement positions for {playerColor}");
        List<BoardPosition> validPositions = GetValidPlacementPositions(playerColor);
        Debug.Log($"🤖 AIPlacementEngine: Found {validPositions.Count} valid positions");
        
        if (validPositions.Count == 0)
        {
            Debug.LogError($"🚨 AIPlacementEngine: FAILURE - No valid positions for {playerColor}");
            
            // Enhanced diagnostics for position validation failure
            PlacementZone zone = PlacementManager.GetPlacementZoneForColor(playerColor);
            Debug.LogError($"🚨 AIPlacementEngine: Expected zone for {playerColor}: X=[{zone.xMin}-{zone.xMax}], Y=[{zone.yMin}-{zone.yMax}], Z=[{zone.zMin}-{zone.zMax}]");

            if (ChessBoard.Instance == null)
            {
                Debug.LogError($"🚨 AIPlacementEngine: ChessBoard.Instance is NULL!");
            }
            else
            {
                Debug.LogError($"🚨 AIPlacementEngine: ChessBoard exists, checking specific positions in zone...");
                for (int x = zone.xMin; x <= zone.xMax; x++)
                {
                    for (int y = zone.yMin; y <= zone.yMax; y++)
                    {
                        for (int z = zone.zMin; z <= zone.zMax; z++)
                        {
                            BoardPosition pos = new BoardPosition(x, y, z);
                            bool canPlace = ChessBoard.Instance.CanPlacePieceAt(pos, playerColor);
                            Debug.LogError($"  - Position {pos}: CanPlacePieceAt = {canPlace}");
                        }
                    }
                }
            }
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
        Debug.Log($"🔍 AIPlacementEngine.GetAvailablePieces: ENTRY - Getting pieces for {playerColor}");
        List<ChessPiece> pieces = new List<ChessPiece>();

        // Use dynamic tray lookup to support all 6 player colors
        PieceTray tray = PieceTray.GetTrayForColor(playerColor);
        Debug.Log($"🔍 AIPlacementEngine.GetAvailablePieces: Tray reference = {(tray != null ? "EXISTS" : "NULL")}");
        
        if (tray != null)
        {
            // Get all pieces from the tray
            ChessPiece[] trayPieces = tray.GetComponentsInChildren<ChessPiece>();
            Debug.Log($"🔍 AIPlacementEngine.GetAvailablePieces: Raw tray pieces found: {trayPieces.Length}");
            
            foreach (ChessPiece piece in trayPieces)
            {
                Debug.Log($"🔍 AIPlacementEngine.GetAvailablePieces: Examining piece {piece.pieceColor} {piece.pieceType}");
                Debug.Log($"    - Position: {piece.CurrentPosition}");
                Debug.Log($"    - IsInTray(): {piece.IsInTray()}");
                Debug.Log($"    - Color match ({playerColor}): {piece.pieceColor == playerColor}");
                
                if (piece.pieceColor == playerColor)
                {
                    // POTENTIAL ISSUE: Check if piece position is causing problems
                    if (piece.CurrentPosition.IsValid())
                    {
                        Debug.LogWarning($"⚠️ AIPlacementEngine.GetAvailablePieces: Tray piece {piece.pieceColor} {piece.pieceType} has VALID position {piece.CurrentPosition} - this may cause issues!");
                        Debug.LogWarning($"⚠️ If this happens, it means the PlacementManager tray repair didn't work or wasn't called!");
                    }
                    
                    pieces.Add(piece);
                    Debug.Log($"✅ AIPlacementEngine.GetAvailablePieces: Added {piece.pieceColor} {piece.pieceType}");
                }
            }
        }
        
        Debug.Log($"🔍 AIPlacementEngine.GetAvailablePieces: EXIT - Returning {pieces.Count} pieces");
        return pieces;
    }
    
    /// <summary>
    /// Get all valid placement positions for the given color using dynamic placement zones
    /// </summary>
    private List<BoardPosition> GetValidPlacementPositions(PieceColor playerColor)
    {
        List<BoardPosition> validPositions = new List<BoardPosition>();

        if (ChessBoard.Instance == null) return validPositions;

        // Get the placement zone for this color (handles 4x4x4, 6x6x6, 8x8x8 boards)
        PlacementZone zone = PlacementManager.GetPlacementZoneForColor(playerColor);

        Debug.Log($"🤖 AIPlacementEngine.GetValidPlacementPositions: {playerColor} zone = X:[{zone.xMin}-{zone.xMax}], Y:[{zone.yMin}-{zone.yMax}], Z:[{zone.zMin}-{zone.zMax}]");

        // Check all positions within the placement zone
        for (int x = zone.xMin; x <= zone.xMax; x++)
        {
            for (int y = zone.yMin; y <= zone.yMax; y++)
            {
                for (int z = zone.zMin; z <= zone.zMax; z++)
                {
                    BoardPosition position = new BoardPosition(x, y, z);

                    if (ChessBoard.Instance.CanPlacePieceAt(position, playerColor))
                    {
                        validPositions.Add(position);
                    }
                }
            }
        }

        Debug.Log($"🤖 AIPlacementEngine.GetValidPlacementPositions: Found {validPositions.Count} valid positions for {playerColor}");
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

        // Get board dimensions for dynamic evaluation
        Vector3Int dims = BoardDimensionsManager.Instance != null
            ? BoardDimensionsManager.Instance.GetDimensions()
            : new Vector3Int(4, 4, 4);

        // Get placement zone for this color
        PlacementZone zone = PlacementManager.GetPlacementZoneForColor(playerColor);

        // King safety: prefer corners and edges for king
        if (piece.pieceType == ChessPieceType.King)
        {
            bool isCorner = (position.y == zone.yMin || position.y == zone.yMax) &&
                            (position.z == zone.zMin || position.z == zone.zMax);
            bool isEdge = position.y == zone.yMin || position.y == zone.yMax ||
                          position.z == zone.zMin || position.z == zone.zMax;

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

        // Get board dimensions and placement zone
        Vector3Int dims = BoardDimensionsManager.Instance != null
            ? BoardDimensionsManager.Instance.GetDimensions()
            : new Vector3Int(4, 4, 4);

        PlacementZone zone = PlacementManager.GetPlacementZoneForColor(playerColor);

        // Center control bonus
        float centerDistance = GetDistanceFromCenter(position, zone);
        float maxDistance = Mathf.Sqrt(Mathf.Pow((zone.yMax - zone.yMin) / 2f, 2) + Mathf.Pow((zone.zMax - zone.zMin) / 2f, 2));
        score += (maxDistance - centerDistance) * CENTER_CONTROL_WEIGHT * 0.5f;

        // Piece-specific positioning
        switch (piece.pieceType)
        {
            case ChessPieceType.King:
                // Moderate king safety - prefer positions away from edges
                bool isProtected = position.y > zone.yMin && position.y < zone.yMax &&
                                   position.z > zone.zMin && position.z < zone.zMax;
                if (isProtected) score += KING_SAFETY_WEIGHT * 0.7f;
                break;

            case ChessPieceType.Queen:
                // Queens prefer central positions but not too exposed
                score += (maxDistance - centerDistance) * 1.2f;
                break;

            case ChessPieceType.Rook:
                // Rooks prefer corners and edges for file/rank control
                bool onEdge = position.y == zone.yMin || position.y == zone.yMax ||
                              position.z == zone.zMin || position.z == zone.zMax;
                if (onEdge) score += 1.0f;
                break;

            case ChessPieceType.Bishop:
                // Bishops prefer positions with diagonal control
                score += 1.0f + Random.Range(0f, 0.5f);
                break;

            case ChessPieceType.Knight:
                // Knights prefer central positions for maximum mobility
                score += (maxDistance - centerDistance) * 1.0f;
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

        // Get board dimensions and placement zone
        Vector3Int dims = BoardDimensionsManager.Instance != null
            ? BoardDimensionsManager.Instance.GetDimensions()
            : new Vector3Int(4, 4, 4);

        PlacementZone zone = PlacementManager.GetPlacementZoneForColor(playerColor);

        // Strong center control emphasis
        float centerDistance = GetDistanceFromCenter(position, zone);
        float maxDistance = Mathf.Sqrt(Mathf.Pow((zone.yMax - zone.yMin) / 2f, 2) + Mathf.Pow((zone.zMax - zone.zMin) / 2f, 2));
        score += (maxDistance - centerDistance) * CENTER_CONTROL_WEIGHT;

        // Calculate zone center for safety checks
        float centerY = (zone.yMin + zone.yMax) / 2f;
        float centerZ = (zone.zMin + zone.zMax) / 2f;

        // Piece-specific aggressive positioning
        switch (piece.pieceType)
        {
            case ChessPieceType.King:
                // Still prioritize king safety but less conservatively - avoid dead center
                bool isReasonablySafe = !(Mathf.Abs(position.y - centerY) < 0.5f && Mathf.Abs(position.z - centerZ) < 0.5f);
                if (isReasonablySafe) score += KING_SAFETY_WEIGHT * 0.5f;
                break;

            case ChessPieceType.Queen:
                // Queens in central, commanding positions
                score += (maxDistance - centerDistance) * 2.0f;
                break;

            case ChessPieceType.Rook:
                // Rooks in positions to control files/ranks
                score += 1.5f;
                break;

            case ChessPieceType.Bishop:
                // Bishops in positions for long diagonal control
                score += (maxDistance - centerDistance) * 1.3f;
                break;

            case ChessPieceType.Knight:
                // Knights in forward, central positions
                score += (maxDistance - centerDistance) * 1.5f;
                break;
        }

        return score;
    }
    
    /// <summary>
    /// Calculate distance from placement zone center (dynamic based on zone size)
    /// </summary>
    private float GetDistanceFromCenter(BoardPosition position, PlacementZone zone)
    {
        float centerY = (zone.yMin + zone.yMax) / 2f;
        float centerZ = (zone.zMin + zone.zMax) / 2f;

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