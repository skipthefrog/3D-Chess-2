using UnityEngine;

/// <summary>
/// Manages the pawn promotion process, coordinating between detection, UI, and piece replacement
/// </summary>
public class PawnPromotionManager : MonoBehaviour
{
    public static PawnPromotionManager Instance { get; private set; }
    
    [Header("Promotion Settings")]
    public bool enablePromotion = true;
    
    // Promotion state tracking
    private bool isPromotionInProgress = false;
    private Pawn promotingPawn = null;
    private BoardPosition promotionTargetPosition;
    private PieceColor promotingPlayerColor;
    
    // Events
    public System.Action<PieceColor> OnPromotionStarted;
    public System.Action<PieceColor, ChessPieceType> OnPromotionCompleted;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("PawnPromotionManager: Instance created");
        }
        else
        {
            Debug.LogWarning("PawnPromotionManager: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        // Emergency reset mechanism - press R to force reset stuck promotion (for debugging)
        if (Input.GetKeyDown(KeyCode.R) && isPromotionInProgress)
        {
            Debug.LogWarning($"PawnPromotionManager.Update: EMERGENCY RESET triggered by user (R key)");
            Debug.LogWarning($"PawnPromotionManager.Update: Forcing promotion to Queen and resetting state");
            
            // Hide the UI before forcing promotion
            HidePromotionUI();
            
            // Force promotion to Queen and reset state
            OnPieceSelected(ChessPieceType.Queen);
        }
    }
    
    /// <summary>
    /// Check if a pawn move would trigger promotion and handle accordingly
    /// </summary>
    public bool HandlePotentialPromotion(Pawn pawn, BoardPosition fromPosition, BoardPosition targetPosition)
    {
        Debug.Log($"🚨 PawnPromotionManager.HandlePotentialPromotion: ENTRY");
        Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: pawn={pawn?.pieceColor} {pawn?.pieceType}");
        Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: from={fromPosition}, to={targetPosition}");
        Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: enablePromotion={enablePromotion}");
        Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: isPromotionInProgress={isPromotionInProgress}");
        
        if (!enablePromotion || isPromotionInProgress)
        {
            Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: BLOCKING - enablePromotion={enablePromotion}, isPromotionInProgress={isPromotionInProgress}");
            return false; // No promotion or already in progress
        }
        
        Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: Checking IsPromotionMove({targetPosition})");
        bool isPromotionMove = pawn.IsPromotionMove(targetPosition);
        Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: IsPromotionMove result: {isPromotionMove}");
        
        if (isPromotionMove)
        {
            Debug.Log($"🚨 PawnPromotionManager.HandlePotentialPromotion: PROMOTION CONFIRMED - Starting promotion for {pawn.pieceColor} pawn from {fromPosition} to {targetPosition}");
            
            // First, complete the pawn's movement to the target position
            if (ChessBoard.Instance != null)
            {
                Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: Updating board position");
                // Update board array
                ChessBoard.Instance.SetPieceAt(fromPosition, null);
                ChessBoard.Instance.SetPieceAt(targetPosition, pawn);
                
                // Move the pawn visually
                pawn.MoveTo(targetPosition);
                Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: Pawn moved to {targetPosition}");
            }
            
            Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: Calling StartPromotion");
            StartPromotion(pawn, targetPosition);
            Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: StartPromotion completed, returning true");
            return true; // Promotion triggered
        }
        
        Debug.Log($"PawnPromotionManager.HandlePotentialPromotion: No promotion needed, returning false");
        return false; // No promotion needed
    }
    
    /// <summary>
    /// Start the promotion process
    /// </summary>
    private void StartPromotion(Pawn pawn, BoardPosition targetPosition)
    {
        isPromotionInProgress = true;
        promotingPawn = pawn;
        promotionTargetPosition = targetPosition;
        promotingPlayerColor = pawn.pieceColor;
        
        Debug.Log($"PawnPromotionManager.StartPromotion: Promotion started for {promotingPlayerColor} pawn");
        
        // Notify listeners that promotion has started
        OnPromotionStarted?.Invoke(promotingPlayerColor);
        
        // Show promotion UI
        Debug.Log($"PawnPromotionManager.StartPromotion: Looking for PawnPromotionUI component...");
        
        // Try multiple methods to find the UI component
        PawnPromotionUI promotionUI = FindFirstObjectByType<PawnPromotionUI>();
        
        if (promotionUI == null)
        {
            Debug.LogWarning($"PawnPromotionManager.StartPromotion: FindFirstObjectByType failed, trying FindAnyObjectByType...");
            promotionUI = FindAnyObjectByType<PawnPromotionUI>();
        }
        
        if (promotionUI == null)
        {
            Debug.LogWarning($"PawnPromotionManager.StartPromotion: FindAnyObjectByType failed, searching GameObjects...");
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                PawnPromotionUI ui = obj.GetComponent<PawnPromotionUI>();
                if (ui != null)
                {
                    promotionUI = ui;
                    Debug.Log($"PawnPromotionManager.StartPromotion: Found PawnPromotionUI on GameObject '{obj.name}'");
                    break;
                }
            }
        }
        
        if (promotionUI != null)
        {
            Debug.Log($"PawnPromotionManager.StartPromotion: Found PawnPromotionUI on GameObject '{promotionUI.gameObject.name}'");
            Debug.Log($"PawnPromotionManager.StartPromotion: UI GameObject active: {promotionUI.gameObject.activeInHierarchy}");
            Debug.Log($"PawnPromotionManager.StartPromotion: UI Component enabled: {promotionUI.enabled}");
            Debug.Log($"PawnPromotionManager.StartPromotion: Calling ShowPromotionSelection...");
            Debug.Log($"PawnPromotionManager.StartPromotion: Passing OnPieceSelected callback method to UI");
            
            promotionUI.ShowPromotionSelection(promotingPlayerColor, OnPieceSelected);
            Debug.Log($"PawnPromotionManager.StartPromotion: ShowPromotionSelection call completed");
        }
        else
        {
            Debug.LogError("PawnPromotionManager.StartPromotion: PawnPromotionUI not found after exhaustive search!");
            Debug.LogError("PawnPromotionManager.StartPromotion: This indicates the UI component was not created properly");
            Debug.LogError("PawnPromotionManager.StartPromotion: Falling back to automatic Queen promotion");
            OnPieceSelected(ChessPieceType.Queen);
        }
    }
    
    /// <summary>
    /// Handle the player's piece selection
    /// </summary>
    private void OnPieceSelected(ChessPieceType selectedPieceType)
    {
        Debug.Log($"PawnPromotionManager.OnPieceSelected: Player selected {selectedPieceType} for promotion");
        Debug.Log($"PawnPromotionManager.OnPieceSelected: isPromotionInProgress={isPromotionInProgress}");
        
        if (promotingPawn == null)
        {
            Debug.LogError("PawnPromotionManager.OnPieceSelected: No pawn to promote!");
            return;
        }
        
        // Store the old pawn's position for removal
        BoardPosition oldPawnPosition = promotingPawn.CurrentPosition;
        
        // Create the new piece
        ChessPiece newPiece = CreatePromotedPiece(selectedPieceType, promotionTargetPosition, promotingPlayerColor);
        
        if (newPiece != null)
        {
            // Update the board
            if (ChessBoard.Instance != null)
            {
                // Remove old pawn from its current position
                ChessBoard.Instance.SetPieceAt(oldPawnPosition, null);
                
                // Place new piece at target position
                ChessBoard.Instance.SetPieceAt(promotionTargetPosition, newPiece);
                
                Debug.Log($"PawnPromotionManager: Successfully promoted {promotingPlayerColor} pawn to {selectedPieceType} at {promotionTargetPosition}");
                
                // CRITICAL: Invalidate check detection cache and schedule delayed visual feedback
                if (CheckDetectionManager.Instance != null)
                {
                    Debug.Log($"PawnPromotionManager: Invalidating check cache after promotion");
                    CheckDetectionManager.Instance.InvalidateCache();
                    Debug.Log($"PawnPromotionManager: Scheduling delayed visual feedback update to avoid interference");
                    
                    // Use only delayed visual update to avoid immediate interference with game state
                    StartCoroutine(DelayedVisualUpdateAfterPromotion());
                }
                else
                {
                    Debug.LogWarning($"PawnPromotionManager: CheckDetectionManager.Instance is NULL - cannot evaluate check state after promotion");
                }
            }
            
            // Destroy the old pawn
            Destroy(promotingPawn.gameObject);
            
            // Notify completion
            OnPromotionCompleted?.Invoke(promotingPlayerColor, selectedPieceType);
            
            // Complete the turn (since promotion is part of the move)
            if (TurnManager.Instance != null && GameStateManager.Instance != null && GameStateManager.Instance.CanMovePieces())
            {
                TurnManager.Instance.NextTurn();
                Debug.Log($"🔄 Turn switched after {promotingPlayerColor} promotion");
            }
        }
        else
        {
            Debug.LogError("PawnPromotionManager: Failed to create promoted piece!");
        }
        
        // Reset promotion state
        Debug.Log($"PawnPromotionManager.OnPieceSelected: Setting isPromotionInProgress = false");
        isPromotionInProgress = false;
        promotingPawn = null;
        Debug.Log($"PawnPromotionManager.OnPieceSelected: Promotion state reset complete");
    }
    
    /// <summary>
    /// Create the promoted piece based on the selected type
    /// </summary>
    private ChessPiece CreatePromotedPiece(ChessPieceType pieceType, BoardPosition position, PieceColor color)
    {
        Debug.Log($"🚨 CreatePromotedPiece: ENTRY - Creating {color} {pieceType} at {position}");
        
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("CreatePromotedPiece: GameManager not found!");
            return null;
        }
        Debug.Log($"CreatePromotedPiece: GameManager found: {gameManager.name}");
        
        // CRITICAL FIX: Ensure prefabs are created if they don't exist
        // This handles cases where the GameManager hasn't set up the prefabs yet
        Debug.Log($"CreatePromotedPiece: Checking if {pieceType} prefab exists...");
        
        GameObject prefab = null;
        
        // Select the appropriate prefab with auto-creation if missing
        switch (pieceType)
        {
            case ChessPieceType.Queen:
                prefab = gameManager.queenPiecePrefab;
                if (prefab == null)
                {
                    Debug.LogWarning($"CreatePromotedPiece: Queen prefab is NULL, creating it dynamically");
                    prefab = CreateQueenPrefabDirect();
                }
                Debug.Log($"CreatePromotedPiece: Selected Queen prefab: {(prefab != null ? prefab.name : "NULL")}");
                break;
            case ChessPieceType.Rook:
                prefab = gameManager.rookPiecePrefab;
                if (prefab == null)
                {
                    Debug.LogWarning($"CreatePromotedPiece: Rook prefab is NULL, creating it dynamically");
                    prefab = CreateRookPrefabDirect();
                }
                Debug.Log($"CreatePromotedPiece: Selected Rook prefab: {(prefab != null ? prefab.name : "NULL")}");
                break;
            case ChessPieceType.Bishop:
                prefab = gameManager.bishopPiecePrefab;
                if (prefab == null)
                {
                    Debug.LogWarning($"CreatePromotedPiece: Bishop prefab is NULL, creating it dynamically");
                    prefab = CreateBishopPrefabDirect();
                }
                Debug.Log($"CreatePromotedPiece: Selected Bishop prefab: {(prefab != null ? prefab.name : "NULL")}");
                break;
            case ChessPieceType.Knight:
                prefab = gameManager.knightPiecePrefab;
                if (prefab == null)
                {
                    Debug.LogWarning($"CreatePromotedPiece: Knight prefab is NULL, creating it dynamically");
                    prefab = CreateKnightPrefabDirect();
                }
                Debug.Log($"CreatePromotedPiece: Selected Knight prefab: {(prefab != null ? prefab.name : "NULL")}");
                break;
            default:
                Debug.LogError($"CreatePromotedPiece: Invalid piece type for promotion: {pieceType}");
                return null;
        }
        
        if (prefab == null)
        {
            Debug.LogError($"CreatePromotedPiece: No prefab found for {pieceType}");
            Debug.LogError($"CreatePromotedPiece: GameManager piece prefabs status:");
            Debug.LogError($"  Queen: {(gameManager.queenPiecePrefab != null ? gameManager.queenPiecePrefab.name : "NULL")}");
            Debug.LogError($"  Rook: {(gameManager.rookPiecePrefab != null ? gameManager.rookPiecePrefab.name : "NULL")}");
            Debug.LogError($"  Bishop: {(gameManager.bishopPiecePrefab != null ? gameManager.bishopPiecePrefab.name : "NULL")}");
            Debug.LogError($"  Knight: {(gameManager.knightPiecePrefab != null ? gameManager.knightPiecePrefab.name : "NULL")}");
            return null;
        }
        
        // Instantiate the new piece
        Debug.Log($"CreatePromotedPiece: Instantiating prefab {prefab.name}");
        GameObject newPieceObject = null;
        try
        {
            newPieceObject = Instantiate(prefab);
            Debug.Log($"CreatePromotedPiece: Successfully instantiated {newPieceObject.name}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"CreatePromotedPiece: Exception during Instantiate: {ex.Message}");
            return null;
        }
        
        newPieceObject.name = $"{color} {pieceType} (Promoted)";
        Debug.Log($"CreatePromotedPiece: Set GameObject name to {newPieceObject.name}");
        
        // Find the pieces container to parent the piece to
        GameObject piecesContainer = GameObject.Find("Pieces Container");
        if (piecesContainer != null)
        {
            Debug.Log($"CreatePromotedPiece: Found Pieces Container, setting parent");
            newPieceObject.transform.SetParent(piecesContainer.transform);
            Debug.Log($"CreatePromotedPiece: Parented to {piecesContainer.name}");
        }
        else
        {
            Debug.LogWarning($"CreatePromotedPiece: Pieces Container not found, piece will be unparented");
        }
        
        // Log the position before and after parenting
        Debug.Log($"CreatePromotedPiece: GameObject position after parenting: {newPieceObject.transform.position}");
        Debug.Log($"CreatePromotedPiece: GameObject localPosition: {newPieceObject.transform.localPosition}");
        
        // Get the ChessPiece component and initialize it
        ChessPiece newPiece = newPieceObject.GetComponent<ChessPiece>();
        if (newPiece != null)
        {
            Debug.Log($"CreatePromotedPiece: Found ChessPiece component: {newPiece.GetType().Name}");
            Debug.Log($"CreatePromotedPiece: Calling Initialize({position}, {color})");
            
            try
            {
                newPiece.Initialize(position, color);
                Debug.Log($"CreatePromotedPiece: Initialize completed successfully");
                
                // CRITICAL FIX: Activate the GameObject after initialization
                Debug.Log($"CreatePromotedPiece: GameObject was active: {newPieceObject.activeInHierarchy}");
                newPieceObject.SetActive(true);
                Debug.Log($"CreatePromotedPiece: ACTIVATED GameObject - now active: {newPieceObject.activeInHierarchy}");
                
                Debug.Log($"CreatePromotedPiece: Final piece position: {newPiece.transform.position}");
                Debug.Log($"CreatePromotedPiece: Final piece localPosition: {newPiece.transform.localPosition}");
                Debug.Log($"CreatePromotedPiece: Final piece CurrentPosition: {newPiece.CurrentPosition}");
                Debug.Log($"CreatePromotedPiece: Piece GameObject active: {newPieceObject.activeInHierarchy}");
                return newPiece;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CreatePromotedPiece: Exception during Initialize: {ex.Message}");
                Debug.LogError($"CreatePromotedPiece: Exception stack trace: {ex.StackTrace}");
                Destroy(newPieceObject);
                return null;
            }
        }
        else
        {
            Debug.LogError($"CreatePromotedPiece: Created piece does not have ChessPiece component!");
            Debug.LogError($"CreatePromotedPiece: GameObject components:");
            Component[] components = newPieceObject.GetComponents<Component>();
            foreach (Component comp in components)
            {
                Debug.LogError($"  - {comp.GetType().Name}");
            }
            Destroy(newPieceObject);
            return null;
        }
    }
    
    /// <summary>
    /// Check if promotion is currently in progress
    /// </summary>
    public bool IsPromotionInProgress()
    {
        // Only log state changes or periodically to avoid spam
        if (Time.frameCount % 60 == 0) // Log every 60 frames (about once per second)
        {
            Debug.Log($"PawnPromotionManager.IsPromotionInProgress: returning {isPromotionInProgress}");
        }
        return isPromotionInProgress;
    }
    
    /// <summary>
    /// Get the color of the player currently promoting (if any)
    /// </summary>
    public PieceColor? GetPromotingPlayerColor()
    {
        return isPromotionInProgress ? promotingPlayerColor : null;
    }
    
    /// <summary>
    /// Create Knight prefab directly for promotion when GameManager's prefab is missing
    /// </summary>
    private GameObject CreateKnightPrefabDirect()
    {
        Debug.Log("CreateKnightPrefabDirect: Creating Knight prefab for promotion");
        
        GameObject prefab = new GameObject("Knight Piece Prefab (Promotion)");
        prefab.SetActive(false);
        
        // Add visual representation - L-shaped geometry for Knight (same as GameManager)
        GameObject visual1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject visual2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual1.transform.SetParent(prefab.transform);
        visual2.transform.SetParent(prefab.transform);
        
        // Vertical part of L
        visual1.transform.localPosition = new Vector3(0, 1.05f, 0);
        visual1.transform.localScale = new Vector3(0.4f, 2.1f, 0.4f);
        
        // Horizontal part of L
        visual2.transform.localPosition = new Vector3(0.4f, 0.3f, 0);
        visual2.transform.localScale = new Vector3(0.8f, 0.6f, 0.4f);
        
        // Remove colliders from visuals
        Collider visualCollider1 = visual1.GetComponent<Collider>();
        if (visualCollider1 != null) DestroyImmediate(visualCollider1);
        
        Collider visualCollider2 = visual2.GetComponent<Collider>();
        if (visualCollider2 != null) DestroyImmediate(visualCollider2);
        
        // Add touch collider
        SphereCollider collider = prefab.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 1.05f, 0);
        collider.radius = 1.3f;
        
        // Add Knight script
        Knight knightScript = prefab.AddComponent<Knight>();
        prefab.tag = "ChessPiece";
        
        Debug.Log("CreateKnightPrefabDirect: Knight prefab created");
        return prefab;
    }
    
    /// <summary>
    /// Create Queen prefab directly for promotion when GameManager's prefab is missing
    /// </summary>
    private GameObject CreateQueenPrefabDirect()
    {
        Debug.Log("CreateQueenPrefabDirect: Creating Queen prefab for promotion");
        
        GameObject prefab = new GameObject("Queen Piece Prefab (Promotion)");
        prefab.SetActive(false);
        
        // Add visual representation - cylinder for Queen
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.transform.SetParent(prefab.transform);
        visual.transform.localPosition = new Vector3(0, 1.19f, 0);
        visual.transform.localScale = new Vector3(0.7f, 1.19f, 0.7f);
        
        // Remove visual collider
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null) DestroyImmediate(visualCollider);
        
        // Add touch collider
        SphereCollider collider = prefab.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 1.19f, 0);
        collider.radius = 1.3f;
        
        // Add Queen script
        Queen queenScript = prefab.AddComponent<Queen>();
        prefab.tag = "ChessPiece";
        
        Debug.Log("CreateQueenPrefabDirect: Queen prefab created");
        return prefab;
    }
    
    /// <summary>
    /// Create Rook prefab directly for promotion when GameManager's prefab is missing
    /// </summary>
    private GameObject CreateRookPrefabDirect()
    {
        Debug.Log("CreateRookPrefabDirect: Creating Rook prefab for promotion");
        
        GameObject prefab = new GameObject("Rook Piece Prefab (Promotion)");
        prefab.SetActive(false);
        
        // Add visual representation - castle tower
        GameObject baseVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject towerVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseVisual.transform.SetParent(prefab.transform);
        towerVisual.transform.SetParent(prefab.transform);
        
        // Base and tower
        baseVisual.transform.localPosition = new Vector3(0, 0.3f, 0);
        baseVisual.transform.localScale = new Vector3(1.0f, 0.6f, 1.0f);
        towerVisual.transform.localPosition = new Vector3(0, 1.26f, 0);
        towerVisual.transform.localScale = new Vector3(0.7f, 1.9f, 0.7f);
        
        // Remove visual colliders
        Collider baseCollider = baseVisual.GetComponent<Collider>();
        if (baseCollider != null) DestroyImmediate(baseCollider);
        Collider towerCollider = towerVisual.GetComponent<Collider>();
        if (towerCollider != null) DestroyImmediate(towerCollider);
        
        // Add touch collider
        SphereCollider collider = prefab.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 1.26f, 0);
        collider.radius = 1.3f;
        
        // Add Rook script
        Rook rookScript = prefab.AddComponent<Rook>();
        prefab.tag = "ChessPiece";
        
        Debug.Log("CreateRookPrefabDirect: Rook prefab created");
        return prefab;
    }
    
    /// <summary>
    /// Create Bishop prefab directly for promotion when GameManager's prefab is missing
    /// </summary>
    private GameObject CreateBishopPrefabDirect()
    {
        Debug.Log("CreateBishopPrefabDirect: Creating Bishop prefab for promotion");
        
        GameObject prefab = new GameObject("Bishop Piece Prefab (Promotion)");
        prefab.SetActive(false);
        
        // Add visual representation - rotated cube for Bishop
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(prefab.transform);
        visual.transform.localPosition = new Vector3(0, 1.12f, 0);
        visual.transform.localScale = new Vector3(0.6f, 2.24f, 0.6f);
        visual.transform.localRotation = Quaternion.Euler(0, 45f, 0);
        
        // Remove visual collider
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null) DestroyImmediate(visualCollider);
        
        // Add touch collider
        SphereCollider collider = prefab.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 1.12f, 0);
        collider.radius = 1.2f;
        
        // Add Bishop script
        Bishop bishopScript = prefab.AddComponent<Bishop>();
        prefab.tag = "ChessPiece";
        
        Debug.Log("CreateBishopPrefabDirect: Bishop prefab created");
        return prefab;
    }
    
    /// <summary>
    /// Hide the promotion UI (for timeout/emergency situations)
    /// </summary>
    private void HidePromotionUI()
    {
        Debug.Log("PawnPromotionManager.HidePromotionUI: Looking for PawnPromotionUI to hide...");
        
        PawnPromotionUI promotionUI = FindFirstObjectByType<PawnPromotionUI>();
        if (promotionUI != null)
        {
            Debug.Log("PawnPromotionManager.HidePromotionUI: Found UI, calling HidePromotionSelection");
            promotionUI.HidePromotionSelection();
        }
        else
        {
            Debug.LogWarning("PawnPromotionManager.HidePromotionUI: PawnPromotionUI not found");
        }
    }
    
    /// <summary>
    /// Delayed visual update after promotion to ensure check indicators appear without interfering with movement
    /// </summary>
    private System.Collections.IEnumerator DelayedVisualUpdateAfterPromotion()
    {
        Debug.Log("PawnPromotionManager: Starting delayed visual update after promotion");
        
        // Wait multiple frames for all systems to stabilize
        yield return null;
        yield return null;
        
        // Additional delay to ensure turn switching and all game state updates are complete
        yield return new WaitForSeconds(0.3f);
        
        if (CheckDetectionManager.Instance != null)
        {
            Debug.Log("PawnPromotionManager: Executing delayed visual feedback update after promotion");
            CheckDetectionManager.Instance.InvalidateCache();
            CheckDetectionManager.Instance.SafeUpdateVisualFeedback();
            Debug.Log("PawnPromotionManager: Delayed visual feedback update completed");
        }
        else
        {
            Debug.LogWarning("PawnPromotionManager: CheckDetectionManager not available for delayed update");
        }
    }
}