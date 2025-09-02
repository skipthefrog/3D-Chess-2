using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a tray that holds pieces before they are placed on the board
/// Each player has their own tray positioned beside the board
/// </summary>
public class PieceTray : MonoBehaviour
{
    [Header("Tray Configuration")]
    public PieceColor trayColor;
    public int maxPieces = 8; // Increased to accommodate all piece types: King, Queen, 2 Bishops, 2 Knights, 2 Rooks
    public float pieceSpacing = 2f;
    public bool isLeftSide = true; // true for left side of board, false for right
    
    [Header("Visual Elements")]
    public Material trayMaterial;
    public GameObject trayBase;
    
    private List<ChessPiece> piecesInTray = new List<ChessPiece>();
    private List<Vector3> piecePositions = new List<Vector3>();
    
    // Reorganization control - disable during placement phase to prevent unwanted piece shifting
    [Header("Reorganization Control")]
    public bool enableReorganization = false; // Default disabled to prevent shifting during placement
    
    public static PieceTray WhiteTray { get; private set; }
    public static PieceTray BlackTray { get; private set; }
    
    private void Awake()
    {
        Debug.Log($"PieceTray.Awake: Starting - component added but waiting for explicit initialization");
        Debug.Log($"  GameObject: '{gameObject.name}', trayColor: {trayColor} (may be default)");
        
        // DO NOT register here - wait for explicit Initialize() call
        // This fixes the Unity component lifecycle issue where Awake() runs before field assignment
    }
    
    /// <summary>
    /// Initialize the tray with explicit color and position settings
    /// This must be called after the component is added and fields are set
    /// </summary>
    public void Initialize(PieceColor color, bool leftSide)
    {
        Debug.Log($"PieceTray.Initialize: Starting initialization for {color} tray on {(leftSide ? "left" : "right")} side");
        
        // Set the properties explicitly
        trayColor = color;
        isLeftSide = leftSide;
        
        Debug.Log($"PieceTray.Initialize: Set trayColor = {trayColor}, isLeftSide = {isLeftSide}");
        
        // Register tray instances for easy access (prevent duplicates)
        if (trayColor == PieceColor.White)
        {
            if (WhiteTray == null)
            {
                WhiteTray = this;
                Debug.Log("✅ PieceTray: White tray registered successfully");
            }
            else if (WhiteTray != this)
            {
                Debug.LogError($"❌ PieceTray: White tray already exists ({WhiteTray.name})! Cannot register duplicate.");
                return;
            }
            else
            {
                Debug.Log("PieceTray: White tray already registered to this object");
            }
        }
        else if (trayColor == PieceColor.Black)
        {
            if (BlackTray == null)
            {
                BlackTray = this;
                Debug.Log("✅ PieceTray: Black tray registered successfully");
            }
            else if (BlackTray != this)
            {
                Debug.LogError($"❌ PieceTray: Black tray already exists ({BlackTray.name})! Cannot register duplicate.");
                return;
            }
            else
            {
                Debug.Log("PieceTray: Black tray already registered to this object");
            }
        }
        else
        {
            Debug.LogError($"PieceTray: Invalid tray color {trayColor}!");
            return;
        }
        
        // Now set up the tray visuals and positions
        Debug.Log($"PieceTray.Initialize: About to call SetupTray for {trayColor}");
        SetupTray();
        
        Debug.Log($"✅ PieceTray.Initialize: Completed initialization for {trayColor} tray");
    }
    
    /// <summary>
    /// Set up the tray's visual elements and piece positions
    /// </summary>
    private void SetupTray()
    {
        Debug.Log($"PieceTray: Setting up {trayColor} tray on {(isLeftSide ? "left" : "right")} side");
        
        // Calculate tray position relative to board
        CalculateTrayPosition();
        
        // Create visual tray base
        CreateTrayBase();
        
        // Calculate positions for pieces in the tray
        CalculatePiecePositions();
        
        Debug.Log($"PieceTray: {trayColor} tray setup complete at {transform.position}");
    }
    
