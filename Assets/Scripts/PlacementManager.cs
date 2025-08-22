using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the piece placement phase where players place pieces from trays onto the board
/// </summary>
public class PlacementManager : MonoBehaviour
{
    [Header("Placement Settings")]
    public bool alternatingTurns = false; // If true, players alternate placing pieces
    
    [Header("Visual Feedback")]
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;
    
    private ChessPiece selectedTrayPiece;
    private List<GameObject> placementIndicators = new List<GameObject>();
    private PieceColor currentPlacingPlayer = PieceColor.White;
    
    public static PlacementManager Instance { get; private set; }
    
    // Events
    public System.Action<PieceColor> OnPlayerTurnChanged;
    public System.Action OnPlacementPhaseComplete;
    
    // Public property to check if a piece is selected
    public bool HasSelectedTrayPiece => selectedTrayPiece != null;
    
    /// <summary>
    /// Get the valid X-layer for a piece color during placement phase
    /// White pieces can only be placed in X=0, Black pieces in X=3
    /// </summary>
    public static int GetValidXForColor(PieceColor color)
    {
        return color == PieceColor.White ? 0 : 3;
    }
    
    /// <summary>
    /// Check if a board position is in the correct X-layer for the given piece color
    /// </summary>
    public static bool IsPositionInColorZone(BoardPosition position, PieceColor color)
    {
        return position.x == GetValidXForColor(color);
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("PlacementManager: Instance created");
        }
        else
        {
            Debug.LogWarning("PlacementManager: Multiple instances detected, destroying duplicate");
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
    }
    
    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }
    
    /// <summary>
    /// Handle game state changes
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.PiecePlacement)
        {
            StartPlacementPhase();
        }
        else
        {
            EndPlacementPhase();
        }
    }
    
    /// <summary>
    /// Start the placement phase
    /// </summary>
    private void StartPlacementPhase()
    {
        Debug.Log("PlacementManager: Starting placement phase");
        
        // Ensure trays are visible during placement phase
        PieceTray.ShowAllTrays();
        Debug.Log("PlacementManager: Trays shown for placement phase");
        
        currentPlacingPlayer = PieceColor.White;
        OnPlayerTurnChanged?.Invoke(currentPlacingPlayer);
        
        // Trigger AI placement if the starting player is AI
        TriggerAIPlacementIfNeeded();
    }
    
    /// <summary>
    /// End the placement phase and clean up
    /// </summary>
    private void EndPlacementPhase()
    {
        Debug.Log("PlacementManager: Ending placement phase");
        
        if (selectedTrayPiece != null)
        {
            DeselectTrayPiece();
        }
        
        ClearPlacementIndicators();
        
        // Clean up any remaining repositioning tracking
        if (repositioningPieces.Count > 0)
        {
            Debug.Log($"PlacementManager: Cleaning up {repositioningPieces.Count} repositioning pieces");
            repositioningPieces.Clear();
        }
    }
    
    /// <summary>
    /// Handle clicking on a piece in a tray
    /// </summary>
    public void OnTrayPieceClicked(ChessPiece piece)
    {
        Debug.Log($"🎯 PlacementManager: Tray piece clicked - {piece.pieceColor} {piece.pieceType}");
        
        // DIAGNOSTIC: Log piece position BEFORE any processing
        Debug.Log($"🔍 BEFORE SELECTION - {piece.pieceColor} {piece.pieceType}:");
        Debug.Log($"    Transform position: {piece.transform.position}");
        Debug.Log($"    Transform local position: {piece.transform.localPosition}");  
        Debug.Log($"    Transform parent: {(piece.transform.parent != null ? piece.transform.parent.name : "None")}");
        Debug.Log($"    Is child of White Tray: {piece.transform.IsChildOf(PieceTray.WhiteTray?.transform)}");
        Debug.Log($"    Is child of Black Tray: {piece.transform.IsChildOf(PieceTray.BlackTray?.transform)}");
        
        if (!GameStateManager.Instance.CanPlacePieces())
        {
            Debug.LogWarning("PlacementManager: Cannot place pieces - not in placement phase");
            return;
        }
        
        // Check if it's this player's turn (if using alternating turns)
        if (alternatingTurns && piece.pieceColor != currentPlacingPlayer)
        {
            Debug.LogWarning($"PlacementManager: Not {piece.pieceColor}'s turn to place pieces");
            return;
        }
        
        // Select the piece for placement
        SelectTrayPiece(piece);
    }
    
    /// <summary>
    /// Select a piece from the tray for placement
    /// </summary>
    private void SelectTrayPiece(ChessPiece piece)
    {
        Debug.Log($"🔒 PlacementManager: Starting piece selection for {piece.pieceColor} {piece.pieceType}");
        
        // Deselect previous piece if any
        if (selectedTrayPiece != null)
        {
            DeselectTrayPiece();
        }
        
        // DIAGNOSTIC: Log piece position BEFORE selection
        Debug.Log($"🔍 BEFORE piece.OnSelected() - {piece.pieceColor} {piece.pieceType}:");
        Debug.Log($"    Transform position: {piece.transform.position}");
        Debug.Log($"    Transform local position: {piece.transform.localPosition}");
        
        selectedTrayPiece = piece;
        
        // Add visual feedback to selected piece
        piece.OnSelected();
        
        // DIAGNOSTIC: Log piece position AFTER OnSelected() 
        Debug.Log($"🔍 AFTER piece.OnSelected() - {piece.pieceColor} {piece.pieceType}:");
        Debug.Log($"    Transform position: {piece.transform.position}");
        Debug.Log($"    Transform local position: {piece.transform.localPosition}");
        Debug.Log($"    Transform parent: {(piece.transform.parent != null ? piece.transform.parent.name : "None")}");
        
        // Show valid placement positions
        ShowValidPlacementPositions();
        
        // DIAGNOSTIC: Log piece position AFTER ShowValidPlacementPositions()
        Debug.Log($"🔍 AFTER ShowValidPlacementPositions() - {piece.pieceColor} {piece.pieceType}:");
        Debug.Log($"    Transform position: {piece.transform.position}");
        Debug.Log($"    Transform local position: {piece.transform.localPosition}");
        
        Debug.Log($"✅ PlacementManager: Selected {piece.pieceColor} {piece.pieceType} for placement");
        
        // SAFETY CHECK: Validate tray pieces after selection to catch any positioning issues
        if (PieceTray.WhiteTray != null || PieceTray.BlackTray != null)
        {
            PieceTray.ValidateAndRecoverAllTrayPieces();
        }
        
        Debug.Log($"🔓 PlacementManager: Finished piece selection for {piece.pieceColor} {piece.pieceType}");
    }
    
    /// <summary>
    /// Deselect the currently selected tray piece
    /// </summary>
    public void DeselectTrayPiece()
    {
        if (selectedTrayPiece != null)
        {
            // If this was a repositioning piece that wasn't placed, restore it
            RestoreRepositioningPieceIfNeeded();
            
            selectedTrayPiece.OnDeselected();
            
            // Clear the selection
            ChessPiece previousSelection = selectedTrayPiece;
            selectedTrayPiece = null;
            
            ClearPlacementIndicators();
            Debug.Log($"PlacementManager: Deselected {previousSelection.pieceColor} {previousSelection.pieceType}");
        }
    }
    
    // Track pieces being repositioned to ensure they don't get lost
    private Dictionary<ChessPiece, BoardPosition> repositioningPieces = new Dictionary<ChessPiece, BoardPosition>();
    
    /// <summary>
    /// Handle clicking on a board piece during placement phase for repositioning
    /// </summary>
    public void OnBoardPieceClickedForRepositioning(ChessPiece piece)
    {
        if (piece == null)
        {
            Debug.LogError("PlacementManager: Cannot reposition null piece");
            return;
        }
        
        Debug.Log($"🔄 PlacementManager: Board piece clicked for repositioning - {piece.pieceColor} {piece.pieceType} at {piece.CurrentPosition}");
        
        // First, validate and repair position if needed
        if (!piece.ValidatePosition())
        {
            Debug.LogWarning($"🔄 Position validation failed for {piece.pieceColor} {piece.pieceType}, attempting repair...");
            if (!piece.RepairPosition())
            {
                Debug.LogError($"🔄 ❌ Cannot reposition piece - position repair failed for {piece.pieceColor} {piece.pieceType}");
                Debug.LogError($"🔄 Piece thinks it's at {piece.CurrentPosition}, but this position is not synchronized with the board");
                return;
            }
            Debug.Log($"🔄 ✅ Position repair successful for {piece.pieceColor} {piece.pieceType}");
        }
        
        // Store original position for recovery if needed (now guaranteed to be valid)
        BoardPosition originalPos = piece.CurrentPosition;
        if (!originalPos.IsValid())
        {
            Debug.LogError($"🔄 ❌ Cannot reposition piece with invalid position {originalPos} even after repair attempt");
            return;
        }
        
        Debug.Log($"🔄 Proceeding with repositioning {piece.pieceColor} {piece.pieceType} from validated position {originalPos}");
        
        // Check if this piece is already being repositioned (prevent double-processing)
        if (repositioningPieces.ContainsKey(piece))
        {
            Debug.LogWarning($"🔄 ⚠️ Piece {piece.pieceColor} {piece.pieceType} is already being repositioned - ignoring duplicate click");
            return;
        }
        
        // Check if this piece is already selected (deselect if so)
        if (selectedTrayPiece == piece)
        {
            Debug.Log($"🔄 Piece {piece.pieceColor} {piece.pieceType} is already selected - deselecting");
            DeselectTrayPiece();
            return;
        }
        
        // First, deselect any currently selected tray piece and restore previous repositioning piece if any
        if (selectedTrayPiece != null)
        {
            Debug.Log($"🔄 Deselecting previously selected piece: {selectedTrayPiece.pieceColor} {selectedTrayPiece.pieceType}");
            RestoreRepositioningPieceIfNeeded();
            DeselectTrayPiece();
        }
        
        // Store the original position for recovery
        repositioningPieces[piece] = originalPos;
        Debug.Log($"🔄 Stored original position {originalPos} for recovery");
        
        // NEW APPROACH: Keep piece in place, just highlight it
        // Do NOT remove from board until actually placing elsewhere
        // Do NOT change piece position - it stays where it is
        
        // Select this piece for placement (same as tray piece selection)
        selectedTrayPiece = piece;
        piece.OnSelected(); // This just adds visual highlighting
        
        // Show valid placement positions using green indicators
        ShowValidPlacementPositions();
        
        Debug.Log($"🔄 ✨ {piece.pieceColor} {piece.pieceType} highlighted for repositioning from {originalPos} (piece stays in place until placed)");
    }
    
    /// <summary>
    /// Restore a repositioning piece to its original position if placement was cancelled
    /// </summary>
    private void RestoreRepositioningPieceIfNeeded()
    {
        if (selectedTrayPiece != null && repositioningPieces.ContainsKey(selectedTrayPiece))
        {
            BoardPosition originalPos = repositioningPieces[selectedTrayPiece];
            Debug.Log($"🔄 PlacementManager: Cancelling repositioning for {selectedTrayPiece.pieceColor} {selectedTrayPiece.pieceType} (was at {originalPos})");
            
            // Since piece never moved during selection, no restoration needed!
            // Piece is already in correct position, just remove from tracking
            repositioningPieces.Remove(selectedTrayPiece);
            
            Debug.Log($"🔄 ✅ Repositioning cancelled - {selectedTrayPiece.pieceColor} {selectedTrayPiece.pieceType} remains at {originalPos}");
        }
    }
    
    /// <summary>
    /// Emergency cleanup method to recover any pieces that may be stuck in repositioning state
    /// </summary>
    public void CleanupRepositioningPieces()
    {
        Debug.Log($"🧹 PlacementManager: Emergency cleanup - checking {repositioningPieces.Count} repositioning pieces");
        
        var piecesToRestore = new List<ChessPiece>(repositioningPieces.Keys);
        foreach (var piece in piecesToRestore)
        {
            if (piece != null && repositioningPieces.ContainsKey(piece))
            {
                BoardPosition originalPos = repositioningPieces[piece];
                Debug.LogWarning($"🧹 Found stuck repositioning piece: {piece.pieceColor} {piece.pieceType} (original: {originalPos})");
                
                // Temporarily select the piece to restore it using existing logic
                ChessPiece previousSelection = selectedTrayPiece;
                selectedTrayPiece = piece;
                RestoreRepositioningPieceIfNeeded();
                selectedTrayPiece = previousSelection;
                
                Debug.Log($"🧹 Restored stuck piece: {piece.pieceColor} {piece.pieceType}");
            }
        }
        
        Debug.Log($"🧹 Cleanup complete - {repositioningPieces.Count} pieces remaining in repositioning state");
    }
    
    /// <summary>
    /// Show indicators for valid placement positions
    /// </summary>
    private void ShowValidPlacementPositions()
    {
        ClearPlacementIndicators();
        
        // CRITICAL: Clear any existing move indicators from InputManager to avoid yellow/green conflicts
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ClearMoveIndicators();
            Debug.Log("PlacementManager: Cleared conflicting move indicators to ensure consistent green placement indicators");
        }
        
        if (selectedTrayPiece == null || ChessBoard.Instance == null)
            return;
        
        Debug.Log("PlacementManager: Showing valid placement positions");
        
        // Get all positions that are valid for piece placement (empty squares only)
        var validPlacementPositions = GetValidPlacementPositions();
        
        foreach (var position in validPlacementPositions)
        {
            CreatePlacementIndicator(position);
        }
        
        Debug.Log($"PlacementManager: Created {placementIndicators.Count} placement indicators");
    }
    
    /// <summary>
    /// Get all valid positions for piece placement during placement phase.
    /// Uses strict placement validation - only empty squares in the correct Z-layer are allowed.
    /// </summary>
    private List<BoardPosition> GetValidPlacementPositions()
    {
        List<BoardPosition> validPositions = new List<BoardPosition>();
        
        if (ChessBoard.Instance == null)
        {
            Debug.LogError("PlacementManager: ChessBoard.Instance is null, cannot get valid placement positions");
            return validPositions;
        }
        
        if (selectedTrayPiece == null)
        {
            Debug.LogError("PlacementManager: No piece selected, cannot determine valid placement zone");
            return validPositions;
        }
        
        // Get the valid X-layer for the selected piece color
        int validX = GetValidXForColor(selectedTrayPiece.pieceColor);
        
        Debug.Log($"PlacementManager: Finding valid positions for {selectedTrayPiece.pieceColor} piece in X-layer {validX}");
        
        // BOARD STATE DIAGNOSTIC: Check if board is in expected state for initial placement
        Debug.Log($"🔍 BOARD STATE: About to validate placement positions");
        Debug.Log($"🔍 Selected piece: {selectedTrayPiece.pieceColor} {selectedTrayPiece.pieceType}");
        Debug.Log($"🔍 Piece CurrentPosition: {selectedTrayPiece.CurrentPosition}");
        Debug.Log($"🔍 Piece transform parent: {(selectedTrayPiece.transform.parent != null ? selectedTrayPiece.transform.parent.name : "None")}");
        
        // CRITICAL DIAGNOSTIC: Check if tray pieces have invalid positions as they should
        bool isTrayPiece = selectedTrayPiece.transform.IsChildOf(PieceTray.WhiteTray?.transform) || 
                          selectedTrayPiece.transform.IsChildOf(PieceTray.BlackTray?.transform);
        if (isTrayPiece && selectedTrayPiece.CurrentPosition.IsValid())
        {
            Debug.LogError($"🚨 INVALID STATE: Tray piece {selectedTrayPiece.pieceColor} {selectedTrayPiece.pieceType} has valid CurrentPosition {selectedTrayPiece.CurrentPosition}!");
            Debug.LogError($"🚨 Tray pieces should have INVALID positions, not valid board positions!");
            Debug.LogError($"🚨 This causes position repair to incorrectly place tray pieces on board!");
        }
        
        // Check what's actually on the board
        int occupiedPositions = 0;
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    BoardPosition pos = new BoardPosition(x, y, z);
                    ChessPiece pieceAt = ChessBoard.Instance.GetPieceAt(pos);
                    if (pieceAt != null)
                    {
                        occupiedPositions++;
                        Debug.LogWarning($"🔍 OCCUPIED: Position {pos} has {pieceAt.pieceColor} {pieceAt.pieceType}");
                        Debug.LogWarning($"🔍   Piece parent: {(pieceAt.transform.parent != null ? pieceAt.transform.parent.name : "None")}");
                        Debug.LogWarning($"🔍   Piece CurrentPosition: {pieceAt.CurrentPosition}");
                        
                        // Check if this is a tray piece incorrectly placed on board
                        bool isPieceInTray = pieceAt.transform.IsChildOf(PieceTray.WhiteTray?.transform) || 
                                           pieceAt.transform.IsChildOf(PieceTray.BlackTray?.transform);
                        if (isPieceInTray)
                        {
                            Debug.LogError($"🚨 CRITICAL: Piece at {pos} is STILL IN TRAY but registered on board!");
                            Debug.LogError($"🚨 This is the root cause of the missing 16th placement position!");
                        }
                    }
                }
            }
        }
        Debug.Log($"🔍 BOARD STATE: {occupiedPositions} positions occupied on board (should be 0 for initial placement)");
        
        // Check only positions in the correct X-layer for this piece color
        int positionsChecked = 0;
        int positionsValidated = 0;
        int positionsAdded = 0;
        
        for (int y = 0; y < 4; y++)
        {
            for (int z = 0; z < 4; z++)
            {
                // Only check the valid X-layer for this piece color
                BoardPosition position = new BoardPosition(validX, y, z);
                positionsChecked++;
                
                Debug.Log($"🔍 LOOP: Checking position {position} (iteration {positionsChecked}/16)");
                
                // Check if position is empty and in correct X-layer (enhanced placement validation)
                bool canPlace = ChessBoard.Instance.CanPlacePieceAt(position, selectedTrayPiece.pieceColor);
                Debug.Log($"🔍 VALIDATION: CanPlacePieceAt({position}) = {canPlace}");
                
                if (canPlace)
                {
                    positionsValidated++;
                    Debug.Log($"🔍 ADDING: Adding position {position} to validPositions list (#{positionsValidated})");
                    
                    int countBefore = validPositions.Count;
                    validPositions.Add(position);
                    int countAfter = validPositions.Count;
                    positionsAdded++;
                    
                    Debug.Log($"🔍 LIST STATE: validPositions count before={countBefore}, after={countAfter}, total added={positionsAdded}");
                    
                    // Special tracking for the problematic position
                    if (position.x == 0 && position.y == 3 && position.z == 3)
                    {
                        Debug.LogWarning($"🎯 SPECIAL: Position (0,3,3) successfully added to list at index {countAfter - 1}");
                    }
                }
                else
                {
                    Debug.Log($"🔍 SKIPPED: Position {position} failed validation, not adding to list");
                }
            }
        }
        
        Debug.Log($"🔍 SUMMARY: Checked {positionsChecked} positions, {positionsValidated} validated, {positionsAdded} added");
        Debug.Log($"🔍 FINAL: validPositions.Count = {validPositions.Count}");
        
        // Log all positions in the final list
        Debug.Log($"🔍 FINAL LIST CONTENTS:");
        for (int i = 0; i < validPositions.Count; i++)
        {
            Debug.Log($"  [{i}]: {validPositions[i]}");
        }
        
        Debug.Log($"PlacementManager: Found {validPositions.Count} valid placement positions for {selectedTrayPiece.pieceColor} in X={validX} layer");
        return validPositions;
    }
    
    /// <summary>
    /// Create a visual indicator for a valid placement position
    /// </summary>
    private void CreatePlacementIndicator(BoardPosition position)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = $"PlacementIndicator_{position.x}_{position.y}_{position.z}";
        
        // CRITICAL FIX: Parent indicator to board container so it rotates with the board
        GameObject piecesContainer = GameObject.Find("Pieces Container");
        if (piecesContainer != null)
        {
            indicator.transform.SetParent(piecesContainer.transform);
            
            // Use local positioning relative to the board container
            if (ChessBoard.Instance != null)
            {
                Vector3 localPos = ChessBoard.Instance.BoardToLocalPosition(position);
                indicator.transform.localPosition = localPos + Vector3.up * 0.5f; // Slightly above floor
                
                // DIAGNOSTIC: Log comprehensive position information
                Vector3 finalLocalPos = indicator.transform.localPosition;
                Vector3 finalWorldPos = indicator.transform.position;
                Debug.Log($"PlacementManager: Created indicator for {position}");
                Debug.Log($"  Calculated local position: {localPos}");
                Debug.Log($"  Final local position (with Y offset): {finalLocalPos}");
                Debug.Log($"  Final world position: {finalWorldPos}");
                Debug.Log($"  Distance from origin: {finalWorldPos.magnitude:F2}");
                
                // BOUNDS CHECK: Warn if indicator is positioned extremely far from board center
                if (finalWorldPos.magnitude > 20f)
                {
                    Debug.LogWarning($"⚠️ PlacementManager: Indicator for {position} is very far from board center!");
                    Debug.LogWarning($"   World position: {finalWorldPos} (distance: {finalWorldPos.magnitude:F2})");
                    Debug.LogWarning($"   This may cause visibility or interaction issues");
                }
                
                // SPECIAL CASE: Check for high-elevation positions that might be out of view
                if (finalLocalPos.y > 5f)
                {
                    Debug.LogWarning($"⚠️ PlacementManager: High elevation detected for {position}!");
                    Debug.LogWarning($"   Local Y position: {finalLocalPos.y} (may be outside camera view)");
                    Debug.LogWarning($"   Consider camera settings if not visible");
                }
            }
        }
        else
        {
            Debug.LogWarning("PlacementManager: Pieces Container not found - indicators may not rotate with board");
            
            // Fallback to world positioning if container not found
            if (ChessBoard.Instance != null)
            {
                Vector3 worldPos = ChessBoard.Instance.BoardToWorld(position);
                indicator.transform.position = worldPos + Vector3.up * 0.5f; // Slightly above floor
            }
        }
        
        // Make it small and unobtrusive
        indicator.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        // Ensure rotation is normalized to prevent quaternion warnings
        indicator.transform.rotation = Quaternion.identity;
        
        // Remove collider and add placement data component
        Collider indicatorCollider = indicator.GetComponent<Collider>();
        if (indicatorCollider != null)
        {
            DestroyImmediate(indicatorCollider);
        }
        
        // Add a larger invisible collider for easier clicking
        SphereCollider clickCollider = indicator.AddComponent<SphereCollider>();
        clickCollider.radius = 2f; // Much larger for easier clicking
        
        // Add component to identify this as a placement target
        PlacementTargetData targetData = indicator.AddComponent<PlacementTargetData>();
        targetData.targetPosition = position;
        
        // Apply material
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (validPlacementMaterial == null)
        {
            validPlacementMaterial = CreatePlacementMaterial(Color.green);
        }
        renderer.material = validPlacementMaterial;
        
        placementIndicators.Add(indicator);
    }
    
    /// <summary>
    /// Create a material for placement indicators
    /// </summary>
    private Material CreatePlacementMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        material.SetFloat("_Smoothness", 0.8f);
        return material;
    }
    
    /// <summary>
    /// Clear all placement indicators
    /// </summary>
    private void ClearPlacementIndicators()
    {
        foreach (var indicator in placementIndicators)
        {
            if (indicator != null)
            {
                DestroyImmediate(indicator);
            }
        }
        placementIndicators.Clear();
    }
    
    /// <summary>
    /// Handle clicking on a placement indicator
    /// </summary>
    public void OnPlacementTargetClicked(BoardPosition targetPosition)
    {
        Debug.Log($"PlacementManager: Placement target clicked at {targetPosition}");
        
        if (selectedTrayPiece == null)
        {
            Debug.LogWarning("PlacementManager: No piece selected for placement");
            return;
        }
        
        if (!GameStateManager.Instance.CanPlacePieces())
        {
            Debug.LogWarning("PlacementManager: Cannot place pieces - not in placement phase");
            return;
        }
        
        // Attempt to place the piece
        PlacePieceOnBoard(selectedTrayPiece, targetPosition);
    }
    
    /// <summary>
    /// Place a piece from the tray onto the board
    /// </summary>
    private void PlacePieceOnBoard(ChessPiece piece, BoardPosition position)
    {
        Debug.Log($"PlacementManager: Placing {piece.pieceColor} {piece.pieceType} at {position}");
        
        // COMPREHENSIVE BOARD STATE DEBUG: Log current board state and tray contents
        DebugBoardAndTrayState(piece, position);
        
        // BOARD STATE CONSISTENCY CHECK: Verify board array matches piece positions
        ValidateBoardConsistency(piece);
        
        // DUPLICATE PIECE VALIDATION: Check for illegal duplicate placements
        if (!ValidateNoDuplicatePlacement(piece, position))
        {
            Debug.LogError($"❌ PlacementManager: BLOCKED - Cannot place duplicate {piece.pieceColor} {piece.pieceType}");
            return;
        }
        
        // Check if this is a repositioning piece that needs special handling
        bool isRepositioningPiece = repositioningPieces.ContainsKey(piece);
        BoardPosition originalPosition = new BoardPosition(-1, -1, -1); // Invalid position
        ChessPiece originalPositionPiece = null;
        
        if (isRepositioningPiece)
        {
            originalPosition = repositioningPieces[piece];
            Debug.Log($"🔄 PlacementManager: This is a repositioning piece from {originalPosition} to {position}");
            
            // CRITICAL FIX: Temporarily clear the original position for clean validation
            // This prevents the repositioning piece from interfering with placement validation
            originalPositionPiece = ChessBoard.Instance.GetPieceAt(originalPosition);
            if (originalPositionPiece == piece)
            {
                ChessBoard.Instance.SetPieceAt(originalPosition, null);
                Debug.Log($"🔄 Temporarily cleared original position {originalPosition} for clean validation");
            }
        }
        
        // Now run clean validation without repositioning interference
        if (!ChessBoard.Instance.CanPlacePieceAt(position, piece.pieceColor))
        {
            // RESTORE: Put piece back at original position if validation fails
            if (isRepositioningPiece && originalPositionPiece == piece)
            {
                ChessBoard.Instance.SetPieceAt(originalPosition, piece);
                Debug.Log($"🔄 Restored piece to original position {originalPosition} after validation failure");
            }
            
            // ENHANCED ERROR REPORTING: Provide detailed reason for validation failure
            Debug.LogError($"❌ PlacementManager: PLACEMENT BLOCKED for {piece.pieceColor} {piece.pieceType} at {position}");
            Debug.LogError($"❌ Detailed validation failure analysis:");
            Debug.LogError($"   - Target position: {position}");
            Debug.LogError($"   - Required X-layer for {piece.pieceColor}: {GetValidXForColor(piece.pieceColor)}");
            Debug.LogError($"   - Actual X-layer: {position.x}");
            Debug.LogError($"   - Position in bounds: {ChessBoard.Instance.IsValidPosition(position)}");
            
            ChessPiece positionPiece = ChessBoard.Instance.GetPieceAt(position);
            if (positionPiece != null)
            {
                Debug.LogError($"   - Position occupied by: {positionPiece.pieceColor} {positionPiece.pieceType}");
            }
            else
            {
                Debug.LogError($"   - Position is empty");
            }
            
            if (isRepositioningPiece)
            {
                Debug.LogError($"   - This was a repositioning piece from: {originalPosition}");
            }
            else
            {
                Debug.LogError($"   - This was a fresh placement from tray");
            }
            
            return;
        }
        
        // Double-check that position is truly empty after cleaning repositioning interference
        ChessPiece existingPiece = ChessBoard.Instance.GetPieceAt(position);
        if (existingPiece != null)
        {
            // RESTORE: Put piece back at original position if there's still a conflict
            if (isRepositioningPiece && originalPositionPiece == piece)
            {
                ChessBoard.Instance.SetPieceAt(originalPosition, piece);
                Debug.Log($"🔄 Restored piece to original position {originalPosition} after conflict detection");
            }
            
            // ENHANCED ERROR REPORTING: Detailed conflict analysis
            Debug.LogError($"❌ PlacementManager: POSITION CONFLICT DETECTED");
            Debug.LogError($"❌ Attempted to place: {piece.pieceColor} {piece.pieceType} at {position}");
            Debug.LogError($"❌ Position occupied by: {existingPiece.pieceColor} {existingPiece.pieceType}");
            Debug.LogError($"❌ Conflict analysis:");
            Debug.LogError($"   - Same piece? {existingPiece == piece}");
            Debug.LogError($"   - Same color? {existingPiece.pieceColor == piece.pieceColor}");
            Debug.LogError($"   - Existing piece position: {existingPiece.CurrentPosition}");
            Debug.LogError($"   - Placement position: {position}");
            
            if (isRepositioningPiece)
            {
                Debug.LogError($"   - Repositioning from: {originalPosition}");
                Debug.LogError($"   - Original position piece: {(originalPositionPiece != null ? $"{originalPositionPiece.pieceColor} {originalPositionPiece.pieceType}" : "NULL")}");
            }
            
            return;
        }
        
        Debug.Log($"✅ PlacementManager: Placement validation passed - position {position} is empty and valid");
        
        // Remove piece from tray only if it's not a repositioning piece
        if (!isRepositioningPiece)
        {
            PieceTray tray = (piece.pieceColor == PieceColor.White) ? PieceTray.WhiteTray : PieceTray.BlackTray;
            if (tray != null)
            {
                tray.RemovePiece(piece);
                Debug.Log($"PlacementManager: Removed {piece.pieceColor} {piece.pieceType} from tray");
            }
        }
        else
        {
            // This is a repositioning piece - original position was already cleared during validation
            Debug.Log($"🔄 PlacementManager: Completing repositioning from {originalPosition} to {position}");
            
            // The original position was already cleared during validation above
            // Just remove from repositioning tracking since it's being successfully placed
            repositioningPieces.Remove(piece);
            Debug.Log($"🔄 ✅ Completed repositioning of {piece.pieceColor} {piece.pieceType} from {originalPosition} to {position}");
        }
        
        // Find the pieces container to parent the piece to
        GameObject piecesContainer = GameObject.Find("Pieces Container");
        if (piecesContainer != null)
        {
            piece.transform.SetParent(piecesContainer.transform);
        }
        
        // Initialize the piece at the board position
        piece.Initialize(position, piece.pieceColor);
        
        // Deselect the piece
        DeselectTrayPiece();
        
        // Switch turns if using alternating placement
        if (alternatingTurns)
        {
            SwitchPlacingPlayer();
        }
        
        // Check if placement phase should end
        CheckPlacementCompletion();
        
        Debug.Log($"PlacementManager: Successfully placed {piece.pieceColor} {piece.pieceType} at {position}");
    }
    
    /// <summary>
    /// Switch to the other player for alternating placement
    /// </summary>
    private void SwitchPlacingPlayer()
    {
        currentPlacingPlayer = (currentPlacingPlayer == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        OnPlayerTurnChanged?.Invoke(currentPlacingPlayer);
        Debug.Log($"PlacementManager: Switched to {currentPlacingPlayer}'s turn");
        
        // Trigger AI placement if the new current player is AI
        TriggerAIPlacementIfNeeded();
    }
    
    /// <summary>
    /// Check if the placement phase should be completed
    /// </summary>
    private void CheckPlacementCompletion()
    {
        // For now, let players manually transition to play phase
        // In the future, this could automatically transition when certain conditions are met
        Debug.Log("PlacementManager: Checking placement completion...");
        
        // Could check if both trays are empty, or if players have placed minimum pieces, etc.
    }
    
    /// <summary>
    /// Manually complete the placement phase and start gameplay
    /// </summary>
    public void CompletePlacementPhase()
    {
        Debug.Log("🚀 PlacementManager: ==== COMPLETING PLACEMENT PHASE ====");
        Debug.Log($"🚀 PlacementManager: Current GameState: {(GameStateManager.Instance != null ? GameStateManager.Instance.currentState.ToString() : "NULL")}");
        
        // DEBUG: Check what pieces are currently on the board before validation
        if (ChessBoard.Instance != null)
        {
            int totalPieces = 0;
            int whitePieces = 0;
            int blackPieces = 0;
            
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        ChessPiece piece = ChessBoard.Instance.GetPieceAt(new BoardPosition(x, y, z));
                        if (piece != null)
                        {
                            totalPieces++;
                            if (piece.pieceColor == PieceColor.White) whitePieces++;
                            else if (piece.pieceColor == PieceColor.Black) blackPieces++;
                            Debug.Log($"🚀 BOARD SCAN: Found {piece.pieceColor} {piece.pieceType} at ({x},{y},{z})");
                        }
                    }
                }
            }
            
            Debug.Log($"🚀 BOARD STATE: Total pieces: {totalPieces}, White: {whitePieces}, Black: {blackPieces}");
        }
        else
        {
            Debug.LogError("🚀 ❌ ChessBoard.Instance is NULL during completion!");
        }
        
        // Validate that the game setup is valid before transitioning
        Debug.Log("🚀 PlacementManager: Validating game setup...");
        if (!ValidateGameSetup())
        {
            Debug.LogError("🚀 ❌ PlacementManager: Cannot start game - invalid setup");
            return;
        }
        Debug.Log("🚀 ✅ PlacementManager: Game setup validation passed");
        
        // Clear any remaining placement indicators
        Debug.Log("🚀 PlacementManager: Clearing placement indicators...");
        ClearPlacementIndicators();
        
        // Clear any selected pieces
        Debug.Log("🚀 PlacementManager: Deselecting any selected pieces...");
        DeselectTrayPiece();
        
        // Spawn pawns in front of all placed pieces
        Debug.Log("🚀 PlacementManager: About to call SpawnPawnsForGameplay()...");
        SpawnPawnsForGameplay();
        Debug.Log("🚀 PlacementManager: SpawnPawnsForGameplay() completed");
        
        // Evaluate check states after all pieces are placed and pawns spawned
        Debug.Log("🚀 PlacementManager: Evaluating initial check states...");
        EvaluatePostPlacementCheckStates();
        
        Debug.Log("🚀 PlacementManager: Transitioning to Playing state");
        
        // Hide trays during gameplay for clearer board view
        PieceTray.HideAllTrays();
        Debug.Log("🚀 PlacementManager: Trays hidden for gameplay");
        
        OnPlacementPhaseComplete?.Invoke();
        Debug.Log("🚀 PlacementManager: OnPlacementPhaseComplete event invoked");
        
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameState.Playing);
            Debug.Log($"🚀 PlacementManager: GameState changed to: {GameStateManager.Instance.currentState}");
        }
        else
        {
            Debug.LogError("🚀 ❌ PlacementManager: GameStateManager.Instance is null!");
        }
        
        Debug.Log("🚀 PlacementManager: ==== PLACEMENT PHASE COMPLETION FINISHED ====");
    }
    
    /// <summary>
    /// Evaluate check states immediately after placement to determine opening turn order
    /// </summary>
    private void EvaluatePostPlacementCheckStates()
    {
        if (CheckDetectionManager.Instance == null)
        {
            Debug.LogError("🚀 ❌ PlacementManager: CheckDetectionManager.Instance is null - cannot evaluate check states");
            return;
        }
        
        Debug.Log("🚀 📋 PlacementManager: Evaluating initial check states after placement...");
        
        // Force a fresh evaluation of check states with all pieces on board
        CheckDetectionManager.Instance.InvalidateCache();
        
        // Get the current check states
        bool whiteInCheck = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.White);
        
        // CRITICAL FIX: Invalidate cache again before Black king check to ensure fresh evaluation
        CheckDetectionManager.Instance.InvalidateCache();
        bool blackInCheck = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.Black);
        
        Debug.Log($"🚀 📋 PlacementManager: Post-placement check states - White: {whiteInCheck}, Black: {blackInCheck}");
        
        // Initialize UI state with safe method and update visual feedback
        CheckDetectionManager.Instance.InitializeUIStateWithInitialChecks(whiteInCheck, blackInCheck);
        
        // Also trigger safe visual feedback update to ensure UI is in sync
        CheckDetectionManager.Instance.SafeUpdateVisualFeedback();
        
        // Store the initial check states for TurnManager to use
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.SetInitialCheckStates(whiteInCheck, blackInCheck);
            Debug.Log("🚀 📋 PlacementManager: Initial check states sent to TurnManager");
        }
        else
        {
            Debug.LogError("🚀 ❌ PlacementManager: TurnManager.Instance is null - cannot set initial check states");
        }
    }
    
    /// <summary>
    /// Validate that both players have pieces on the board
    /// </summary>
    private bool ValidateGameSetup()
    {
        if (ChessBoard.Instance == null)
        {
            Debug.LogError("PlacementManager: ChessBoard not available for validation");
            return false;
        }
        
        bool hasWhitePieces = false;
        bool hasBlackPieces = false;
        int totalPieces = 0;
        
        // Check all board positions for pieces
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(new BoardPosition(x, y, z));
                    if (piece != null)
                    {
                        totalPieces++;
                        if (piece.pieceColor == PieceColor.White)
                            hasWhitePieces = true;
                        else if (piece.pieceColor == PieceColor.Black)
                            hasBlackPieces = true;
                    }
                }
            }
        }
        
        Debug.Log($"PlacementManager: Game setup validation - Total pieces: {totalPieces}, White: {hasWhitePieces}, Black: {hasBlackPieces}");
        
        if (!hasWhitePieces)
        {
            Debug.LogError("PlacementManager: No White pieces found on board");
            return false;
        }
        
        if (!hasBlackPieces)
        {
            Debug.LogError("PlacementManager: No Black pieces found on board");
            return false;
        }
        
        if (totalPieces < 2)
        {
            Debug.LogError("PlacementManager: Not enough pieces on board for gameplay");
            return false;
        }
        
        Debug.Log("PlacementManager: ✅ Game setup validation passed");
        return true;
    }
    
    /// <summary>
    /// Reset all pieces back to trays
    /// </summary>
    public void ResetToTrays()
    {
        Debug.Log("PlacementManager: Resetting all pieces to trays");
        
        // Clear any selection and indicators first
        DeselectTrayPiece();
        ClearPlacementIndicators();
        
        // Find all pieces on the board and move them back to trays
        List<ChessPiece> boardPieces = new List<ChessPiece>();
        
        // Only collect pieces that are actually on the board (have valid positions)
        if (ChessBoard.Instance != null)
        {
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        BoardPosition pos = new BoardPosition(x, y, z);
                        ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);
                        if (piece != null)
                        {
                            boardPieces.Add(piece);
                        }
                    }
                }
            }
        }
        
        Debug.Log($"PlacementManager: Found {boardPieces.Count} pieces on board to reset");
        
        // Move each board piece back to its tray
        foreach (ChessPiece piece in boardPieces)
        {
            Debug.Log($"PlacementManager: Moving {piece.pieceColor} {piece.pieceType} from board to tray");
            
            // Remove from board
            if (piece.CurrentPosition.IsValid())
            {
                ChessBoard.Instance.SetPieceAt(piece.CurrentPosition, null);
            }
            
            // Reset piece state
            piece.ResetPosition(); // This should invalidate CurrentPosition
            
            // Add back to appropriate tray
            PieceTray tray = (piece.pieceColor == PieceColor.White) ? PieceTray.WhiteTray : PieceTray.BlackTray;
            if (tray != null)
            {
                bool success = tray.AddPiece(piece);
                if (success)
                {
                    Debug.Log($"PlacementManager: Successfully moved {piece.pieceColor} {piece.pieceType} to tray");
                }
                else
                {
                    Debug.LogError($"PlacementManager: Failed to add {piece.pieceColor} {piece.pieceType} to tray - tray may be full");
                }
            }
            else
            {
                Debug.LogError($"PlacementManager: No tray found for {piece.pieceColor} pieces");
            }
        }
        
        // Ensure trays are visible after reset
        PieceTray.ShowAllTrays();
        
        // Reset placing player and ensure we're in placement state
        currentPlacingPlayer = PieceColor.White;
        OnPlayerTurnChanged?.Invoke(currentPlacingPlayer);
        
        // Ensure we're in placement phase
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameState.PiecePlacement);
        }
        
        Debug.Log("PlacementManager: Reset to trays completed");
    }
    
    /// <summary>
    /// Spawn pawns in front of all placed pieces when transitioning to gameplay
    /// </summary>
    private void SpawnPawnsForGameplay()
    {
        Debug.Log("Spawning pawns for gameplay...");
        
        // ANTI-CORRUPTION: First, clean up any existing pawns to prevent duplicates
        CleanUpExistingPawns();
        
        // Validate ChessBoard
        if (ChessBoard.Instance == null)
        {
            Debug.LogError("PlacementManager: ChessBoard.Instance is null, cannot spawn pawns");
            return;
        }
        
        // Validate GameManager and pawn prefab
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("PlacementManager: GameManager not found");
            return;
        }
        
        if (gameManager.pawnPiecePrefab == null)
        {
            Debug.LogError("PlacementManager: pawnPiecePrefab is null");
            return;
        }
        
        int pawnCount = 0;
        List<BoardPosition> pawnPositions = new List<BoardPosition>(); // Track to prevent duplicates
        
        // Find all pieces on the board and spawn pawns in front of them
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    BoardPosition position = new BoardPosition(x, y, z);
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(position);
                    
                    if (piece != null && piece.pieceType != ChessPieceType.Pawn)
                    {
                        // Calculate position directly in front of the piece based on color
                        BoardPosition pawnPosition = GetPawnSpawnPosition(position, piece.pieceColor);
                        
                        // Validate pawn spawn position and check for duplicates
                        if (pawnPosition.IsValid() && 
                            ChessBoard.Instance.GetPieceAt(pawnPosition) == null &&
                            !pawnPositions.Contains(pawnPosition))
                        {
                            SpawnPawn(pawnPosition, piece.pieceColor);
                            pawnPositions.Add(pawnPosition);
                            pawnCount++;
                        }
                        else if (pawnPositions.Contains(pawnPosition))
                        {
                            Debug.LogWarning($"PlacementManager: Skipping duplicate pawn spawn at {pawnPosition}");
                        }
                    }
                }
            }
        }
        
        Debug.Log($"Pawn spawning complete: {pawnCount} pawns spawned");
    }
    
    /// <summary>
    /// Clean up any existing pawns before spawning new ones to prevent duplicates
    /// </summary>
    private void CleanUpExistingPawns()
    {
        Debug.Log("PlacementManager: Cleaning up existing pawns...");
        
        // Find all pawns in the scene
        Pawn[] allPawns = FindObjectsByType<Pawn>(FindObjectsSortMode.None);
        
        int removedCount = 0;
        foreach (Pawn pawn in allPawns)
        {
            if (pawn != null && pawn.gameObject != null)
            {
                // Remove pawn from board if registered
                if (pawn.CurrentPosition.IsValid() && ChessBoard.Instance != null)
                {
                    ChessPiece boardPiece = ChessBoard.Instance.GetPieceAt(pawn.CurrentPosition);
                    if (boardPiece == pawn)
                    {
                        ChessBoard.Instance.SetPieceAt(pawn.CurrentPosition, null);
                    }
                }
                
                Debug.Log($"PlacementManager: Removing existing {pawn.pieceColor} pawn");
                DestroyImmediate(pawn.gameObject);
                removedCount++;
            }
        }
        
        Debug.Log($"PlacementManager: Removed {removedCount} existing pawns");
    }
    
    /// <summary>
    /// Calculate the position directly in front of a piece based on color
    /// White pieces advance +X, Black pieces advance -X
    /// </summary>
    private BoardPosition GetPawnSpawnPosition(BoardPosition piecePosition, PieceColor color)
    {
        int forwardX = (color == PieceColor.White) ? 1 : -1;
        BoardPosition pawnPosition = new BoardPosition(
            piecePosition.x + forwardX,
            piecePosition.y,
            piecePosition.z
        );
        
        
        return pawnPosition;
    }
    
    /// <summary>
    /// Spawn a single pawn at the specified position
    /// </summary>
    private void SpawnPawn(BoardPosition position, PieceColor color)
    {
        // Get GameManager
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("SpawnPawn: GameManager not found");
            return;
        }
        
        if (gameManager.pawnPiecePrefab == null)
        {
            Debug.LogError("SpawnPawn: pawnPiecePrefab is null");
            return;
        }
        
        // Instantiate pawn from GameManager's prefab
        GameObject pawnObject = Instantiate(gameManager.pawnPiecePrefab);
        
        pawnObject.name = $"{color} Pawn at {position}";
        
        // Get the Pawn component
        Pawn pawn = pawnObject.GetComponent<Pawn>();
        if (pawn == null)
        {
            Debug.LogError("SpawnPawn: Spawned object missing Pawn component");
            DestroyImmediate(pawnObject);
            return;
        }
        
        // Find the pieces container to parent the pawn to
        GameObject piecesContainer = GameObject.Find("Pieces Container");
        if (piecesContainer != null)
        {
            pawnObject.transform.SetParent(piecesContainer.transform);
        }
        else
        {
            Debug.LogWarning("SpawnPawn: Pieces Container not found, pawn may not rotate with board");
        }
        
        // Initialize the pawn at the board position
        pawn.Initialize(position, color);
        
        // CRITICAL: Activate the pawn object to make it visible
        pawnObject.SetActive(true);
    }
    
    // === AI PLACEMENT INTEGRATION ===
    
    /// <summary>
    /// Trigger AI placement if the current placing player is an AI
    /// </summary>
    private void TriggerAIPlacementIfNeeded()
    {
        // Only trigger AI if alternating turns is enabled or if we're in a specific AI mode
        if (!alternatingTurns && !ShouldUseAIPlacement())
        {
            return;
        }
        
        // Check if current placing player is AI
        if (TurnManager.Instance != null && TurnManager.Instance.IsPlayerAI(currentPlacingPlayer))
        {
            Debug.Log($"PlacementManager: Triggering AI placement for {currentPlacingPlayer}");
            
            // Request placement from AI player
            if (AIPlayer.Instance != null)
            {
                AIPlayer.Instance.RequestPlacement(currentPlacingPlayer);
            }
            else
            {
                Debug.LogError("PlacementManager: AIPlayer.Instance is null, cannot trigger AI placement");
            }
        }
    }
    
    /// <summary>
    /// Check if we should use AI placement based on game configuration
    /// </summary>
    private bool ShouldUseAIPlacement()
    {
        // Use AI placement for AI vs AI games even without alternating turns
        if (TurnManager.Instance != null)
        {
            bool whiteIsAI = TurnManager.Instance.IsPlayerAI(PieceColor.White);
            bool blackIsAI = TurnManager.Instance.IsPlayerAI(PieceColor.Black);
            
            // If both players are AI, use automatic placement
            if (whiteIsAI && blackIsAI)
            {
                return true;
            }
            
            // If current player is AI and it's their turn, use automatic placement
            if (TurnManager.Instance.IsPlayerAI(currentPlacingPlayer))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the current placing player (for external access)
    /// </summary>
    public PieceColor GetCurrentPlacingPlayer()
    {
        return currentPlacingPlayer;
    }
    
    /// <summary>
    /// Check if a specific player is currently placing
    /// </summary>
    public bool IsPlayerPlacing(PieceColor playerColor)
    {
        return currentPlacingPlayer == playerColor;
    }
    
    /// <summary>
    /// Debug board and tray state to help identify piece placement issues
    /// </summary>
    private void DebugBoardAndTrayState(ChessPiece targetPiece, BoardPosition targetPosition)
    {
        Debug.Log($"🔍 === BOARD & TRAY STATE DEBUG ===");
        Debug.Log($"🔍 Target: Place {targetPiece.pieceColor} {targetPiece.pieceType} at {targetPosition}");
        Debug.Log($"🔍 Target piece position: {targetPiece.transform.position}");
        Debug.Log($"🔍 Target piece parent: {(targetPiece.transform.parent != null ? targetPiece.transform.parent.name : "None")}");
        Debug.Log($"🔍 Target piece CurrentPosition: {targetPiece.CurrentPosition}");
        
        // Check what's at the target position
        if (ChessBoard.Instance != null)
        {
            ChessPiece existingPiece = ChessBoard.Instance.GetPieceAt(targetPosition);
            Debug.Log($"🔍 Target position {targetPosition} contains: {(existingPiece != null ? $"{existingPiece.pieceColor} {existingPiece.pieceType}" : "EMPTY")}");
            
            if (existingPiece != null)
            {
                Debug.Log($"🔍 Existing piece transform: {existingPiece.transform.position}");
                Debug.Log($"🔍 Existing piece parent: {(existingPiece.transform.parent != null ? existingPiece.transform.parent.name : "None")}");
                Debug.Log($"🔍 Same piece object? {existingPiece == targetPiece}");
            }
        }
        
        // Count pieces of same type/color on board
        int sameTypeCount = 0;
        if (ChessBoard.Instance != null)
        {
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        ChessPiece boardPiece = ChessBoard.Instance.GetPieceAt(new BoardPosition(x, y, z));
                        if (boardPiece != null && 
                            boardPiece.pieceColor == targetPiece.pieceColor && 
                            boardPiece.pieceType == targetPiece.pieceType)
                        {
                            sameTypeCount++;
                            Debug.Log($"🔍 Found {boardPiece.pieceColor} {boardPiece.pieceType} at ({x},{y},{z})");
                        }
                    }
                }
            }
        }
        Debug.Log($"🔍 Total {targetPiece.pieceColor} {targetPiece.pieceType} pieces on board: {sameTypeCount}");
        
        // Check tray contents
        if (PieceTray.WhiteTray != null)
        {
            var whitePieces = PieceTray.WhiteTray.GetPieces();
            int whiteQueens = 0;
            foreach (var piece in whitePieces)
            {
                if (piece.pieceType == ChessPieceType.Queen) whiteQueens++;
            }
            Debug.Log($"🔍 White tray contains {whiteQueens} Queens out of {whitePieces.Count} total pieces");
        }
        
        if (PieceTray.BlackTray != null)
        {
            var blackPieces = PieceTray.BlackTray.GetPieces();
            int blackQueens = 0;
            foreach (var piece in blackPieces)
            {
                if (piece.pieceType == ChessPieceType.Queen) blackQueens++;
            }
            Debug.Log($"🔍 Black tray contains {blackQueens} Queens out of {blackPieces.Count} total pieces");
        }
        
        Debug.Log($"🔍 === END BOARD & TRAY STATE DEBUG ===");
    }
    
    /// <summary>
    /// Validate that we're not trying to place a duplicate piece
    /// </summary>
    private bool ValidateNoDuplicatePlacement(ChessPiece piece, BoardPosition targetPosition)
    {
        if (ChessBoard.Instance == null) return true;
        
        // For repositioning pieces, this is always valid (moving existing piece)
        if (repositioningPieces.ContainsKey(piece))
        {
            Debug.Log($"🔍 ValidateNoDuplicatePlacement: Repositioning piece - allowing duplicate check bypass");
            return true;
        }
        
        // Count how many pieces of this type/color are already on the board
        int existingCount = 0;
        ChessPiece existingPiece = null;
        
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    ChessPiece boardPiece = ChessBoard.Instance.GetPieceAt(new BoardPosition(x, y, z));
                    if (boardPiece != null && 
                        boardPiece.pieceColor == piece.pieceColor && 
                        boardPiece.pieceType == piece.pieceType)
                    {
                        existingCount++;
                        existingPiece = boardPiece;
                        Debug.Log($"🔍 Found existing {piece.pieceColor} {piece.pieceType} at ({x},{y},{z})");
                    }
                }
            }
        }
        
        Debug.Log($"🔍 ValidateNoDuplicatePlacement: Found {existingCount} existing {piece.pieceColor} {piece.pieceType} on board");
        
        // Check if we're trying to place a piece when one already exists
        if (existingCount > 0)
        {
            // Special case: Check if the existing piece is actually the same object (shouldn't happen for tray pieces)
            if (existingPiece == piece)
            {
                Debug.LogWarning($"🔍 ValidateNoDuplicatePlacement: WEIRD - Existing piece is same object as placement piece");
                Debug.LogWarning($"   This suggests the piece is already on the board but trying to place from tray");
                Debug.LogWarning($"   Existing piece CurrentPosition: {existingPiece.CurrentPosition}");
                Debug.LogWarning($"   Target position: {targetPosition}");
                return false; // Block this placement
            }
            
            // For unique pieces (King, Queen), only one should exist
            if (piece.pieceType == ChessPieceType.King || piece.pieceType == ChessPieceType.Queen)
            {
                Debug.LogError($"🔍 ValidateNoDuplicatePlacement: BLOCKING - {piece.pieceColor} {piece.pieceType} already exists on board");
                Debug.LogError($"   Existing piece at: {existingPiece.CurrentPosition}");
                Debug.LogError($"   Attempted placement at: {targetPosition}");
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Validate board state consistency for a specific piece
    /// </summary>
    private void ValidateBoardConsistency(ChessPiece piece)
    {
        if (ChessBoard.Instance == null || piece == null) return;
        
        // Get piece's reported position
        BoardPosition reportedPosition = piece.CurrentPosition;
        
        // Only validate pieces that think they're on the board (not in tray)
        if (!reportedPosition.IsValid()) return;
        
        // Check what the board array says is at that position
        ChessPiece boardPiece = ChessBoard.Instance.GetPieceAt(reportedPosition);
        
        if (boardPiece != piece)
        {
            Debug.LogWarning($"🔧 BOARD CONSISTENCY ISSUE detected for {piece.pieceColor} {piece.pieceType}");
            Debug.LogWarning($"   - Piece thinks it's at: {reportedPosition}");
            Debug.LogWarning($"   - Board array contains: {(boardPiece != null ? $"{boardPiece.pieceColor} {boardPiece.pieceType}" : "NULL")}");
            
            // Auto-repair: Update board array to match piece position
            if (boardPiece == null)
            {
                ChessBoard.Instance.SetPieceAt(reportedPosition, piece);
                Debug.Log($"✅ Auto-repaired: Set board[{reportedPosition}] = {piece.pieceColor} {piece.pieceType}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Cannot auto-repair: Position occupied by different piece");
            }
        }
    }
}