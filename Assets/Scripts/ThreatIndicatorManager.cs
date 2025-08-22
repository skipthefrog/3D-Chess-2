using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages visual indicators for pieces that are threatening the king during check conditions.
/// Shows orange/red indicators on attacking pieces to distinguish them from the threatened king.
/// Integrates with CheckDetectionManager and CheckVisualFeedbackManager.
/// </summary>
public class ThreatIndicatorManager : MonoBehaviour
{
    [Header("Threat Indicator Settings")]
    public bool enableThreatIndicators = true;
    public bool showAttackPaths = true;
    
    [Header("Visual Settings")]
    [Range(0.5f, 3.0f)]
    public float attackerIndicatorSize = 1.5f;
    [Range(0.1f, 1.0f)]
    public float attackerIndicatorAlpha = 0.7f;
    [Range(1.0f, 5.0f)]
    public float attackerPulseSpeed = 2.0f;
    
    [Header("Attack Path Settings")]
    [Range(0.1f, 1.0f)]
    public float attackPathWidth = 0.2f;  // MODERATE: Thicker than default but not extreme
    [Range(0.1f, 1.0f)]
    public float attackPathAlpha = 0.7f;  // MODERATE: More visible than default but not extreme
    
    public static ThreatIndicatorManager Instance { get; private set; }
    