    /// <summary>
    /// Position the tray beside the 4x4x4 board with proper coordinate system alignment
    /// </summary>
    private void CalculateTrayPosition()
    {
        // CORRECTED COORDINATE SYSTEM ANALYSIS:
        // - Board Rotator: at world origin (0, 0, 0)
        // - Emergency Chess Board: child with local offset (-4.2, -4.2, -4.2)
        // - Floor planes: positioned at x*2.8, y*2.8, z*2.8 then parented to Emergency Chess Board
        // - ACTUAL world bounds: X [-4.2 to 4.2], Y [-9.8 to 1.4], Z [-4.2 to 4.2]
        // - Board center in world space: (0, -4.2, 0)
        
        // Calculate correct board bounds in world coordinates
        float cellSize = 2.8f;
        float halfBoardSize = (4 * cellSize) / 2f; // 4 cells * 2.8 / 2 = 5.6, but offset by -4.2 makes range -4.2 to 4.2
        float boardLeftEdge = -4.2f;   // Actual leftmost edge in world space
        float boardRightEdge = 4.2f;   // Actual rightmost edge in world space
        float traySpacing = 8.0f;      // Maximum spacing for clear board visibility during piece placement
        
        // Position trays at a visible height (above the board center level)
        float trayHeight = -1.0f; // Above board center (-4.2) but not too high
        
        Vector3 trayPosition;
        
        if (isLeftSide)
        {
            // White tray: Position 8 units to the left of board's left edge
            float trayX = boardLeftEdge - traySpacing; // -4.2 - 8.0 = -12.2
            trayPosition = new Vector3(trayX, trayHeight, 0f); // Z=0 for center alignment
            Debug.Log($"PieceTray: White tray positioned at X={trayX} (left edge {boardLeftEdge} - {traySpacing})");
        }
        else
        {
            // Black tray: Position 8 units to the right of board's right edge
            float trayX = boardRightEdge + traySpacing; // 4.2 + 8.0 = 12.2
            trayPosition = new Vector3(trayX, trayHeight, 0f); // Z=0 for center alignment
            Debug.Log($"PieceTray: Black tray positioned at X={trayX} (right edge {boardRightEdge} + {traySpacing})");
        }
        
        // Find the Board Rotator to ensure trays are children and rotate with the board
        GameObject boardRotator = GameObject.Find("Board Rotator");
        if (boardRotator != null)
        {
            Debug.Log($"PieceTray: Found Board Rotator at position {boardRotator.transform.position}");
            Debug.Log($"PieceTray: Board Rotator rotation: {boardRotator.transform.rotation}");
            Debug.Log($"PieceTray: Board Rotator scale: {boardRotator.transform.localScale}");
            
            transform.SetParent(boardRotator.transform);
            transform.localPosition = trayPosition;
            
            // SAFETY CHECK: Verify parenting worked correctly
            Vector3 finalWorldPos = transform.position;
            Debug.Log($"PieceTray: {trayColor} tray parented to Board Rotator and positioned at local {trayPosition}");
            Debug.Log($"PieceTray: {trayColor} tray final world position after parenting: {finalWorldPos}");
            
            // Validate that world position is reasonable
            if (finalWorldPos.magnitude > 100f || float.IsNaN(finalWorldPos.x))
            {
                Debug.LogError($"🚨 PieceTray: {trayColor} tray has INVALID final world position: {finalWorldPos}");
                Debug.LogError($"    Local position: {transform.localPosition}");
                Debug.LogError($"    Board Rotator position: {boardRotator.transform.position}");
            }
        }
        else
        {
            // Fallback to world positioning if Board Rotator not found
            transform.position = trayPosition;
            Debug.LogWarning($"PieceTray: Board Rotator not found, using world position {trayPosition} for {trayColor} tray");
        }
        
        Debug.Log($"PieceTray: {trayColor} tray CORRECTED position - world: {transform.position}, local: {transform.localPosition}");
        Debug.Log($"PieceTray: Board world bounds X[{boardLeftEdge} to {boardRightEdge}], trays at X={trayPosition.x}");
        Debug.Log($"PieceTray: Symmetric distances - White: {Mathf.Abs(trayPosition.x)}, Black: {Mathf.Abs(trayPosition.x)} (should be equal when both trays created)");
    }
    
