using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject kingPiecePrefab;
    public GameObject queenPiecePrefab;
    public GameObject bishopPiecePrefab;
    public GameObject knightPiecePrefab;
    public GameObject rookPiecePrefab;
    public GameObject pawnPiecePrefab;
    
    [Header("Game Settings")]
    public bool setupTestPieces = true;
    
    // State tracking to prevent multiple initialization attempts
    private bool traysCreated = false;
    
    private void Start()
    {
        InitializeGame();
    }
    
    private void InitializeGame()
    {
        Debug.Log("GameManager: Starting game initialization...");
        
        // Create game state management
        CreateGameStateManager();
        
        // Create ChessBoard for piece management alongside EmergencyChessBoard for grid visualization  
        CreateChessBoardInstance();
        Debug.Log("GameManager: ChessBoard created for piece management");
        
        // CRITICAL: Wait one frame for ChessBoard singleton to be fully initialized
        StartCoroutine(InitializeGameDelayed());
    }
    
    private System.Collections.IEnumerator InitializeGameDelayed()
    {
        Debug.Log("GameManager: Waiting one frame for ChessBoard to initialize...");
        yield return null; // Wait one frame
        
        // Verify ChessBoard.Instance is ready
        if (ChessBoard.Instance == null)
        {
            Debug.LogError("GameManager: ChessBoard.Instance is still NULL after waiting! Cannot continue.");
            yield break;
        }
        
        Debug.Log("GameManager: ChessBoard.Instance confirmed ready, continuing initialization...");
        
        // DIAGNOSTIC: Create test input script first to verify Unity compilation
        Debug.Log("GameManager: Creating InputManagerTest for diagnostics...");
        GameObject testObject = new GameObject("InputManagerTest");
        testObject.AddComponent<InputManagerTest>();
        Debug.Log("GameManager: InputManagerTest created");
        
        // Ensure InputManager exists
        if (InputManager.Instance == null)
        {
            Debug.Log("GameManager: Creating Input Manager GameObject...");
            GameObject inputObject = new GameObject("Input Manager");
            inputObject.AddComponent<InputManager>();
            Debug.Log("GameManager: Input Manager created successfully");
        }
        else
        {
            Debug.Log("GameManager: Input Manager already exists");
        }
        
        // Set up camera target for board center
        SetupCameraTarget();
        
        // Set up rotation UI controls
        SetupRotationUI();
        
        // Clean up any existing King pieces BEFORE creating trays
        CleanUpExistingKingPieces();
        
        // CRITICAL: Wait for EmergencyChessBoard and Board Rotator to be created
        // The trays need to be children of Board Rotator to rotate properly with the board
        yield return null; // Wait one more frame for board structure to be complete
        
        // Create piece trays (they will automatically parent themselves to Board Rotator)
        Debug.Log("GameManager: About to call SetupPieceTrays()...");
        SetupPieceTrays();
        Debug.Log($"GameManager: SetupPieceTrays() completed. Static references - WhiteTray: {(PieceTray.WhiteTray != null ? PieceTray.WhiteTray.name : "NULL")}, BlackTray: {(PieceTray.BlackTray != null ? PieceTray.BlackTray.name : "NULL")}");
        
        // Create placement manager
        SetupPlacementManager();
        
        // Create placement UI
        SetupPlacementUI();
        
        // Create pawn promotion components
        SetupPawnPromotionSystem();
        
        // Create check detection system
        SetupCheckDetectionSystem();
        
        // Wait one more frame for trays to be fully initialized
        yield return null;
        
        if (setupTestPieces)
        {
            Debug.Log("GameManager: Setting up pieces in trays...");
            SetupPiecesInTrays();
        }
        
        // Add the automated test system
        GameObject movementTestObject = new GameObject("Movement Test System");
        movementTestObject.AddComponent<TestMovementSystem>();
        Debug.Log("GameManager: Added TestMovementSystem for verification");
        
        // Add UI tester for debugging check UI issues
        GameObject checkUITesterObject = new GameObject("Check UI Tester");
        checkUITesterObject.AddComponent<CheckUITester>();
        Debug.Log("GameManager: Added CheckUITester for manual UI testing (T/Y/U keys)");
        
        Debug.Log("GameManager: Game initialization complete!");
    }
    
    /// <summary>
    /// Public method to access piece prefabs, ensuring they are created if needed
    /// </summary>
    public GameObject GetPiecePrefab(ChessPieceType pieceType)
    {
        // Ensure prefabs are created if they don't exist
        EnsurePrefabsExist();
        
        switch (pieceType)
        {
            case ChessPieceType.King:
                return kingPiecePrefab;
            case ChessPieceType.Queen:
                return queenPiecePrefab;
            case ChessPieceType.Bishop:
                return bishopPiecePrefab;
            case ChessPieceType.Knight:
                return knightPiecePrefab;
            case ChessPieceType.Rook:
                return rookPiecePrefab;
            case ChessPieceType.Pawn:
                return pawnPiecePrefab;
            default:
                Debug.LogError($"GameManager.GetPiecePrefab: Unknown piece type {pieceType}");
                return null;
        }
    }
    
    /// <summary>
    /// Ensure all piece prefabs exist, creating them if necessary
    /// </summary>
    private void EnsurePrefabsExist()
    {
        if (kingPiecePrefab == null)
        {
            kingPiecePrefab = CreateKingPiecePrefab();
            Debug.Log("GameManager: Created King prefab on demand");
        }
        
        if (queenPiecePrefab == null)
        {
            queenPiecePrefab = CreateQueenPiecePrefab();
            Debug.Log("GameManager: Created Queen prefab on demand");
        }
        
        if (bishopPiecePrefab == null)
        {
            bishopPiecePrefab = CreateBishopPiecePrefab();
            Debug.Log("GameManager: Created Bishop prefab on demand");
        }
        
        if (knightPiecePrefab == null)
        {
            knightPiecePrefab = CreateKnightPiecePrefab();
            Debug.Log("GameManager: Created Knight prefab on demand");
        }
        
        if (rookPiecePrefab == null)
        {
            rookPiecePrefab = CreateRookPiecePrefab();
            Debug.Log("GameManager: Created Rook prefab on demand");
        }
        
        if (pawnPiecePrefab == null)
        {
            pawnPiecePrefab = CreatePawnPiecePrefab();
            Debug.Log("GameManager: Created Pawn prefab on demand");
        }
    }
    
    private void SetupRotationUI()
    {
        // Add BoardRotationUI component to this GameObject
        BoardRotationUI rotationUI = gameObject.GetComponent<BoardRotationUI>();
        if (rotationUI == null)
        {
            rotationUI = gameObject.AddComponent<BoardRotationUI>();
            Debug.Log("GameManager: Added BoardRotationUI component");
        }
        else
        {
            Debug.Log("GameManager: BoardRotationUI already exists");
        }
    }
    
    private void SetupCameraTarget()
    {
        // Find the camera controller and set target to board center
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            // Look for the Board Rotator (parent of the actual board)
            GameObject boardRotator = GameObject.Find("Board Rotator");
            if (boardRotator != null)
            {
                cameraController.SetTarget(boardRotator.transform);
                Debug.Log("GameManager: Camera target set to Board Rotator");
            }
            else
            {
                // Fallback: create a target at world center
                GameObject cameraTarget = new GameObject("Camera Target");
                cameraTarget.transform.position = Vector3.zero;
                cameraController.SetTarget(cameraTarget.transform);
                Debug.Log("GameManager: Created camera target at world center");
            }
        }
        else
        {
            Debug.LogWarning("GameManager: No CameraController found to set target");
        }
    }
    
    private void CreateChessBoardInstance()
    {
        // Create a simple ChessBoard GameObject for piece management
        // This works alongside the EmergencyChessBoard for grid visualization
        GameObject chessBoardObject = new GameObject("Chess Board");
        chessBoardObject.AddComponent<ChessBoard>();
        Debug.Log("GameManager: ChessBoard instance created");
    }
    
    private void CreateGameStateManager()
    {
        GameObject gameStateObject = new GameObject("Game State Manager");
        gameStateObject.AddComponent<GameStateManager>();
        Debug.Log("GameManager: GameStateManager created");
        
        // Create TurnManager for turn-based gameplay
        GameObject turnManagerObject = new GameObject("Turn Manager");
        turnManagerObject.AddComponent<TurnManager>();
        Debug.Log("GameManager: TurnManager created for turn-based gameplay");
    }
    
    /// <summary>
    /// Clean up any existing King pieces in the scene before creating the tray system
    /// </summary>
    private void CleanUpExistingKingPieces()
    {
        Debug.Log("GameManager: Cleaning up any existing King pieces in scene...");
        
        // Find ALL King components in the scene (more aggressive than ChessBoard cleanup)
        King[] allKings = FindObjectsByType<King>(FindObjectsSortMode.None);
        Debug.Log($"GameManager: Found {allKings.Length} King GameObjects in scene");
        
        int destroyedCount = 0;
        foreach (King king in allKings)
        {
            Debug.Log($"GameManager: Found King {king.pieceColor} {king.pieceType} at {king.transform.position}");
            Debug.Log($"  Parent: {(king.transform.parent != null ? king.transform.parent.name : "None")}");
            Debug.Log($"  GameObject: {king.gameObject.name}");
            Debug.Log($"  CurrentPosition valid: {king.CurrentPosition.IsValid()}");
            
            // Destroy ALL Kings found in scene - we'll recreate them properly in trays
            Debug.Log($"GameManager: Destroying King GameObject '{king.gameObject.name}'");
            DestroyImmediate(king.gameObject);
            destroyedCount++;
        }
        
        // Also search for any ChessPiece components (broader search)
        ChessPiece[] allPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
        Debug.Log($"GameManager: Found {allPieces.Length} ChessPiece GameObjects in scene");
        
        foreach (ChessPiece piece in allPieces)
        {
            if (piece != null) // Check if not already destroyed
            {
                Debug.Log($"GameManager: Found ChessPiece {piece.pieceColor} {piece.pieceType} at {piece.transform.position}");
                Debug.Log($"GameManager: Destroying ChessPiece GameObject '{piece.gameObject.name}'");
                DestroyImmediate(piece.gameObject);
                destroyedCount++;
            }
        }
        
        Debug.Log($"GameManager: Destroyed {destroyedCount} total King/ChessPiece GameObjects from scene");
    }
    
    private void SetupPieceTrays()
    {
        if (traysCreated)
        {
            Debug.Log("GameManager: Trays already created, skipping...");
            return;
        }
        
        Debug.Log("GameManager: Setting up piece trays...");
        
        // Create white tray (left side) - NEW APPROACH: Initialize after component creation
        GameObject whiteTrayObject = new GameObject("White Tray");
        PieceTray whiteTray = whiteTrayObject.AddComponent<PieceTray>();
        whiteTray.Initialize(PieceColor.White, true); // Explicit initialization avoids lifecycle issues
        
        // Create black tray (right side) - NEW APPROACH: Initialize after component creation  
        GameObject blackTrayObject = new GameObject("Black Tray");
        PieceTray blackTray = blackTrayObject.AddComponent<PieceTray>();
        blackTray.Initialize(PieceColor.Black, false); // Explicit initialization avoids lifecycle issues
        
        traysCreated = true;
        Debug.Log("GameManager: Piece trays created and marked as completed");
    }
    
    private void SetupPlacementManager()
    {
        GameObject placementObject = new GameObject("Placement Manager");
        placementObject.AddComponent<PlacementManager>();
        Debug.Log("GameManager: PlacementManager created");
    }
    
    private void SetupPlacementUI()
    {
        GameObject placementUIObject = new GameObject("Placement UI");
        placementUIObject.AddComponent<PlacementUI>();
        Debug.Log("GameManager: PlacementUI created");
    }
    
    private void SetupPawnPromotionSystem()
    {
        // Create PawnPromotionManager
        GameObject promotionManagerObject = new GameObject("Pawn Promotion Manager");
        promotionManagerObject.AddComponent<PawnPromotionManager>();
        Debug.Log("GameManager: PawnPromotionManager created");
        
        // Create PawnPromotionUI
        GameObject promotionUIObject = new GameObject("Pawn Promotion UI");
        promotionUIObject.AddComponent<PawnPromotionUI>();
        Debug.Log("GameManager: PawnPromotionUI created");
        
        Debug.Log("GameManager: Pawn promotion system setup complete");
    }
    
    private void SetupCheckDetectionSystem()
    {
        // Create CheckDetectionManager
        GameObject checkDetectionObject = new GameObject("Check Detection Manager");
        checkDetectionObject.AddComponent<CheckDetectionManager>();
        Debug.Log("GameManager: CheckDetectionManager created");
        
        // Create GameEndDetectionManager 
        GameObject gameEndDetectionObject = new GameObject("Game End Detection Manager");
        gameEndDetectionObject.AddComponent<GameEndDetectionManager>();
        Debug.Log("GameManager: GameEndDetectionManager created");
        
        // Create CheckVisualFeedbackManager
        GameObject visualFeedbackObject = new GameObject("Check Visual Feedback Manager");
        visualFeedbackObject.AddComponent<CheckVisualFeedbackManager>();
        Debug.Log("GameManager: CheckVisualFeedbackManager created");
        
        // Create ThreatIndicatorManager for orange threat indicators
        GameObject threatIndicatorObject = new GameObject("Threat Indicator Manager");
        threatIndicatorObject.AddComponent<ThreatIndicatorManager>();
        Debug.Log("GameManager: ThreatIndicatorManager created");
        
        // NOTE: CheckStatusUI disabled - using persistent status display instead
        // GameObject checkStatusUIObject = new GameObject("Check Status UI");
        // checkStatusUIObject.AddComponent<CheckStatusUI>();
        // Debug.Log("GameManager: CheckStatusUI created");
        
        // Create UIManager for coordinated UI system (persistent check status only)
        GameObject uiManagerObject = new GameObject("UI Manager");
        uiManagerObject.AddComponent<UIManager>();
        Debug.Log("GameManager: UIManager created with persistent check status");
        
        // Create PiecePreviewRenderer for visual promotion buttons
        GameObject piecePreviewObject = new GameObject("Piece Preview Renderer");
        piecePreviewObject.AddComponent<PiecePreviewRenderer>();
        Debug.Log("GameManager: PiecePreviewRenderer created");
        
        Debug.Log("GameManager: Check detection system setup complete");
    }
    
    private void SetupPiecesInTrays()
    {
        // Create piece prefabs if they don't exist
        if (kingPiecePrefab == null)
        {
            kingPiecePrefab = CreateKingPiecePrefab();
        }
        
        if (queenPiecePrefab == null)
        {
            queenPiecePrefab = CreateQueenPiecePrefab();
        }
        
        if (bishopPiecePrefab == null)
        {
            bishopPiecePrefab = CreateBishopPiecePrefab();
        }
        
        if (knightPiecePrefab == null)
        {
            knightPiecePrefab = CreateKnightPiecePrefab();
        }
        
        if (rookPiecePrefab == null)
        {
            rookPiecePrefab = CreateRookPiecePrefab();
        }
        
        if (pawnPiecePrefab == null)
        {
            pawnPiecePrefab = CreatePawnPiecePrefab();
        }
        
        Debug.Log("🎭 GameManager: Starting piece creation in trays...");
        
        // Check if trays are ready (with improved error handling)
        if (PieceTray.WhiteTray == null || PieceTray.BlackTray == null)
        {
            Debug.LogError("GameManager: Trays not ready! Cannot create pieces.");
            Debug.LogError($"WhiteTray: {(PieceTray.WhiteTray != null ? "Ready" : "NULL")}");
            Debug.LogError($"BlackTray: {(PieceTray.BlackTray != null ? "Ready" : "NULL")}");
            
            // Try to find existing trays in scene for debugging
            PieceTray[] existingTrays = FindObjectsByType<PieceTray>(FindObjectsSortMode.None);
            Debug.Log($"GameManager: Found {existingTrays.Length} existing tray objects in scene");
            
            foreach (PieceTray tray in existingTrays)
            {
                Debug.Log($"  Existing tray: {tray.name}, color: {tray.trayColor}, static reference set: {(tray.trayColor == PieceColor.White ? PieceTray.WhiteTray != null : PieceTray.BlackTray != null)}");
            }
            
            return;
        }
        
        Debug.Log($"🎭 GameManager: Trays ready! WhiteTray: {PieceTray.WhiteTray.name}, BlackTray: {PieceTray.BlackTray.name}");
        
        // Create pieces and add them to trays (not the board) - 1 King + 1 Queen + 2 Bishops + 2 Knights + 2 Rooks per side
        CreatePieceInTray(PieceColor.White, ChessPieceType.King);
        CreatePieceInTray(PieceColor.White, ChessPieceType.Queen);
        CreatePieceInTray(PieceColor.White, ChessPieceType.Bishop);
        CreatePieceInTray(PieceColor.White, ChessPieceType.Bishop);
        CreatePieceInTray(PieceColor.White, ChessPieceType.Knight);
        CreatePieceInTray(PieceColor.White, ChessPieceType.Knight);
        CreatePieceInTray(PieceColor.White, ChessPieceType.Rook);
        CreatePieceInTray(PieceColor.White, ChessPieceType.Rook);
        
        CreatePieceInTray(PieceColor.Black, ChessPieceType.King);
        CreatePieceInTray(PieceColor.Black, ChessPieceType.Queen);
        CreatePieceInTray(PieceColor.Black, ChessPieceType.Bishop);
        CreatePieceInTray(PieceColor.Black, ChessPieceType.Bishop);
        CreatePieceInTray(PieceColor.Black, ChessPieceType.Knight);
        CreatePieceInTray(PieceColor.Black, ChessPieceType.Knight);
        CreatePieceInTray(PieceColor.Black, ChessPieceType.Rook);
        CreatePieceInTray(PieceColor.Black, ChessPieceType.Rook);
        
        Debug.Log("🎭 GameManager: All pieces created in trays");
        
        // Validate that all pieces were successfully placed in trays
        int expectedPiecesPerTray = 8; // King, Queen, 2 Bishops, 2 Knights, 2 Rooks
        int whiteTrayCount = PieceTray.WhiteTray != null ? PieceTray.WhiteTray.GetPieceCount() : 0;
        int blackTrayCount = PieceTray.BlackTray != null ? PieceTray.BlackTray.GetPieceCount() : 0;
        
        Debug.Log($"🎭 White tray has {whiteTrayCount}/{expectedPiecesPerTray} pieces");
        Debug.Log($"🎭 Black tray has {blackTrayCount}/{expectedPiecesPerTray} pieces");
        
        if (whiteTrayCount == expectedPiecesPerTray && blackTrayCount == expectedPiecesPerTray)
        {
            Debug.Log("✅ SUCCESS: All pieces successfully placed in trays!");
        }
        else
        {
            Debug.LogError($"❌ VALIDATION FAILED: Expected {expectedPiecesPerTray} pieces per tray");
            Debug.LogError($"  White tray: {whiteTrayCount}/{expectedPiecesPerTray} pieces");
            Debug.LogError($"  Black tray: {blackTrayCount}/{expectedPiecesPerTray} pieces");
            
            if (whiteTrayCount < expectedPiecesPerTray || blackTrayCount < expectedPiecesPerTray)
            {
                Debug.LogError("  Some pieces may have been destroyed or failed to place in trays");
            }
        }
    }
    
    private void CreatePieceInTray(PieceColor color, ChessPieceType pieceType)
    {
        Debug.Log($"🎭 GameManager.CreatePieceInTray: Creating {color} {pieceType}");
        
        // Select the appropriate prefab based on piece type
        GameObject prefabToUse = null;
        string prefabName = "";
        
        switch (pieceType)
        {
            case ChessPieceType.King:
                prefabToUse = kingPiecePrefab;
                prefabName = "King";
                break;
            case ChessPieceType.Queen:
                prefabToUse = queenPiecePrefab;
                prefabName = "Queen";
                break;
            case ChessPieceType.Bishop:
                prefabToUse = bishopPiecePrefab;
                prefabName = "Bishop";
                break;
            case ChessPieceType.Knight:
                prefabToUse = knightPiecePrefab;
                prefabName = "Knight";
                break;
            case ChessPieceType.Rook:
                prefabToUse = rookPiecePrefab;
                prefabName = "Rook";
                break;
            default:
                Debug.LogError($"🎭❌ Unsupported piece type: {pieceType}");
                return;
        }
        
        if (prefabToUse == null)
        {
            Debug.LogError($"🎭❌ No prefab available for {pieceType}");
            return;
        }
        
        Debug.Log($"  Using {prefabName} prefab: {prefabToUse.name}");
        
        GameObject pieceObject = Instantiate(prefabToUse);
        pieceObject.name = $"{color} {pieceType}";
        // CRITICAL FIX: Do NOT activate yet - set properties first
        Debug.Log($"  Created GameObject: {pieceObject.name} at position {pieceObject.transform.position}");
        
        // Get the ChessPiece component (works for both King and Queen)
        ChessPiece piece = pieceObject.GetComponent<ChessPiece>();
        if (piece != null)
        {
            Debug.Log($"  Found ChessPiece component: {piece.GetType().Name}");
            Debug.Log($"  Piece initial state - Color: {piece.pieceColor}, Type: {piece.pieceType}, Position: {piece.CurrentPosition}");
            
            // Set properties BEFORE activation to prevent timing issues
            piece.pieceColor = color;
            piece.pieceType = pieceType;
            
            // CRITICAL FIX: Set tray pieces to have INVALID position to prevent board contamination
            piece.SetCurrentPosition(new BoardPosition(-1, -1, -1));
            Debug.Log($"  Set tray piece to invalid position (-1, -1, -1) to prevent board contamination");
            
            // CRITICAL: Apply material after setting color to ensure correct appearance
            piece.ApplyMaterial();
            Debug.Log($"  Applied {color} material to piece");
            
            Debug.Log($"  Set piece properties - Color: {piece.pieceColor}, Type: {piece.pieceType}");
            
            // NOW activate the piece with all properties properly set
            pieceObject.SetActive(true);
            Debug.Log($"  Activated piece after setting properties");
            
            // Add piece to appropriate tray
            PieceTray targetTray = (color == PieceColor.White) ? PieceTray.WhiteTray : PieceTray.BlackTray;
            Debug.Log($"  Target tray: {(targetTray != null ? targetTray.name : "NULL")}");
            
            if (targetTray != null)
            {
                Debug.Log($"  Attempting to add piece to tray...");
                bool success = targetTray.AddPiece(piece);
                Debug.Log($"  AddPiece result: {success}");
                
                if (success)
                {
                    Debug.Log($"🎭✅ Successfully added {color} {pieceType} to tray");
                    Debug.Log($"  Final piece position: {piece.transform.position}");
                    Debug.Log($"  Final piece parent: {(piece.transform.parent != null ? piece.transform.parent.name : "None")}");
                    Debug.Log($"  Piece CurrentPosition: {piece.CurrentPosition} (should be invalid for tray pieces)");
                }
                else
                {
                    Debug.LogError($"🎭❌ CRITICAL: Failed to add {color} {pieceType} to tray - tray may be full!");
                    Debug.LogError($"  Tray capacity: {targetTray.maxPieces}, Current count: {targetTray.GetPieceCount()}");
                    
                    // Destroy the piece rather than letting it auto-position on board
                    Debug.LogError($"  Destroying piece to prevent unwanted board placement");
                    DestroyImmediate(pieceObject);
                    return;
                }
            }
            else
            {
                Debug.LogError($"🎭 No tray found for {color} pieces");
            }
        }
        else
        {
            Debug.LogError($"🎭❌ Created piece object does not have ChessPiece component");
        }
    }
    
    private GameObject CreateKingPiecePrefab()
    {
        Debug.Log("🎭 GameManager.CreateKingPiecePrefab: Creating King prefab template...");
        
        GameObject prefab = new GameObject("King Piece Prefab");
        
        // CRITICAL: Deactivate during setup to prevent Start() from being called
        prefab.SetActive(false);
        
        // Add visual representation - capsule pill shape
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.transform.SetParent(prefab.transform);
        
        // Position piece so bottom rests on floor plane
        // Cell height is 2.8, piece height should be 0.75 * 2.8 = 2.1
        // Capsule default height is 2, so scale Y to 1.05 to get 2.1 height
        // Position up by half the height (1.05) so bottom touches floor
        visual.transform.localPosition = new Vector3(0, 1.05f, 0);
        visual.transform.localScale = new Vector3(0.8f, 1.05f, 0.8f); // Slightly wider, 2.1 units tall
        
        // Remove the collider from the visual object
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            DestroyImmediate(visualCollider);
        }
        
        // Add larger invisible collider for better mobile touch targets
        SphereCollider collider = prefab.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 1.05f, 0);
        collider.radius = 1.2f; // Larger than visual for easier touching
        
        // Add the King script
        King kingScript = prefab.AddComponent<King>();
        
        // Set the piece tag for input detection
        prefab.tag = "ChessPiece";
        
        Debug.Log("🎭 GameManager.CreateKingPiecePrefab: Prefab created successfully (inactive)");
        
        return prefab;
    }
    
    private GameObject CreateQueenPiecePrefab()
    {
        Debug.Log("🎭 GameManager.CreateQueenPiecePrefab: Creating Queen prefab template...");
        
        GameObject prefab = new GameObject("Queen Piece Prefab");
        
        // CRITICAL: Deactivate during setup to prevent Start() from being called
        prefab.SetActive(false);
        
        // Add visual representation - cylinder for Queen (taller/different from King's capsule)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visual.transform.SetParent(prefab.transform);
        
        // Position piece so bottom rests on floor plane
        // Cell height is 2.8, piece height should be 0.85 * 2.8 = 2.38 (taller than King)
        // Cylinder default height is 2, so scale Y to 1.19 to get 2.38 height
        // Position up by half the height (1.19) so bottom touches floor
        visual.transform.localPosition = new Vector3(0, 1.19f, 0);
        visual.transform.localScale = new Vector3(0.7f, 1.19f, 0.7f); // Narrower but taller than King
        
        // Remove the collider from the visual object
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            DestroyImmediate(visualCollider);
        }
        
        // Add larger invisible collider for better mobile touch targets
        SphereCollider collider = prefab.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 1.19f, 0);
        collider.radius = 1.3f; // Slightly larger than King for easier touching
        
        // Add the Queen script
        Queen queenScript = prefab.AddComponent<Queen>();
        
        // Set the piece tag for input detection
        prefab.tag = "ChessPiece";
        
        Debug.Log("🎭 GameManager.CreateQueenPiecePrefab: Prefab created successfully (inactive)");
        
        return prefab;
    }
    
    private GameObject CreateBishopPiecePrefab()
    {
        Debug.Log("🎭 GameManager.CreateBishopPiecePrefab: Creating Bishop prefab template...");
        
        GameObject prefab = new GameObject("Bishop Piece Prefab");
        
        // CRITICAL: Deactivate during setup to prevent Start() from being called
        prefab.SetActive(false);
        
        // Add visual representation - cone/pyramid for Bishop (distinctive diagonal shape)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(prefab.transform);
        
        // Position piece so bottom rests on floor plane
        // Cell height is 2.8, piece height should be 0.8 * 2.8 = 2.24 (between King and Queen)
        // Cube default height is 1, so scale Y to 2.24 to get proper height
        // Position up by half the height (1.12) so bottom touches floor
        visual.transform.localPosition = new Vector3(0, 1.12f, 0);
        visual.transform.localScale = new Vector3(0.6f, 2.24f, 0.6f); // Tall narrow cube resembling traditional bishop
        
        // Rotate 45 degrees to make it diamond-shaped from above (distinctive)
        visual.transform.localRotation = Quaternion.Euler(0, 45f, 0);
        
        // Remove the collider from the visual object
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            DestroyImmediate(visualCollider);
        }
        
        // Add larger invisible collider for better mobile touch targets
        SphereCollider collider = prefab.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 1.12f, 0);
        collider.radius = 1.2f; // Same as King for consistency
        
        // Add the Bishop script
        Bishop bishopScript = prefab.AddComponent<Bishop>();
        
        // Set the piece tag for input detection
        prefab.tag = "ChessPiece";
        
        Debug.Log("🎭 GameManager.CreateBishopPiecePrefab: Prefab created successfully (inactive)");
        
        return prefab;
    }
    
    private GameObject CreateKnightPiecePrefab()
    {
        Debug.Log("🎭 GameManager.CreateKnightPiecePrefab: Creating Knight prefab template...");
        
        GameObject prefab = new GameObject("Knight Piece Prefab");
        
        // CRITICAL: Deactivate during setup to prevent Start() from being called
        prefab.SetActive(false);
        
        // Add visual representation - L-shaped geometry for Knight (distinctive movement pattern)
        GameObject visual1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject visual2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual1.transform.SetParent(prefab.transform);
        visual2.transform.SetParent(prefab.transform);
        
        // Position pieces to create an L-shape representing Knight's movement
        // Cell height is 2.8, piece height should be 0.75 * 2.8 = 2.1 (similar to King)
        // Create L-shape: vertical part (2.1 tall) and horizontal part (extends sideways)
        
        // Vertical part of L - tall narrow cube
        visual1.transform.localPosition = new Vector3(0, 1.05f, 0);
        visual1.transform.localScale = new Vector3(0.4f, 2.1f, 0.4f); // Tall and narrow
        
        // Horizontal part of L - extends to the side
        visual2.transform.localPosition = new Vector3(0.4f, 0.3f, 0);
        visual2.transform.localScale = new Vector3(0.8f, 0.6f, 0.4f); // Wide and low
        
        // Remove colliders from visual objects
        Collider visualCollider1 = visual1.GetComponent<Collider>();
        if (visualCollider1 != null)
        {
            DestroyImmediate(visualCollider1);
        }
        
        Collider visualCollider2 = visual2.GetComponent<Collider>();
        if (visualCollider2 != null)
        {
            DestroyImmediate(visualCollider2);
        }
        
        // Add larger invisible collider for better mobile touch targets
        SphereCollider collider = prefab.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 1.05f, 0);
        collider.radius = 1.3f; // Larger for easier touching (distinctive from other pieces)
        
        // Add the Knight script
        Knight knightScript = prefab.AddComponent<Knight>();
        
        // Set the piece tag for input detection
        prefab.tag = "ChessPiece";
        
        Debug.Log("🎭 GameManager.CreateKnightPiecePrefab: Prefab created successfully (inactive)");
        
        return prefab;
    }
    
    private GameObject CreateRookPiecePrefab()
    {
        Debug.Log("🎭 GameManager.CreateRookPiecePrefab: Creating Rook prefab template...");
        
        GameObject prefab = new GameObject("Rook Piece Prefab");
        
        // CRITICAL: Deactivate during setup to prevent Start() from being called
        prefab.SetActive(false);
        
        // Add visual representation - simplified castle tower (following Knight pattern)
        GameObject baseVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject towerVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseVisual.transform.SetParent(prefab.transform);
        towerVisual.transform.SetParent(prefab.transform);
        
        // Position piece so bottom rests on floor plane (following other pieces' pattern)
        // Cell height is 2.8, piece height should be 0.9 * 2.8 = 2.52 (tallest after Queen)
        // Position up by half the total height so bottom touches floor
        
        // Base of castle - wide foundation at bottom
        baseVisual.transform.localPosition = new Vector3(0, 0.3f, 0);
        baseVisual.transform.localScale = new Vector3(1.0f, 0.6f, 1.0f); // Wide and low base
        
        // Tower body - main vertical structure
        towerVisual.transform.localPosition = new Vector3(0, 1.26f, 0);
        towerVisual.transform.localScale = new Vector3(0.7f, 1.9f, 0.7f); // Tall central tower
        
        // Remove colliders from visual objects
        Collider baseCollider = baseVisual.GetComponent<Collider>();
        if (baseCollider != null)
        {
            DestroyImmediate(baseCollider);
        }
        
        Collider towerCollider = towerVisual.GetComponent<Collider>();
        if (towerCollider != null)
        {
            DestroyImmediate(towerCollider);
        }
        
        // Add larger invisible collider for better mobile touch targets (following Knight pattern)
        SphereCollider collider = prefab.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 1.26f, 0); // Center on tower middle
        collider.radius = 1.3f; // Large touch target
        
        // Add the Rook script
        Rook rookScript = prefab.AddComponent<Rook>();
        
        // Set the piece tag for input detection
        prefab.tag = "ChessPiece";
        
        Debug.Log("🎭 GameManager.CreateRookPiecePrefab: Prefab created successfully (inactive)");
        
        return prefab;
    }
    
    private GameObject CreatePawnPiecePrefab()
    {
        Debug.Log("🎭 GameManager.CreatePawnPiecePrefab: Creating Pawn prefab template...");
        
        GameObject prefab = new GameObject("Pawn Piece Prefab");
        
        // CRITICAL: Deactivate during setup to prevent Start() from being called
        prefab.SetActive(false);
        
        // Add visual representation - simple sphere for Pawn (distinctive from all other pieces)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(prefab.transform);
        
        // Position piece so bottom rests on floor plane
        // Cell height is 2.8, piece height should be 0.6 * 2.8 = 1.68 (smallest piece)
        // Sphere default diameter is 1, so scale to 1.68 to get proper height
        // Position up by half the height (0.84) so bottom touches floor
        visual.transform.localPosition = new Vector3(0, 0.84f, 0);
        visual.transform.localScale = new Vector3(1.68f, 1.68f, 1.68f); // Uniform sphere scaling
        
        // Remove the collider from the visual object
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            DestroyImmediate(visualCollider);
        }
        
        // Add larger invisible collider for better mobile touch targets
        SphereCollider collider = prefab.AddComponent<SphereCollider>();
        collider.center = new Vector3(0, 0.84f, 0);
        collider.radius = 1.1f; // Smaller than other pieces but still easy to touch
        
        // Add the Pawn script
        Pawn pawnScript = prefab.AddComponent<Pawn>();
        
        // Set the piece tag for input detection
        prefab.tag = "ChessPiece";
        
        Debug.Log("🎭 GameManager.CreatePawnPiecePrefab: Prefab created successfully (inactive)");
        
        return prefab;
    }
    
    private void CreateKingPiece(BoardPosition position, PieceColor color)
    {
        Debug.Log($"🎭 GameManager.CreateKingPiece: Starting creation of {color} King at {position}");
        
        if (ChessBoard.Instance == null) 
        {
            Debug.LogError("GameManager: Cannot create King piece - ChessBoard.Instance is null!");
            return;
        }
        
        Debug.Log($"GameManager: ChessBoard.Instance confirmed available for {color} King");
        
        GameObject pieceObject = Instantiate(kingPiecePrefab);
        pieceObject.name = $"{color} King at {position}"; // Better naming for debugging
        King piece = pieceObject.GetComponent<King>();
        
        Debug.Log($"GameManager: Instantiated piece object '{pieceObject.name}' with King component: {piece != null}");
        
        // Make piece a child of the rotating board structure
        GameObject piecesContainer = GameObject.Find("Pieces Container");
        if (piecesContainer != null)
        {
            pieceObject.transform.SetParent(piecesContainer.transform);
            Debug.Log($"GameManager: Set piece parent to Pieces Container");
        }
        else
        {
            Debug.LogError("GameManager: Pieces Container not found! Creating piece without parent - this will break rotation!");
            // Try to find any emergency board structure
            GameObject boardRotator = GameObject.Find("Board Rotator");
            if (boardRotator != null)
            {
                pieceObject.transform.SetParent(boardRotator.transform);
                Debug.Log($"GameManager: Fallback - set piece parent to Board Rotator");
            }
        }
        
        if (piece != null)
        {
            Debug.Log($"GameManager: About to call piece.Initialize({position}, {color})");
            piece.Initialize(position, color);
            Debug.Log($"GameManager: piece.Initialize() completed");
            
            // Verify the piece was actually registered
            ChessPiece verifyRegistration = ChessBoard.Instance.GetPieceAt(position);
            if (verifyRegistration == piece)
            {
                Debug.Log($"✅ GameManager: REGISTRATION SUCCESSFUL - {color} King at {position}");
            }
            else
            {
                Debug.LogError($"❌ GameManager: REGISTRATION FAILED - {color} King at {position} not found in board!");
                Debug.LogError($"   Expected: {piece.GetType().Name}, Got: {(verifyRegistration != null ? verifyRegistration.GetType().Name : "NULL")}");
            }
            
            Vector3 expectedLocal = ChessBoard.Instance.BoardToLocalPosition(position);
            Debug.Log($"GameManager: Created {color} King at board {position}");
            Debug.Log($"  Expected local position: {expectedLocal}");
            Debug.Log($"  Actual local position: {pieceObject.transform.localPosition}");
            Debug.Log($"  Parent: {(pieceObject.transform.parent != null ? pieceObject.transform.parent.name : "None")}");
        }
        else
        {
            Debug.LogError("GameManager: Failed to get King component!");
        }
        
        Debug.Log($"🎭 GameManager.CreateKingPiece: Completed creation of {color} King at {position}");
    }
    
    private void Update()
    {
        // DIAGNOSTIC: Check InputManager status every 5 seconds - DISABLED to prevent log spam
        if (false && Time.time > 5f && Mathf.FloorToInt(Time.time) % 5 == 0 && Time.time % 1f < 0.1f)
        {
            Debug.Log("=== GAMEMANAGER DIAGNOSTIC ===");
            Debug.Log($"InputManager.Instance exists: {InputManager.Instance != null}");
            
            if (InputManager.Instance != null)
            {
                GameObject inputGO = InputManager.Instance.gameObject;
                Debug.Log($"InputManager GameObject: {inputGO.name}");
                Debug.Log($"InputManager active: {inputGO.activeInHierarchy}");
                Debug.Log($"InputManager enabled: {InputManager.Instance.enabled}");
                
                // Force verify all components
                MonoBehaviour[] components = inputGO.GetComponents<MonoBehaviour>();
                Debug.Log($"InputManager GameObject has {components.Length} MonoBehaviour components:");
                foreach (var comp in components)
                {
                    if (comp != null)
                    {
                        Debug.Log($"  - {comp.GetType().Name}: enabled={comp.enabled}");
                    }
                    else
                    {
                        Debug.Log($"  - NULL component found!");
                    }
                }
            }
            else
            {
                Debug.LogError("InputManager.Instance is NULL! Trying to recreate...");
                
                // Try to find existing InputManager in scene
                InputManager[] found = FindObjectsByType<InputManager>(FindObjectsSortMode.None);
                Debug.Log($"Found {found.Length} InputManager instances in scene");
                
                if (found.Length == 0)
                {
                    Debug.Log("Creating new InputManager GameObject...");
                    GameObject inputObject = new GameObject("InputManager - Recreated");
                    inputObject.AddComponent<InputManager>();
                    Debug.Log("InputManager recreated!");
                }
                else
                {
                    Debug.Log($"Found existing InputManager on GameObject: {found[0].gameObject.name}");
                }
            }
        }
        
        // Handle escape key to clear selection (useful for testing)
        if (InputHelper.GetKeyDown(KeyCode.Escape))
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.ClearSelection();
            }
        }
        
        // Handle camera control shortcuts
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            // I key for Interior zoom (close-up preset)
            if (InputHelper.GetKeyDown(KeyCode.I))
            {
                cameraController.SetZoomPreset(5f); // Close zoom preset (5 units)
                Debug.Log("🎥 Zoomed to Interior preset (5 units) - press O for Overview");
            }
            
            // O key for Overview zoom (far preset)
            if (InputHelper.GetKeyDown(KeyCode.O))
            {
                cameraController.SetZoomPreset(20f); // Far zoom preset (20 units)
                Debug.Log("🎥 Zoomed to Overview preset (20 units) - press I for Interior");
            }
            
            // R key to reset camera
            if (InputHelper.GetKeyDown(KeyCode.R))
            {
                cameraController.ResetToDefaultView();
                Debug.Log("🎥 Camera reset to default view");
            }
        }
        
        // Display helpful controls info (for first few seconds) - FIXED: Only print once every 4 seconds
        if (Time.time < 20f && Time.time > 3f && Mathf.FloorToInt(Time.time) % 4 == 0 && Time.time % 1f < Time.deltaTime)
        {
            Debug.Log("🎮 CONTROLS: Right-click+drag = Camera Orbit | Q/E = Rotate Board | I = Close Zoom (5 units) | O = Far Zoom (20 units) | R = Reset | Mouse wheel/Pinch = Continuous Zoom (3-35 units) | Left-click = Select");
        }
    }
}