    // Track active threat indicators
    private Dictionary<ChessPiece, GameObject> activeAttackerIndicators = new Dictionary<ChessPiece, GameObject>();
    private List<GameObject> activeAttackPaths = new List<GameObject>();
    private CheckThreatInfo currentThreats;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("ThreatIndicatorManager: Instance created");
        }
        else
        {
            Debug.LogWarning("ThreatIndicatorManager: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Subscribe to check detection events
        if (CheckDetectionManager.Instance != null)
        {
            Debug.Log("ThreatIndicatorManager: Subscribing to CheckDetectionManager events");
            // We'll integrate with CheckVisualFeedbackManager events instead
        }
        else
        {
            Debug.LogWarning("ThreatIndicatorManager: CheckDetectionManager.Instance not found");
        }
        
        // Subscribe to visual feedback events for coordination
        if (CheckVisualFeedbackManager.Instance != null)
        {
            Debug.Log("ThreatIndicatorManager: CheckVisualFeedbackManager found");
        }
        else
        {
            Debug.LogWarning("ThreatIndicatorManager: CheckVisualFeedbackManager not found");
        }
    }
    
    /// <summary>
    /// Show threat indicators for a king in check
    /// </summary>
    /// <param name="kingColor">Color of the king being threatened</param>
    public void ShowThreatIndicators(PieceColor kingColor)
    {
        Debug.Log($"🟠 ThreatIndicatorManager.ShowThreatIndicators: ENTRY - called for {kingColor} king");
        Debug.Log($"🟠 ThreatIndicatorManager.ShowThreatIndicators: enableThreatIndicators={enableThreatIndicators}");
        Debug.Log($"🟠 ThreatIndicatorManager.ShowThreatIndicators: CheckDetectionManager.Instance={(CheckDetectionManager.Instance != null ? "EXISTS" : "NULL")}");
        
        if (!enableThreatIndicators || CheckDetectionManager.Instance == null)
        {
            Debug.Log("🟠 ThreatIndicatorManager.ShowThreatIndicators: Disabled or CheckDetectionManager not available - EXITING");
            return;
        }
        
        Debug.Log($"🟠 ThreatIndicatorManager.ShowThreatIndicators: Showing threats for {kingColor} king");
        
        // CUMULATIVE APPROACH: Don't clear all indicators, only clear invalid ones
        Debug.Log($"🟠 ThreatIndicatorManager.ShowThreatIndicators: Using cumulative approach - preserving existing valid indicators");
        
        // Get threat information
        currentThreats = CheckDetectionManager.Instance.GetCheckThreats(kingColor);
        
        if (!currentThreats.HasThreats)
        {
            Debug.Log($"ThreatIndicatorManager.ShowThreatIndicators: No threats found for {kingColor} king");
            return;
        }
        
        Debug.Log($"🟠 ThreatIndicatorManager.ShowThreatIndicators: Found {currentThreats.attackingPieces.Count} attacking pieces");
        
        // Create indicators for each attacking piece (cumulative approach)
        foreach (ChessPiece attacker in currentThreats.attackingPieces)
        {
            // Check if this attacker already has an indicator
            if (activeAttackerIndicators.ContainsKey(attacker))
            {
                Debug.Log($"🟠 ThreatIndicatorManager.ShowThreatIndicators: Indicator already exists for {attacker.pieceColor} {attacker.pieceType} at {attacker.CurrentPosition} - skipping");
                continue;
            }
            
            Debug.Log($"🟠 ThreatIndicatorManager.ShowThreatIndicators: Creating NEW indicator for {attacker.pieceColor} {attacker.pieceType} at {attacker.CurrentPosition}");
            CreateAttackerIndicator(attacker);
        }
        
        // Create attack path lines if enabled
        if (showAttackPaths)
        {
            Debug.Log($"🟠 ThreatIndicatorManager.ShowThreatIndicators: Creating attack paths...");
            CreateAttackPaths();
        }
        
        Debug.Log($"🟠 ThreatIndicatorManager.ShowThreatIndicators: Created {activeAttackerIndicators.Count} attacker indicators and {activeAttackPaths.Count} attack paths");
        
        // Validate that indicators were actually created
        if (currentThreats.attackingPieces.Count > 0 && activeAttackerIndicators.Count == 0)
        {
            Debug.LogError($"🟠 ❌ ThreatIndicatorManager.ShowThreatIndicators: PROBLEM - {currentThreats.attackingPieces.Count} attacking pieces found but NO indicators created!");
        }
        else if (activeAttackerIndicators.Count != currentThreats.attackingPieces.Count)
        {
            Debug.LogWarning($"🟠 ⚠️ ThreatIndicatorManager.ShowThreatIndicators: MISMATCH - {currentThreats.attackingPieces.Count} attacking pieces but {activeAttackerIndicators.Count} indicators created");
        }
        else
        {
            Debug.Log($"🟠 ✅ ThreatIndicatorManager.ShowThreatIndicators: SUCCESS - All {currentThreats.attackingPieces.Count} attacking pieces have indicators");
        }
    }
    
    /// <summary>
    /// Hide all threat indicators
    /// </summary>
    public void ClearThreatIndicators()
    {
        Debug.Log($"ThreatIndicatorManager.ClearThreatIndicators: Clearing {activeAttackerIndicators.Count} attacker indicators and {activeAttackPaths.Count} attack paths");
        
        // Clear attacker indicators
        foreach (var kvp in activeAttackerIndicators)
        {
            if (kvp.Value != null)
            {
                DestroyImmediate(kvp.Value);
            }
        }
        activeAttackerIndicators.Clear();
        
        // Clear attack paths
        foreach (GameObject path in activeAttackPaths)
        {
            if (path != null)
            {
                DestroyImmediate(path);
            }
        }
        activeAttackPaths.Clear();
        
        currentThreats = null;
        
        Debug.Log("ThreatIndicatorManager.ClearThreatIndicators: All threat indicators cleared");
    }
    
    /// <summary>
    /// Clear threat indicators only for pieces that are no longer attacking the specified king
    /// </summary>
    /// <param name="resolvedKingColor">The king whose check was resolved</param>
    public void ClearThreatIndicatorsForResolvedKing(PieceColor resolvedKingColor)
    {
        if (CheckDetectionManager.Instance == null)
        {
            Debug.LogWarning("ThreatIndicatorManager.ClearThreatIndicatorsForResolvedKing: CheckDetectionManager not available");
            return;
        }
        
        Debug.Log($"🟢 ThreatIndicatorManager.ClearThreatIndicatorsForResolvedKing: Clearing indicators for {resolvedKingColor} king resolution");
        
        // Get current threats for the resolved king (should be empty now)
        CheckThreatInfo resolvedThreats = CheckDetectionManager.Instance.GetCheckThreats(resolvedKingColor);
        
        // Find indicators to remove: pieces that were attacking this king but no longer are
        List<ChessPiece> indicatorsToRemove = new List<ChessPiece>();
        
        foreach (var kvp in activeAttackerIndicators)
        {
            ChessPiece attacker = kvp.Key;
            
            // If this piece is no longer attacking the resolved king, remove its indicator
            if (!resolvedThreats.attackingPieces.Contains(attacker))
            {
                // But check if this piece is still attacking the OTHER king
                PieceColor otherKingColor = (resolvedKingColor == PieceColor.White) ? PieceColor.Black : PieceColor.White;
                CheckThreatInfo otherKingThreats = CheckDetectionManager.Instance.GetCheckThreats(otherKingColor);
                
                if (!otherKingThreats.attackingPieces.Contains(attacker))
                {
                    // This piece is not attacking either king, remove its indicator
                    indicatorsToRemove.Add(attacker);
                    Debug.Log($"🟢 ThreatIndicatorManager.ClearThreatIndicatorsForResolvedKing: Removing indicator for {attacker.pieceColor} {attacker.pieceType} (no longer attacking any king)");
                }
                else
                {
                    Debug.Log($"🟢 ThreatIndicatorManager.ClearThreatIndicatorsForResolvedKing: Keeping indicator for {attacker.pieceColor} {attacker.pieceType} (still attacking {otherKingColor} king)");
                }
            }
        }
        
        // Remove the identified indicators
        foreach (ChessPiece attacker in indicatorsToRemove)
        {
            if (activeAttackerIndicators.ContainsKey(attacker))
            {
                GameObject indicator = activeAttackerIndicators[attacker];
                if (indicator != null)
                {
                    DestroyImmediate(indicator);
                }
                activeAttackerIndicators.Remove(attacker);
            }
        }
        
        Debug.Log($"🟢 ThreatIndicatorManager.ClearThreatIndicatorsForResolvedKing: Removed {indicatorsToRemove.Count} indicators, {activeAttackerIndicators.Count} remain");
        
        // Clear attack paths that are no longer valid
        ClearInvalidAttackPaths();
        
        Debug.Log($"🟢 ThreatIndicatorManager.ClearThreatIndicatorsForResolvedKing: Attack paths cleared, {activeAttackPaths.Count} paths remain");
    }
    
    /// <summary>
    /// Create a visual indicator for an attacking piece
    /// </summary>
    /// <param name="attacker">The piece threatening the king</param>
    private void CreateAttackerIndicator(ChessPiece attacker)
    {
        if (attacker == null || ChessBoard.Instance == null)
        {
            Debug.LogError("ThreatIndicatorManager.CreateAttackerIndicator: Null attacker or ChessBoard");
            return;
        }
        
        Debug.Log($"🟠 ThreatIndicatorManager.CreateAttackerIndicator: Creating indicator for {attacker.pieceColor} {attacker.pieceType} at {attacker.CurrentPosition}");
        
        // Create indicator sphere
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = $"ThreatIndicator_{attacker.pieceColor}_{attacker.pieceType}_{attacker.CurrentPosition}";
        
        // Make it a child of the rotating board so it rotates with the board
        GameObject piecesContainer = GameObject.Find("Pieces Container");
        if (piecesContainer != null)
        {
            indicator.transform.SetParent(piecesContainer.transform);
        }
        
        // Position at the attacking piece's location (slightly above)
        Vector3 localPos = ChessBoard.Instance.BoardToLocalPosition(attacker.CurrentPosition);
        indicator.transform.localPosition = localPos + Vector3.up * 0.3f; // Lower than king indicator
        
        // Scale for attacker indicator (smaller than king's check indicator)
        indicator.transform.localScale = Vector3.one * attackerIndicatorSize;
        
        // Remove collider so it doesn't interfere with input
        Collider indicatorCollider = indicator.GetComponent<Collider>();
        if (indicatorCollider != null)
        {
            DestroyImmediate(indicatorCollider);
        }
        
        // Create orange/red material for attacking piece
        Renderer renderer = indicator.GetComponent<Renderer>();
        Material threatMaterial = CreateAttackerMaterial();
        renderer.material = threatMaterial;
        
        // Add pulsing animation
        ThreatIndicatorPulse pulseScript = indicator.AddComponent<ThreatIndicatorPulse>();
        pulseScript.pulseSpeed = attackerPulseSpeed;
        pulseScript.minScale = attackerIndicatorSize * 0.8f;
        pulseScript.maxScale = attackerIndicatorSize * 1.2f;
        pulseScript.minAlpha = attackerIndicatorAlpha * 0.5f;
        pulseScript.maxAlpha = attackerIndicatorAlpha;
        
        // Track the indicator
        activeAttackerIndicators[attacker] = indicator;
        
        Debug.Log($"🟠 ThreatIndicatorManager.CreateAttackerIndicator: Successfully created orange indicator '{indicator.name}' at {indicator.transform.localPosition}");
    }
    
    /// <summary>
    /// Create visual attack path lines from attackers to king
    /// </summary>
    private void CreateAttackPaths()
    {
        if (currentThreats == null || !currentThreats.HasThreats || ChessBoard.Instance == null)
        {
            return;
        }
        
        Debug.Log($"ThreatIndicatorManager.CreateAttackPaths: Creating {currentThreats.attackingPieces.Count} attack paths");
        Debug.Log($"ThreatIndicatorManager.CreateAttackPaths: King at board position {currentThreats.kingPosition}");
        
        // FIXED: Use world space coordinates to avoid coordinate system mismatches
        // Find the king piece at the threat position
        ChessPiece kingPiece = ChessBoard.Instance.GetPieceAt(currentThreats.kingPosition);
        if (kingPiece == null)
        {
            Debug.LogError($"ThreatIndicatorManager.CreateAttackPaths: No king found at position {currentThreats.kingPosition}");
            return;
        }
        
        // Use actual king piece world position with elevation for ray visibility
        Vector3 kingRayPos = kingPiece.transform.position + Vector3.up * 0.5f;
        
        Debug.Log($"ThreatIndicatorManager.CreateAttackPaths: King world position for ray: {kingRayPos}");
        
        foreach (ChessPiece attacker in currentThreats.attackingPieces)
        {
            // FIXED: Use actual attacker piece world position with elevation for ray visibility
            Vector3 attackerRayPos = attacker.transform.position + Vector3.up * 0.5f;
            
            Debug.Log($"ThreatIndicatorManager.CreateAttackPaths: Attacker {attacker.pieceColor} {attacker.pieceType} at board {attacker.CurrentPosition}");
            Debug.Log($"ThreatIndicatorManager.CreateAttackPaths: Attacker world position for ray: {attackerRayPos}");
            
            GameObject attackLine = CreateAttackLine(attackerRayPos, kingRayPos);
            if (attackLine != null)
            {
                activeAttackPaths.Add(attackLine);
                Debug.Log($"ThreatIndicatorManager.CreateAttackPaths: Created attack line '{attackLine.name}' from {attackerRayPos} to {kingRayPos}");
            }
        }
        
        Debug.Log($"ThreatIndicatorManager.CreateAttackPaths: Created {activeAttackPaths.Count} attack path lines");
    }
    
    /// <summary>
    /// Create a line renderer for attack path visualization
    /// </summary>
    /// <param name="start">Start position (attacker)</param>
    /// <param name="end">End position (king)</param>
    /// <returns>GameObject with LineRenderer</returns>
    private GameObject CreateAttackLine(Vector3 start, Vector3 end)
    {
        GameObject lineObject = new GameObject("AttackPath");
        
        // Make it a child of the rotating board
        GameObject piecesContainer = GameObject.Find("Pieces Container");
        if (piecesContainer != null)
        {
            lineObject.transform.SetParent(piecesContainer.transform);
        }
        
        // Add LineRenderer component
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        
        // Configure line renderer
        lineRenderer.material = CreateAttackLineMaterial();
        lineRenderer.startWidth = attackPathWidth;
        lineRenderer.endWidth = attackPathWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true; // FIXED: Use world space to avoid coordinate system issues
        
        // Use world space positions directly - no coordinate transformation needed
        Vector3 worldStart = start;
        Vector3 worldEnd = end;
        
        Debug.Log($"ThreatIndicatorManager.CreateAttackLine: Setting LineRenderer world positions - Start: {worldStart}, End: {worldEnd}");
        Debug.Log($"ThreatIndicatorManager.CreateAttackLine: LineRenderer useWorldSpace: {lineRenderer.useWorldSpace}");
        Debug.Log($"ThreatIndicatorManager.CreateAttackLine: Parent container: {(piecesContainer != null ? piecesContainer.name : "NULL")}");
        
        lineRenderer.SetPosition(0, worldStart);
        lineRenderer.SetPosition(1, worldEnd);
        
        // ENHANCED: Material already has optimized render queue (3100) for high visibility
        // No need to override here since CreateAttackLineMaterial sets the appropriate queue
        
        return lineObject;
    }
    
    /// <summary>
    /// Create material for attacking piece indicators (orange/red)
    /// </summary>
    /// <returns>Material for attacker indicators</returns>
    private Material CreateAttackerMaterial()
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // Orange-red color to distinguish from pure red king indicator
        Color attackerColor = new Color(1.0f, 0.4f, 0.0f, attackerIndicatorAlpha); // Orange-red
        material.color = attackerColor;
        
        // Set up transparency
        material.SetFloat("_Surface", 1); // Transparent
        material.SetFloat("_Blend", 0); // Alpha blend
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        
        // Add some emission for visibility
        material.SetFloat("_Smoothness", 0.8f);
        material.SetFloat("_Metallic", 0.1f);
        
        return material;
    }
    
    /// <summary>
    /// Create material for attack path lines - simplified version to avoid breaking gameplay
    /// </summary>
    /// <returns>Material for attack lines</returns>
    private Material CreateAttackLineMaterial()
    {
        // SIMPLIFIED: Use standard URP Lit shader to avoid shader issues
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // ENHANCED: Bright red color for high visibility (keeping this improvement)
        Color lineColor = new Color(1.0f, 0.0f, 0.0f, attackPathAlpha); // Bright red
        material.color = lineColor;
        
        // Standard transparency setup (no complex emission or shader fallbacks)
        material.SetFloat("_Surface", 1); // Transparent
        material.SetFloat("_Blend", 0); // Alpha blend
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        
        // Standard render queue to avoid conflicts
        material.renderQueue = 2900;
        
        return material;
    }
    
    /// <summary>
    /// Update threat indicators when pieces move (cleanup invalid references)
    /// </summary>
    public void ValidateThreatIndicators()
    {
        List<ChessPiece> invalidPieces = new List<ChessPiece>();
        
        foreach (var kvp in activeAttackerIndicators)
        {
            if (kvp.Key == null || kvp.Value == null)
            {
                invalidPieces.Add(kvp.Key);
            }
        }
        
        foreach (ChessPiece invalidPiece in invalidPieces)
        {
            activeAttackerIndicators.Remove(invalidPiece);
        }
        
        if (invalidPieces.Count > 0)
        {
            Debug.Log($"ThreatIndicatorManager.ValidateThreatIndicators: Cleaned up {invalidPieces.Count} invalid indicators");
        }
    }
    
    /// <summary>
    /// Clear attack paths that are no longer valid (no longer represent active threats)
    /// </summary>
    private void ClearInvalidAttackPaths()
    {
        if (CheckDetectionManager.Instance == null)
        {
            return;
        }
        
        // Get current threats for both kings
        CheckThreatInfo whiteKingThreats = CheckDetectionManager.Instance.GetCheckThreats(PieceColor.White);
        CheckThreatInfo blackKingThreats = CheckDetectionManager.Instance.GetCheckThreats(PieceColor.Black);
        
        // Create a set of valid attacker positions that should have attack paths
        HashSet<BoardPosition> validAttackerPositions = new HashSet<BoardPosition>();
        
        foreach (ChessPiece attacker in whiteKingThreats.attackingPieces)
        {
            validAttackerPositions.Add(attacker.CurrentPosition);
        }
        
        foreach (ChessPiece attacker in blackKingThreats.attackingPieces)
        {
            validAttackerPositions.Add(attacker.CurrentPosition);
        }
        
        Debug.Log($"🟢 ThreatIndicatorManager.ClearInvalidAttackPaths: Found {validAttackerPositions.Count} valid attacker positions");
        
        // Clear all attack paths since they don't have position tracking
        // This is a simpler approach: clear all and let them be recreated as needed
        List<GameObject> pathsToRemove = new List<GameObject>(activeAttackPaths);
        
        foreach (GameObject path in pathsToRemove)
        {
            if (path != null)
            {
                Debug.Log($"🟢 ThreatIndicatorManager.ClearInvalidAttackPaths: Clearing attack path '{path.name}'");
                DestroyImmediate(path);
            }
        }
        
        activeAttackPaths.Clear();
        
        Debug.Log($"🟢 ThreatIndicatorManager.ClearInvalidAttackPaths: Cleared {pathsToRemove.Count} attack paths");
    }
    
    /// <summary>
    /// Get debug information about current threat indicators
    /// </summary>
    /// <returns>Debug string</returns>
    public string GetDebugInfo()
    {
        return $"ThreatIndicators: {activeAttackerIndicators.Count} attackers, {activeAttackPaths.Count} paths, Enabled: {enableThreatIndicators}";
    }
}