    /// <summary>
    /// Create the visual base of the tray
    /// </summary>
    private void CreateTrayBase()
    {
        trayBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trayBase.name = $"{trayColor} Tray Base";
        trayBase.transform.SetParent(transform);
        trayBase.transform.localPosition = Vector3.zero;
        
        // Size the tray to hold all pieces
        float trayWidth = 4f;
        float trayHeight = 0.2f;
        float trayDepth = (maxPieces * pieceSpacing) + 2f;
        
        trayBase.transform.localScale = new Vector3(trayWidth, trayHeight, trayDepth);
        
        // Apply tray material
        Renderer renderer = trayBase.GetComponent<Renderer>();
        if (trayMaterial == null)
        {
            trayMaterial = CreateTrayMaterial();
        }
        renderer.material = trayMaterial;
        
        Debug.Log($"PieceTray: Created tray base with size {trayBase.transform.localScale}");
    }
    
    /// <summary>
    /// Create a material for the tray
    /// </summary>
    private Material CreateTrayMaterial()
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // Different colors for different trays
        if (trayColor == PieceColor.White)
        {
            material.color = new Color(0.9f, 0.9f, 0.9f, 0.8f); // Light gray
        }
        else
        {
            material.color = new Color(0.3f, 0.3f, 0.3f, 0.8f); // Dark gray
        }
        
