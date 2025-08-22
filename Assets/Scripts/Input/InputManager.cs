using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    public Camera gameCamera;
    public LayerMask interactableLayer = -1;
    
    private ChessPiece selectedPiece;
    private bool isProcessingInput = false;
    
    public static InputManager Instance { get; private set; }
    
    private void Awake()
    {
        Debug.Log("🚀 InputManager.Awake: CALLED - InputManager starting up!");
        Debug.Log($"InputManager.Awake: GameObject name={gameObject.name}");
        Debug.Log($"InputManager.Awake: Time.time={Time.time}");
        
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("✅ InputManager.Awake: Set as singleton Instance");
        }
        else
        {
            Debug.LogWarning("⚠️ InputManager.Awake: Another instance already exists, destroying this one");
            Destroy(gameObject);
            return;
        }
        
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
            Debug.Log($"InputManager.Awake: Camera set to {(gameCamera != null ? gameCamera.name : "NULL")}");
        }
        
        Debug.Log("✅ InputManager.Awake: Initialization complete, Update should begin next frame");
    }
    
    private void Start()
    {
        // Subscribe to turn change events to automatically deselect pieces when turn changes
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged += OnTurnChanged;
            Debug.Log("InputManager: Subscribed to TurnManager.OnTurnChanged");
        }
        else
        {
            Debug.LogWarning("InputManager: TurnManager.Instance not found - cannot subscribe to turn changes");
        }
        
        // Subscribe to game state changes to clear selection when transitioning states
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
            Debug.Log("InputManager: Subscribed to GameStateManager.OnStateChanged");
        }
        else
        {
            Debug.LogWarning("InputManager: GameStateManager.Instance not found - cannot subscribe to state changes");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged -= OnTurnChanged;
        }
        
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }
    
    /// <summary>
    /// Handle turn change events - automatically deselect pieces from previous turn
    /// </summary>
    private void OnTurnChanged(PieceColor newCurrentPlayer)
    {
        Debug.Log($"InputManager.OnTurnChanged: Turn changed to {newCurrentPlayer}");
        
        // If we have a piece selected and it's not the current player's turn, deselect it
        if (selectedPiece != null && selectedPiece.pieceColor != newCurrentPlayer)
        {
            Debug.Log($"InputManager.OnTurnChanged: Auto-deselecting {selectedPiece.pieceColor} {selectedPiece.pieceType} - not their turn");
            DeselectPiece();
        }
    }
    
    /// <summary>
    /// Handle game state changes - clear selection when transitioning between states
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        Debug.Log($"InputManager.OnGameStateChanged: Game state changed to {newState}");
        
        // Clear any selected piece when transitioning to Playing state to ensure clean start
        if (newState == GameState.Playing && selectedPiece != null)
        {
            Debug.Log($"InputManager.OnGameStateChanged: Clearing selection on transition to Playing state");
            DeselectPiece();
        }
        
        // Also clear selection when leaving Playing state
        if (newState != GameState.Playing && selectedPiece != null)
        {
            Debug.Log($"InputManager.OnGameStateChanged: Clearing selection on transition from Playing state");
            DeselectPiece();
        }
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (isProcessingInput) 
        {
            Debug.Log("HandleInput: Skipping - input processing in progress");
            return;
        }
        
        // Check for input
        bool touchInput = InputHelper.TouchCount > 0;
        bool mouseInput = InputHelper.GetMouseButtonDown(0);
        
        // Handle touch input for iOS using InputHelper
        if (touchInput)
        {
            Touch touch = InputHelper.GetTouch(0);
            Debug.Log($"HandleInput: Touch detected - phase={touch.phase}, position={touch.position}");
            if (touch.phase == TouchPhase.Began)
            {
                Debug.Log("HandleInput: Processing touch input");
                ProcessTouch(touch.position);
            }
        }
        
        // Handle mouse input for testing in editor using InputHelper
        if (mouseInput)
        {
            Vector3 mousePos = InputHelper.MousePosition;
            Debug.Log($"HandleInput: Mouse click detected at {mousePos}");
            ProcessTouch(mousePos);
        }
        
        // FALLBACK: Direct Input system for testing (bypass InputHelper)
        try 
        {
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("HandleInput: FALLBACK - Direct Input.GetMouseButtonDown detected");
                ProcessTouch(Input.mousePosition);
            }
        }
        catch (System.InvalidOperationException e)
        {
            if (Time.frameCount % 300 == 0) // Log every 5 seconds
            {
                Debug.LogWarning($"HandleInput: Direct input still failing - {e.Message}");
            }
        }
        
        // Keyboard testing
        if (InputHelper.GetKeyDown(KeyCode.Space))
        {
            // Select first piece for testing
            ChessPiece[] allPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
            if (allPieces.Length > 0)
            {
                OnPieceClicked(allPieces[0]);
            }
        }
    }
    
    private void ProcessTouch(Vector2 screenPosition)
    {
        // Check if game state allows any interaction
        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning("ProcessTouch: GameStateManager not available, ignoring input");
            return;
        }
        
        if (!GameStateManager.Instance.IsGameActive())
        {
            return;
        }
        
        if (gameCamera == null) 
        {
            Debug.LogError("ProcessTouch: gameCamera is NULL! Cannot perform raycast.");
            return;
        }
        
        Ray ray = gameCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, interactableLayer))
        {
            // Check for placement target clicks first (during placement phase)
            PlacementTargetData placementData = hit.collider.GetComponent<PlacementTargetData>();
            if (placementData != null && GameStateManager.Instance != null && GameStateManager.Instance.CanPlacePieces())
            {
                if (PlacementManager.Instance != null)
                {
                    PlacementManager.Instance.OnPlacementTargetClicked(placementData.targetPosition);
                }
                return;
            }
            
            // Check for move target clicks (during play phase) AND placement targets (during placement phase)
            MoveTargetData moveData = hit.collider.GetComponent<MoveTargetData>();
            if (moveData != null && GameStateManager.Instance != null)
            {
                if (GameStateManager.Instance.CanMovePieces())
                {
                    // During play phase - handle as move target
                    OnMoveTargetClicked(moveData.targetPosition);
                    return;
                }
                else if (GameStateManager.Instance.CanPlacePieces())
                {
                    // During placement phase - handle as placement target
                    Debug.Log($"InputManager: PLACEMENT TARGET clicked at {moveData.targetPosition}");
                    if (PlacementManager.Instance != null)
                    {
                        PlacementManager.Instance.OnPlacementTargetClicked(moveData.targetPosition);
                    }
                    return;
                }
            }
            
            // Handle piece clicks
            ChessPiece hitPiece = hit.collider.GetComponent<ChessPiece>();
            if (hitPiece != null)
            {
                
                // Handle piece clicks based on game state
                if (GameStateManager.Instance.CanPlacePieces())
                {
                    // During placement phase - allow both tray piece selection and board piece repositioning
                    Debug.Log($"InputManager: PLACEMENT PHASE - Handling piece click for {hitPiece.pieceColor} {hitPiece.pieceType}");
                    
                    // Check if this piece is in a tray or on the board
                    bool pieceInTray = hitPiece.transform.IsChildOf(PieceTray.WhiteTray?.transform) || 
                                      hitPiece.transform.IsChildOf(PieceTray.BlackTray?.transform);
                    
                    // Additional validation: check for position sync issues
                    bool hasPositionIssues = !hitPiece.ValidatePosition();
                    
                    if (pieceInTray)
                    {
                        Debug.Log($"InputManager: Piece is in tray - allowing selection for placement");
                        if (PlacementManager.Instance != null)
                        {
                            PlacementManager.Instance.OnTrayPieceClicked(hitPiece);
                        }
                    }
                    else if (hasPositionIssues)
                    {
                        Debug.LogWarning($"🔧 InputManager: Piece {hitPiece.pieceColor} {hitPiece.pieceType} has position sync issues - attempting repair before repositioning");
                        
                        // Try to repair position first
                        if (hitPiece.RepairPosition())
                        {
                            Debug.Log($"🔧 Position repair successful - proceeding with repositioning");
                            if (PlacementManager.Instance != null)
                            {
                                PlacementManager.Instance.OnBoardPieceClickedForRepositioning(hitPiece);
                            }
                        }
                        else
                        {
                            Debug.LogError($"🔧 ❌ Position repair failed for {hitPiece.pieceColor} {hitPiece.pieceType} - cannot process click");
                            Debug.LogError($"🔧 Piece thinks it's at {hitPiece.CurrentPosition}, but position validation failed");
                        }
                    }
                    else
                    {
                        Debug.Log($"InputManager: Piece is on board with valid position - routing to PlacementManager for repositioning");
                        if (PlacementManager.Instance != null)
                        {
                            PlacementManager.Instance.OnBoardPieceClickedForRepositioning(hitPiece);
                        }
                    }
                }
                else if (GameStateManager.Instance.CanMovePieces())
                {
                    // During play phase - ONLY allow piece movement, enforce chess rules
                    Debug.Log($"InputManager: PLAY PHASE - Handling gameplay piece click for {hitPiece.pieceColor} {hitPiece.pieceType}");
                    OnPieceClicked(hitPiece);
                }
                else
                {
                    // Invalid game state for piece interaction
                    Debug.LogWarning($"InputManager: Cannot interact with pieces in current game state: {GameStateManager.Instance.currentState}");
                }
                return;
            }
            
            Debug.Log($"❌ Hit object '{hit.collider.name}' has no ChessPiece or MoveTargetData component");
            
            // Check what components the hit object does have
            Component[] components = hit.collider.GetComponents<Component>();
            Debug.Log($"   Hit object components: {string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name))}");
        }
        else
        {
            Debug.Log($"❌ Raycast hit NOTHING - check collider setup and layers");
            
            // Clicked empty space - if piece is selected, try to move to that world position
            if (selectedPiece != null && ChessBoard.Instance != null)
            {
                BoardPosition targetPos = ChessBoard.Instance.WorldToBoard(ray.GetPoint(10f)); // 10 units from camera
                if (targetPos.IsValid())
                {
                    Debug.Log($"Clicked empty space at world position, converted to board position: {targetPos}");
                    TryMovePiece(targetPos);
                }
            }
        }
    }
    
    public void OnPieceClicked(ChessPiece piece)
    {
        Debug.Log($"🎯 OnPieceClicked: Called with piece {(piece != null ? $"{piece.pieceColor} {piece.pieceType}" : "NULL")}");
        
        // During placement phase, allow selecting placed pieces for repositioning
        if (GameStateManager.Instance != null && GameStateManager.Instance.CanPlacePieces())
        {
            Debug.Log($"OnPieceClicked: Placement phase - allowing piece selection for repositioning {piece.pieceColor} {piece.pieceType}");
        }
        
        if (isProcessingInput) 
        {
            Debug.Log("OnPieceClicked: Skipping - input processing in progress");
            return;
        }
        
        if (piece == null) 
        {
            Debug.LogError("OnPieceClicked: Piece is NULL!");
            return;
        }
        
        // FIXED TURN VALIDATION: Check turn order BEFORE processing any moves (capture or selection)
        PieceColor currentPlayer = GameStateManager.Instance?.GetCurrentPlayer() ?? PieceColor.White;
        
        // If this is an enemy piece and we have a piece selected, treat it as a capture attempt
        // BUT FIRST: Validate that the selected piece can move (it's their turn)
        if (selectedPiece != null && piece.pieceColor != selectedPiece.pieceColor)
        {
            // Check if the selected piece's color is allowed to move
            if (GameStateManager.Instance != null && !GameStateManager.Instance.CanMovePiece(selectedPiece))
            {
                Debug.LogWarning($"🚫 OnPieceClicked: TURN VIOLATION - {selectedPiece.pieceColor} {selectedPiece.pieceType} cannot capture because it's {currentPlayer}'s turn!");
                ShowInvalidTurnFeedback(selectedPiece.pieceColor, currentPlayer);
                return;
            }
            
            Debug.Log($"OnPieceClicked: Enemy piece clicked - attempting capture of {piece.pieceColor} {piece.pieceType} with {selectedPiece.pieceColor} {selectedPiece.pieceType}");
            TryMovePiece(piece.CurrentPosition);
            return;
        }
        
        // For piece selection, check turn validation
        if (GameStateManager.Instance != null && !GameStateManager.Instance.CanMovePiece(piece))
        {
            Debug.LogWarning($"🚫 OnPieceClicked: TURN VIOLATION - {piece.pieceColor} tried to move but it's {currentPlayer}'s turn!");
            
            // Show visual feedback for invalid turn attempt
            ShowInvalidTurnFeedback(piece.pieceColor, currentPlayer);
            return;
        }
        
        // If no piece is selected, select this piece
        if (selectedPiece == null)
        {
            Debug.Log("OnPieceClicked: No piece currently selected - selecting this piece");
            SelectPiece(piece);
        }
        // If the same piece is clicked, deselect it
        else if (selectedPiece == piece)
        {
            DeselectPiece();
        }
        // If a different piece is clicked
        else
        {
            // If it's the same color, select the new piece (only if it's their turn)
            if (selectedPiece.pieceColor == piece.pieceColor)
            {
                DeselectPiece();
                SelectPiece(piece);
            }
            // If it's a different color, try to capture
            else
            {
                TryMovePiece(piece.CurrentPosition);
            }
        }
    }
    
    public void OnMoveTargetClicked(BoardPosition targetPosition)
    {
        Debug.Log($"🎯 OnMoveTargetClicked: Called with position {targetPosition}");
        Debug.Log($"OnMoveTargetClicked: Current time = {Time.time:F2}");
        
        if (isProcessingInput) 
        {
            Debug.LogWarning("OnMoveTargetClicked: Skipping - input processing in progress");
            return;
        }
        
        if (selectedPiece == null)
        {
            Debug.LogError("OnMoveTargetClicked: No piece selected, cannot move to target");
            return;
        }
        
        Debug.Log($"OnMoveTargetClicked: Selected piece = {selectedPiece.pieceColor} {selectedPiece.pieceType} at {selectedPiece.CurrentPosition}");
        Debug.Log($"OnMoveTargetClicked: Target position = {targetPosition}");
        Debug.Log($"OnMoveTargetClicked: About to call TryMovePiece({targetPosition})");
        
        TryMovePiece(targetPosition);
        
        Debug.Log($"OnMoveTargetClicked: TryMovePiece call completed");
    }
    
    public void OnCellClicked(BoardPosition position)
    {
        if (isProcessingInput) return;
        
        Debug.Log($"Cell clicked at {position}. Selected piece: {(selectedPiece != null ? $"{selectedPiece.pieceColor} {selectedPiece.pieceType} at {selectedPiece.CurrentPosition}" : "None")}");
        
        // If a piece is selected, try to move it to this position
        if (selectedPiece != null)
        {
            Debug.Log($"Attempting to move {selectedPiece.pieceType} to {position}");
            TryMovePiece(position);
        }
        else
        {
            // Check if there's a piece at this position to select
            ChessPiece pieceAtPosition = ChessBoard.Instance?.GetPieceAt(position);
            if (pieceAtPosition != null)
            {
                Debug.Log($"Found piece to select: {pieceAtPosition.pieceColor} {pieceAtPosition.pieceType}");
                SelectPiece(pieceAtPosition);
            }
            else
            {
                Debug.Log($"No piece at {position} and no piece selected");
            }
        }
    }
    
    private void SelectPiece(ChessPiece piece)
    {
        if (piece == null) return;
        
        // POSITION SYNC FIX: Validate and repair position before selection
        if (!piece.ValidatePosition())
        {
            Debug.LogWarning($"🔧 Position validation failed for {piece.pieceColor} {piece.pieceType}, attempting repair...");
            if (piece.RepairPosition())
            {
                Debug.Log($"✅ Position repair successful for {piece.pieceColor} {piece.pieceType}");
            }
            else
            {
                Debug.LogError($"❌ Position repair failed for {piece.pieceColor} {piece.pieceType}");
            }
        }
        
        selectedPiece = piece;
        selectedPiece.OnSelected();
        
        // Add visual feedback for mobile users
        ShowValidMoveIndicators();
        
        Debug.Log($"Selected {piece.pieceColor} {piece.pieceType} at {piece.CurrentPosition}");
    }
    
    private void DeselectPiece()
    {
        if (selectedPiece != null)
        {
            selectedPiece.OnDeselected();
            selectedPiece = null;
            HideValidMoveIndicators();
            
            Debug.Log("Piece deselected");
        }
    }
    
    private List<GameObject> moveIndicators = new List<GameObject>();
    
    private void ShowValidMoveIndicators()
    {
        if (selectedPiece == null) 
        {
            Debug.Log("ShowValidMoveIndicators: selectedPiece is null");
            return;
        }
        
        Debug.Log($"ShowValidMoveIndicators: Creating indicators for {selectedPiece.pieceColor} {selectedPiece.pieceType}");
        
        HideValidMoveIndicators(); // Clear any existing indicators
        
        var validMoves = selectedPiece.GetLegalMoves();
        Debug.Log($"ShowValidMoveIndicators: Found {validMoves.Count} legal moves");
        
        // During placement phase, filter moves to respect placement zone restrictions
        if (GameStateManager.Instance != null && GameStateManager.Instance.CanPlacePieces())
        {
            var placementValidMoves = new List<BoardPosition>();
            foreach (var move in validMoves)
            {
                if (PlacementManager.IsPositionInColorZone(move, selectedPiece.pieceColor))
                {
                    placementValidMoves.Add(move);
                }
            }
            validMoves = placementValidMoves;
            Debug.Log($"ShowValidMoveIndicators: After placement zone filtering: {validMoves.Count} moves");
        }
        
        foreach (var move in validMoves)
        {
            // Create a glowing sphere at each legal move position
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = $"MoveIndicator_{move.x}_{move.y}_{move.z}";
            
            // Note: Using MoveTargetData component for identification instead of tags
            
            // Make indicator a child of the rotating board so it rotates with the board
            GameObject piecesContainer = GameObject.Find("Pieces Container");
            if (piecesContainer != null)
            {
                indicator.transform.SetParent(piecesContainer.transform);
            }
            
            // Position at the center of the target cell using local coordinates
            if (ChessBoard.Instance != null)
            {
                Vector3 localPos = ChessBoard.Instance.BoardToLocalPosition(move);
                indicator.transform.localPosition = localPos + Vector3.up * 0.5f; // Slightly above floor
                Debug.Log($"ShowValidMoveIndicators: Created indicator for {move} at local position {localPos}");
            }
            
            // Make it smaller and glowing
            indicator.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            
            // Keep collider but make it larger for mobile-friendly clicking
            SphereCollider indicatorCollider = indicator.GetComponent<SphereCollider>();
            if (indicatorCollider != null)
            {
                indicatorCollider.radius = 1.2f; // Larger than visual for easier touch targeting
                Debug.Log($"ShowValidMoveIndicators: Set collider radius to {indicatorCollider.radius} for move {move}");
            }
            
            // Add a component to store board position data
            MoveTargetData moveData = indicator.AddComponent<MoveTargetData>();
            moveData.targetPosition = move;
            
            // Make it glow with a bright material
            Renderer renderer = indicator.GetComponent<Renderer>();
            Material glowMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            
            // Use green color during placement phase for visual consistency, yellow during play phase
            if (GameStateManager.Instance != null && GameStateManager.Instance.CanPlacePieces())
            {
                glowMaterial.color = Color.green; // GREEN during placement phase
                Debug.Log($"ShowValidMoveIndicators: Using GREEN color for placement phase indicator at {move}");
            }
            else
            {
                glowMaterial.color = Color.yellow; // YELLOW during play phase
            }
            
            glowMaterial.SetFloat("_Smoothness", 0.9f);
            renderer.material = glowMaterial;
            
            moveIndicators.Add(indicator);
        }
    }
    
    private void HideValidMoveIndicators()
    {
        foreach (var indicator in moveIndicators)
        {
            if (indicator != null)
            {
                DestroyImmediate(indicator);
            }
        }
        moveIndicators.Clear();
    }
    
    /// <summary>
    /// Public method to clear move indicators (called by PlacementManager to avoid conflicts)
    /// </summary>
    public void ClearMoveIndicators()
    {
        Debug.Log("InputManager: Clearing move indicators on external request");
        HideValidMoveIndicators();
    }
    
    private void TryMovePiece(BoardPosition targetPosition)
    {
        Debug.Log($"🚀 TryMovePiece: Called with targetPosition {targetPosition}");
        
        // During placement phase, allow piece movement for repositioning
        if (GameStateManager.Instance != null && GameStateManager.Instance.CanPlacePieces())
        {
            Debug.Log($"TryMovePiece: Placement phase - allowing piece repositioning to {targetPosition}");
        }
        
        if (selectedPiece == null)
        {
            Debug.LogError("TryMovePiece: selectedPiece is NULL!");
            return;
        }
        
        if (isProcessingInput)
        {
            Debug.LogWarning("TryMovePiece: Already processing input, skipping move");
            return;
        }
        
        Debug.Log($"TryMovePiece: Selected piece = {selectedPiece.pieceColor} {selectedPiece.pieceType} at {selectedPiece.CurrentPosition}");
        Debug.Log($"TryMovePiece: Attempting move from {selectedPiece.CurrentPosition} to {targetPosition}");
        
        // During placement phase, enforce placement zone restrictions
        bool canMove = true;
        if (GameStateManager.Instance != null && GameStateManager.Instance.CanPlacePieces())
        {
            if (!PlacementManager.IsPositionInColorZone(targetPosition, selectedPiece.pieceColor))
            {
                Debug.LogWarning($"TryMovePiece: Invalid placement zone - {selectedPiece.pieceColor} pieces must stay in X={PlacementManager.GetValidXForColor(selectedPiece.pieceColor)} layer");
                canMove = false;
            }
            else
            {
                Debug.Log($"TryMovePiece: Placement zone valid for {selectedPiece.pieceColor} piece");
            }
        }
        
        // Check piece-specific movement rules
        if (canMove)
        {
            canMove = selectedPiece.CanMoveTo(targetPosition);
        }
        Debug.Log($"TryMovePiece: CanMoveTo({targetPosition}) returned {canMove}");
        
        if (canMove)
        {
            Debug.Log($"✅ TryMovePiece: Move is valid, starting ProcessMoveCoroutine");
            StartCoroutine(ProcessMoveCoroutine(targetPosition));
        }
        else
        {
            Debug.LogWarning($"❌ TryMovePiece: Invalid move to {targetPosition}");
            
            // Debug: Show what moves ARE valid for comparison
            var validMoves = selectedPiece.GetValidMoves();
            Debug.Log($"TryMovePiece: Valid moves for this piece: [{string.Join(", ", validMoves)}]");
            
            // Check if target position is in the valid moves list
            bool inList = validMoves.Contains(targetPosition);
            Debug.Log($"TryMovePiece: Target {targetPosition} in valid moves list: {inList}");
        }
    }
    
    private System.Collections.IEnumerator ProcessMoveCoroutine(BoardPosition targetPosition)
    {
        Debug.Log($"🎬 ProcessMoveCoroutine: Started for move to {targetPosition}");
        isProcessingInput = true;
        
        Debug.Log($"ProcessMoveCoroutine: Set isProcessingInput = true");
        
        // Store piece info before move (in case it gets destroyed during promotion)
        PieceColor movingColor = selectedPiece.pieceColor;
        ChessPieceType movingType = selectedPiece.pieceType;
        BoardPosition oldPosition = selectedPiece.CurrentPosition;
        
        // Check if this is a pawn promotion move
        Pawn pawn = selectedPiece as Pawn;
        bool isPawnPromotion = (pawn != null && pawn.IsPromotionMove(targetPosition));
        
        Debug.Log($"ProcessMoveCoroutine: Moving {movingColor} {movingType}, promotion={isPawnPromotion}");
        
        // Move the piece (ChessBoard.MovePiece will handle capture and promotion logic)
        if (ChessBoard.Instance != null)
        {
            ChessBoard.Instance.MovePiece(oldPosition, targetPosition);
        }
        
        Debug.Log($"Moved {movingColor} {movingType} from {oldPosition} to {targetPosition}");
        
        if (isPawnPromotion)
        {
            // For pawn promotion, don't wait for animation since piece may be destroyed
            Debug.Log($"ProcessMoveCoroutine: Pawn promotion detected, waiting for completion");
            
            // Wait for promotion to complete if PawnPromotionManager exists
            if (PawnPromotionManager.Instance != null)
            {
                Debug.Log($"ProcessMoveCoroutine: PawnPromotionManager found, waiting for promotion to complete");
                
                float promotionTimeout = 0f;
                bool promotionComplete = false;
                
                while (!promotionComplete)
                {
                    bool stillInProgress = PawnPromotionManager.Instance.IsPromotionInProgress();
                    
                    // Only log every 2 seconds to reduce spam
                    if (Time.frameCount % 120 == 0)
                    {
                        Debug.Log($"ProcessMoveCoroutine: Waiting for player to select promotion piece - elapsed: {promotionTimeout:F2}s");
                    }
                    
                    if (!stillInProgress)
                    {
                        promotionComplete = true;
                        Debug.Log("ProcessMoveCoroutine: Promotion completed successfully");
                        break;
                    }
                    
                    promotionTimeout += Time.deltaTime;
                    yield return null;
                }
                
                Debug.Log($"ProcessMoveCoroutine: Promotion completed in {promotionTimeout:F2} seconds");
            }
            else
            {
                Debug.LogWarning("ProcessMoveCoroutine: PawnPromotionManager not found, skipping promotion wait");
            }
        }
        else
        {
            // Normal move: Wait for move animation to complete
            float animationTimeout = 0f;
            while (selectedPiece != null && selectedPiece.IsMoving && animationTimeout < 10f)
            {
                animationTimeout += Time.deltaTime;
                yield return null;
            }
            
            if (animationTimeout >= 10f)
            {
                Debug.LogWarning("ProcessMoveCoroutine: Animation timeout reached!");
            }
        }
        
        // CRITICAL: Always reset input processing state
        DeselectPiece();
        
        Debug.Log($"ProcessMoveCoroutine: Setting isProcessingInput = false");
        isProcessingInput = false;
        Debug.Log($"ProcessMoveCoroutine: Input processing unlocked");
    }
    
    public void ClearSelection()
    {
        DeselectPiece();
    }
    
    /// <summary>
    /// Show visual feedback when a player tries to move out of turn
    /// </summary>
    private void ShowInvalidTurnFeedback(PieceColor attemptedPlayer, PieceColor currentPlayer)
    {
        Debug.LogWarning($"⚠️ INVALID TURN: {attemptedPlayer} attempted to move, but it's {currentPlayer}'s turn!");
        
        // TODO: Add visual/audio feedback here
        // For now, just log the violation - UI will be enhanced later
        
        // Could add:
        // - Screen flash or color overlay
        // - Sound effect
        // - Temporary message display
        // - Highlight current player's pieces
    }
}