        return material;
    }
    
    /// <summary>
    /// Calculate positions where pieces should be placed in the tray
    /// </summary>
    private void CalculatePiecePositions()
    {
        piecePositions.Clear();
        
        // Arrange pieces in a line along the tray
        float startZ = -(maxPieces - 1) * pieceSpacing * 0.5f;
        
        Debug.Log($"PieceTray: Calculating piece positions for {trayColor} tray:");
        Debug.Log($"  maxPieces: {maxPieces}, pieceSpacing: {pieceSpacing}");
        Debug.Log($"  startZ: {startZ}");
        
        for (int i = 0; i < maxPieces; i++)
        {
            Vector3 localPos = new Vector3(0f, 1f, startZ + (i * pieceSpacing));
            piecePositions.Add(localPos);
            Debug.Log($"  Slot {i}: local position {localPos}");
        }
        
        Debug.Log($"PieceTray: Calculated {piecePositions.Count} piece positions for {trayColor} tray");
    }
    
    /// <summary>
    /// Add a piece to this tray
    /// </summary>
    public bool AddPiece(ChessPiece piece)
    {
        if (piecesInTray.Count >= maxPieces)
        {
            Debug.LogWarning($"PieceTray: {trayColor} tray is full! Cannot add piece.");
            return false;
        }
        
        if (piece.pieceColor != trayColor)
        {
            Debug.LogError($"PieceTray: Cannot add {piece.pieceColor} piece to {trayColor} tray");
            return false;
        }
        
        // Position the piece in the tray
        int slotIndex = piecesInTray.Count;
        
        // DIAGNOSTIC: Log BEFORE positioning
        Vector3 pieceOldPosition = piece.transform.position;
        Debug.Log($"🔍 BEFORE AddPiece positioning - {piece.pieceColor} {piece.pieceType}:");
        Debug.Log($"    Old piece position: {pieceOldPosition}");
        Debug.Log($"    Tray position: {transform.position}");
        Debug.Log($"    Target slot {slotIndex} local pos: {piecePositions[slotIndex]}");
        
        // SAFETY CHECK: Validate tray transform before calculating world position
        if (transform.position.magnitude > 1000f || float.IsNaN(transform.position.x) || float.IsInfinity(transform.position.x))
        {
            Debug.LogError($"🚨 PieceTray: INVALID TRAY TRANSFORM DETECTED for {trayColor} tray!");
            Debug.LogError($"    Tray position: {transform.position}");
            Debug.LogError($"    Tray local position: {transform.localPosition}");
            Debug.LogError($"    Tray parent: {(transform.parent != null ? transform.parent.name : "None")}");
            return false;
        }
        
        Vector3 worldPosition = transform.TransformPoint(piecePositions[slotIndex]);
        Debug.Log($"    Calculated world position: {worldPosition}");
        
        // SAFETY CHECK: Validate calculated world position  
        if (worldPosition.magnitude > 1000f || float.IsNaN(worldPosition.x) || float.IsInfinity(worldPosition.x))
        {
            Debug.LogError($"🚨 PieceTray: INVALID WORLD POSITION CALCULATED for {trayColor} tray!");
            Debug.LogError($"    Bad world position: {worldPosition}");
            Debug.LogError($"    Tray position: {transform.position}");
            Debug.LogError($"    Local slot position: {piecePositions[slotIndex]}");
            return false;
        }
        
        piece.transform.position = worldPosition;
        piece.transform.SetParent(transform);
        
        piecesInTray.Add(piece);
        
        Debug.Log($"🎯 PieceTray: Added {piece.pieceColor} {piece.pieceType} to {trayColor} tray at slot {slotIndex}");
        Debug.Log($"  Tray world position: {transform.position}");
        Debug.Log($"  Tray local position: {transform.localPosition}");
        Debug.Log($"  Piece local position in tray: {piecePositions[slotIndex]}");
        Debug.Log($"  Piece final world position: {worldPosition}");
        Debug.Log($"  Piece final transform position: {piece.transform.position}");
        Debug.Log($"  Tray parent: {(transform.parent != null ? transform.parent.name : "None")}");
        return true;
    }
    
    /// <summary>
    /// Remove a piece from this tray
    /// </summary>
    public bool RemovePiece(ChessPiece piece)
    {
        if (!piecesInTray.Contains(piece))
        {
            Debug.LogWarning($"PieceTray: {piece.pieceColor} {piece.pieceType} not found in {trayColor} tray");
            return false;
        }
        
        piecesInTray.Remove(piece);
        
        // IMPORTANT: Remove piece from tray's transform hierarchy 
        // This ensures InputManager won't think it's still in the tray
        if (piece.transform.parent == this.transform)
        {
            piece.transform.SetParent(null); // Remove from tray hierarchy
            Debug.Log($"🔄 PieceTray: Removed {piece.pieceColor} {piece.pieceType} from tray transform hierarchy");
        }
        
        // Attempt to reorganize remaining pieces (will be skipped if reorganization is disabled)
        Debug.Log($"🔄 PieceTray: Attempting to reorganize {trayColor} tray after removal (enableReorganization: {enableReorganization})");
        ReorganizePieces();
        
        Debug.Log($"✅ PieceTray: Successfully removed {piece.pieceColor} {piece.pieceType} from {trayColor} tray");
        return true;
    }
    
    /// <summary>
    /// Reorganize pieces in the tray after removal
    /// </summary>
    private void ReorganizePieces()
    {
        Debug.Log($"🔄 PieceTray.ReorganizePieces: CALLED for {trayColor} tray with {piecesInTray.Count} pieces");
        Debug.Log($"🔄 Tray transform position: {transform.position}");
        Debug.Log($"🔄 Tray transform local position: {transform.localPosition}");
        
        // CONTROL FLAG: Check if reorganization is disabled
        if (!enableReorganization)
        {
            Debug.Log($"🔄 ⚠️ ReorganizePieces: Skipping reorganization for {trayColor} tray - reorganization is disabled");
            Debug.Log($"🔄 This prevents unwanted piece shifting during placement phase");
            return;
        }
        
        // GUARD: Prevent reorganization during piece selection to avoid moving pieces during UI interaction
        if (PlacementManager.Instance != null && PlacementManager.Instance.HasSelectedTrayPiece)
        {
            Debug.Log($"🔄 ⚠️ ReorganizePieces: Skipping reorganization for {trayColor} tray - piece selection in progress");
            return;
        }
        
        // SAFETY CHECK: Validate tray transform before reorganizing
        if (transform.position.magnitude > 1000f || float.IsNaN(transform.position.x) || float.IsInfinity(transform.position.x))
        {
            Debug.LogError($"🚨 ReorganizePieces: INVALID TRAY TRANSFORM for {trayColor} tray - aborting reorganization!");
            Debug.LogError($"    Tray position: {transform.position}");
            Debug.LogError($"    Tray local position: {transform.localPosition}");
            return;
        }
        
        for (int i = 0; i < piecesInTray.Count; i++)
        {
            Vector3 oldPosition = piecesInTray[i].transform.position;
            Vector3 worldPosition = transform.TransformPoint(piecePositions[i]);
            
            // SAFETY CHECK: Validate calculated position
            if (worldPosition.magnitude > 1000f || float.IsNaN(worldPosition.x) || float.IsInfinity(worldPosition.x))
            {
                Debug.LogError($"🚨 ReorganizePieces: INVALID WORLD POSITION for {piecesInTray[i].pieceColor} {piecesInTray[i].pieceType} - skipping!");
                Debug.LogError($"    Bad world position: {worldPosition}");
                Debug.LogError($"    Tray position: {transform.position}");
                Debug.LogError($"    Local position: {piecePositions[i]}");
                continue;
            }
            
            piecesInTray[i].transform.position = worldPosition;
            
            Debug.Log($"🔄 Moved {piecesInTray[i].pieceColor} {piecesInTray[i].pieceType}:");
            Debug.Log($"    From: {oldPosition}");
            Debug.Log($"    To: {worldPosition}");
            Debug.Log($"    Local tray pos: {piecePositions[i]}");
        }
        
        Debug.Log($"🔄 PieceTray.ReorganizePieces: COMPLETED for {trayColor} tray");
    }
    
    /// <summary>
    /// Check if this tray is empty
    /// </summary>
    public bool IsEmpty()
    {
        return piecesInTray.Count == 0;
    }
    
    /// <summary>
    /// Check if this tray is full
    /// </summary>
    public bool IsFull()
    {
        return piecesInTray.Count >= maxPieces;
    }
    
    /// <summary>
    /// Get the number of pieces in this tray
    /// </summary>
    public int GetPieceCount()
    {
        return piecesInTray.Count;
    }
    
    /// <summary>
    /// Get all pieces in this tray
    /// </summary>
    public List<ChessPiece> GetPieces()
    {
        return new List<ChessPiece>(piecesInTray);
    }
    
    /// <summary>
    /// Set the visibility of this tray and all its pieces
    /// </summary>
    public void SetTrayVisibility(bool visible)
    {
        gameObject.SetActive(visible);
        Debug.Log($"PieceTray: {trayColor} tray visibility set to {visible}");
    }
    
    /// <summary>
    /// Hide this tray from view
    /// </summary>
    public void HideTray()
    {
        SetTrayVisibility(false);
    }
    
    /// <summary>
    /// Show this tray
    /// </summary>
    public void ShowTray()
    {
        SetTrayVisibility(true);
    }
    
    /// <summary>
    /// Hide both trays from view
    /// </summary>
    public static void HideAllTrays()
    {
        if (WhiteTray != null) WhiteTray.HideTray();
        if (BlackTray != null) BlackTray.HideTray();
        Debug.Log("PieceTray: All trays hidden");
    }
    
    /// <summary>
    /// Show both trays
    /// </summary>
    public static void ShowAllTrays()
    {
        if (WhiteTray != null) WhiteTray.ShowTray();
        if (BlackTray != null) BlackTray.ShowTray();
        Debug.Log("PieceTray: All trays shown");
    }
    
    /// <summary>
    /// Set visibility of both trays
    /// </summary>
    public static void SetAllTraysVisible(bool visible)
    {
        if (visible)
            ShowAllTrays();
        else
            HideAllTrays();
    }
    
    /// <summary>
    /// Emergency recovery method to find and fix pieces that are in invalid positions
    /// </summary>
    public static void ValidateAndRecoverAllTrayPieces()
    {
        Debug.Log("🚨 PieceTray.ValidateAndRecoverAllTrayPieces: Starting emergency piece recovery");
        
        if (WhiteTray != null)
        {
            WhiteTray.ValidateAndRecoverTrayPieces();
        }
        
        if (BlackTray != null)
        {
            BlackTray.ValidateAndRecoverTrayPieces();
        }
        
        Debug.Log("🚨 PieceTray.ValidateAndRecoverAllTrayPieces: Emergency recovery completed");
    }
    
    /// <summary>
    /// Validate and recover pieces in this specific tray
    /// </summary>
    private void ValidateAndRecoverTrayPieces()
    {
        Debug.Log($"🔧 PieceTray.ValidateAndRecoverTrayPieces: Checking {trayColor} tray");
        
        for (int i = 0; i < piecesInTray.Count; i++)
        {
            ChessPiece piece = piecesInTray[i];
            if (piece == null) continue;
            
            Vector3 currentPos = piece.transform.position;
            Vector3 expectedPos = transform.TransformPoint(piecePositions[i]);
            
            // Check if piece is too far from expected position
            float distance = Vector3.Distance(currentPos, expectedPos);
            if (distance > 5f) // 5 units tolerance
            {
                Debug.LogWarning($"🔧 Found misplaced {piece.pieceColor} {piece.pieceType} in {trayColor} tray:");
                Debug.LogWarning($"    Current position: {currentPos}");
                Debug.LogWarning($"    Expected position: {expectedPos}");
                Debug.LogWarning($"    Distance: {distance}");
                
                // GUARD: Don't move pieces during piece selection to prevent unwanted movement
                if (PlacementManager.Instance != null && PlacementManager.Instance.HasSelectedTrayPiece)
                {
                    Debug.Log($"🔄 ⚠️ ValidateAndRecoverTrayPieces: Skipping recovery - piece selection in progress");
                    Debug.Log($"    Piece will be recovered after selection completes if still needed");
                    continue;
                }
                
                // Recover the piece to correct position
                piece.transform.position = expectedPos;
                piece.transform.SetParent(transform);
                
                Debug.Log($"✅ Recovered {piece.pieceColor} {piece.pieceType} to correct position {expectedPos}");
            }
            
            // Check if piece has correct parent
            if (piece.transform.parent != transform)
            {
                Debug.LogWarning($"🔧 {piece.pieceColor} {piece.pieceType} has incorrect parent: {(piece.transform.parent != null ? piece.transform.parent.name : "None")}");
                piece.transform.SetParent(transform);
                Debug.Log($"✅ Fixed parent for {piece.pieceColor} {piece.pieceType}");
            }
        }
        
        Debug.Log($"🔧 PieceTray.ValidateAndRecoverTrayPieces: Completed validation for {trayColor} tray");
    }
    
    /// <summary>
    /// Manually trigger reorganization of pieces in the tray (ignores enableReorganization flag)
    /// </summary>
    public void ForceReorganizePieces()
    {
        Debug.Log($"🔄 PieceTray.ForceReorganizePieces: Manually triggered for {trayColor} tray (ignoring enableReorganization flag)");
        
        bool originalSetting = enableReorganization;
        enableReorganization = true; // Temporarily enable
        
        try
        {
            ReorganizePieces();
        }
        finally
        {
            enableReorganization = originalSetting; // Restore original setting
        }
        
        Debug.Log($"🔄 PieceTray.ForceReorganizePieces: Completed for {trayColor} tray");
    }
    
    /// <summary>
    /// Enable or disable automatic reorganization for both trays
    /// </summary>
    public static void SetReorganizationEnabled(bool enabled)
    {
        Debug.Log($"🔄 PieceTray.SetReorganizationEnabled: Setting reorganization to {enabled} for all trays");
        
        if (WhiteTray != null)
        {
            WhiteTray.enableReorganization = enabled;
            Debug.Log($"🔄 White tray reorganization: {enabled}");
        }
        
        if (BlackTray != null)
        {
            BlackTray.enableReorganization = enabled;
            Debug.Log($"🔄 Black tray reorganization: {enabled}");
        }
    }
    
    /// <summary>
    /// Force reorganization of pieces in both trays
    /// </summary>
    public static void ForceReorganizeAllTrays()
    {
        Debug.Log($"🔄 PieceTray.ForceReorganizeAllTrays: Manually reorganizing all trays");
        
        if (WhiteTray != null)
        {
            WhiteTray.ForceReorganizePieces();
        }
        
        if (BlackTray != null)
        {
            BlackTray.ForceReorganizePieces();
        }
        
        Debug.Log($"🔄 PieceTray.ForceReorganizeAllTrays: Completed");
    }
}