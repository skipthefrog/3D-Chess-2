using UnityEngine;

/// <summary>
/// Simple UI for the placement phase
/// Provides buttons to control the placement system
/// </summary>
public class PlacementUI : MonoBehaviour
{
    [Header("UI Settings")]
    public bool showUI = true;
    public float buttonWidth = 150f;
    public float buttonHeight = 50f;
    
    private Rect readyButtonRect;
    private Rect resetButtonRect;
    private Rect deselectButtonRect;
    private Rect interiorViewButtonRect;
    private Rect overviewButtonRect;
    private Rect stateInfoRect;
    private Rect cameraInfoRect;
    private string currentStateText = "";
    
    private void Start()
    {
        // Calculate UI positions
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        // Position buttons in top-left corner
        readyButtonRect = new Rect(20, 20, buttonWidth, buttonHeight);
        resetButtonRect = new Rect(20, 80, buttonWidth, buttonHeight);
        deselectButtonRect = new Rect(20, 140, buttonWidth, buttonHeight);
        interiorViewButtonRect = new Rect(20, 260, buttonWidth, buttonHeight);
        overviewButtonRect = new Rect(20, 320, buttonWidth, buttonHeight);
        stateInfoRect = new Rect(20, 380, buttonWidth * 2, buttonHeight * 2);
        cameraInfoRect = new Rect(20, 480, buttonWidth * 3, buttonHeight);
        
        // Subscribe to game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
            UpdateStateText(GameStateManager.Instance.currentState);
        }
        
        // Subscribe to placement manager events
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.OnPlayerTurnChanged += OnPlayerTurnChanged;
            PlacementManager.Instance.OnPlacementPhaseComplete += OnPlacementPhaseComplete;
        }
        
        // Subscribe to turn manager events for gameplay turn changes
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged += OnGameplayTurnChanged;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
        
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.OnPlayerTurnChanged -= OnPlayerTurnChanged;
            PlacementManager.Instance.OnPlacementPhaseComplete -= OnPlacementPhaseComplete;
        }
        
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged -= OnGameplayTurnChanged;
        }
    }
    
    private void OnGUI()
    {
        if (!showUI) return;
        
        // Set GUI style
        GUI.skin.button.fontSize = 16;
        GUI.skin.label.fontSize = 14;
        
        // Game state info
        GUI.Label(stateInfoRect, currentStateText);
        
        // Get camera controller once for all camera-related operations
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        
        // Camera debug info
        if (cameraController != null)
        {
            GUI.Label(cameraInfoRect, $"CAMERA: {cameraController.GetCameraInfo()}");
        }
        
        // Only show buttons during placement phase
        if (GameStateManager.Instance != null && GameStateManager.Instance.CanPlacePieces())
        {
            // Ready to Play button - check if pieces are placed
            bool canStartGame = CanStartGame();
            GUI.enabled = canStartGame; // Disable button if can't start
            
            string buttonText = canStartGame ? "Ready to Play" : "Place Pieces First";
            if (GUI.Button(readyButtonRect, buttonText))
            {
                OnReadyButtonClicked();
            }
            
            GUI.enabled = true; // Re-enable GUI
            
            // Reset button
            if (GUI.Button(resetButtonRect, "Reset Trays"))
            {
                OnResetButtonClicked();
            }
            
            // Deselect button - only show if a piece is selected
            if (PlacementManager.Instance != null && HasSelectedTrayPiece())
            {
                if (GUI.Button(deselectButtonRect, "Deselect Piece"))
                {
                    OnDeselectButtonClicked();
                }
            }
        }
        
        
        // Show camera control buttons
        if (cameraController != null)
        {
            if (GUI.Button(interiorViewButtonRect, "Close Zoom"))
            {
                cameraController.SetZoomPreset(5f);
            }
            
            if (GUI.Button(overviewButtonRect, "Far Zoom"))
            {
                cameraController.SetZoomPreset(20f);
            }
        }
        
        // Show instructions
        string instructions = GetInstructions();
        if (!string.IsNullOrEmpty(instructions))
        {
            Rect instructionsRect = new Rect(20, Screen.height - 120, Screen.width - 40, 100);
            GUI.Label(instructionsRect, instructions);
        }
    }
    
    /// <summary>
    /// Check if the game can be started (enough pieces placed)
    /// </summary>
    private bool CanStartGame()
    {
        // Check that both players have at least one piece on the board
        if (ChessBoard.Instance == null) return false;
        
        bool hasWhitePieces = false;
        bool hasBlackPieces = false;
        
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
                        if (piece.pieceColor == PieceColor.White)
                            hasWhitePieces = true;
                        else if (piece.pieceColor == PieceColor.Black)
                            hasBlackPieces = true;
                    }
                }
            }
        }
        
        // Require at least one piece from each player
        return hasWhitePieces && hasBlackPieces;
    }
    
    /// <summary>
    /// Get instructions text based on current game state
    /// </summary>
    private string GetInstructions()
    {
        if (GameStateManager.Instance == null) return "";
        
        switch (GameStateManager.Instance.currentState)
        {
            case GameState.PiecePlacement:
                return "PLACEMENT PHASE: Click pieces in trays to place them, or click placed pieces to reposition them. Pieces can be freely moved until you press 'Ready to Play'.\nCAMERA: Zoom in/out with mouse wheel or pinch. Use Interior View for close-up navigation inside the 3D grid.";
            
            case GameState.Playing:
                return "GAME PHASE: Click pieces to select them, then click yellow indicators to move. Press 'C' to test captures.\nCAMERA: Zoom deep into the grid interior for precision moves. Use Overview for full board view.";
            
            case GameState.GameOver:
                return "GAME OVER";
            
            default:
                return "";
        }
    }
    
    /// <summary>
    /// Handle Ready button click
    /// </summary>
    private void OnReadyButtonClicked()
    {
        Debug.Log("PlacementUI: Ready to Play button clicked");
        
        if (!CanStartGame())
        {
            Debug.LogWarning("PlacementUI: Cannot start game - not enough pieces placed");
            return;
        }
        
        if (PlacementManager.Instance != null)
        {
            Debug.Log("PlacementUI: Starting gameplay phase...");
            PlacementManager.Instance.CompletePlacementPhase();
        }
    }
    
    /// <summary>
    /// Handle Reset button click
    /// </summary>
    private void OnResetButtonClicked()
    {
        Debug.Log("PlacementUI: Reset button clicked");
        
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.ResetToTrays();
        }
    }
    
    /// <summary>
    /// Handle Deselect button click
    /// </summary>
    private void OnDeselectButtonClicked()
    {
        Debug.Log("PlacementUI: Deselect button clicked");
        
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.DeselectTrayPiece();
        }
    }
    
    /// <summary>
    /// Check if a tray piece is currently selected
    /// </summary>
    private bool HasSelectedTrayPiece()
    {
        return PlacementManager.Instance != null && PlacementManager.Instance.HasSelectedTrayPiece;
    }
    
    
    /// <summary>
    /// Handle game state changes
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        UpdateStateText(newState);
        
    }
    
    /// <summary>
    /// Handle player turn changes during placement
    /// </summary>
    private void OnPlayerTurnChanged(PieceColor currentPlayer)
    {
        Debug.Log($"PlacementUI: Turn changed to {currentPlayer}");
        UpdateStateText(GameStateManager.Instance.currentState);
    }
    
    /// <summary>
    /// Handle placement phase completion
    /// </summary>
    private void OnPlacementPhaseComplete()
    {
        Debug.Log("PlacementUI: Placement phase completed");
    }
    
    /// <summary>
    /// Handle turn changes during gameplay
    /// </summary>
    private void OnGameplayTurnChanged(PieceColor newCurrentPlayer)
    {
        Debug.Log($"PlacementUI: Gameplay turn changed to {newCurrentPlayer}");
        
        // Update the UI display to reflect the new current player
        if (GameStateManager.Instance != null)
        {
            UpdateStateText(GameStateManager.Instance.currentState);
        }
    }
    
    /// <summary>
    /// Update the state text display
    /// </summary>
    private void UpdateStateText(GameState state)
    {
        switch (state)
        {
            case GameState.PiecePlacement:
                string trayInfo = "";
                if (PieceTray.WhiteTray != null && PieceTray.BlackTray != null)
                {
                    trayInfo = $"\nWhite Tray: {PieceTray.WhiteTray.GetPieceCount()} pieces\nBlack Tray: {PieceTray.BlackTray.GetPieceCount()} pieces";
                }
                currentStateText = $"PLACEMENT PHASE{trayInfo}";
                break;
                
            case GameState.Playing:
                string turnInfo = "";
                if (GameStateManager.Instance != null && TurnManager.Instance != null)
                {
                    PieceColor currentPlayer = GameStateManager.Instance.GetCurrentPlayer();
                    turnInfo = $"\n🔄 CURRENT TURN: {currentPlayer}";
                }
                currentStateText = $"PLAYING PHASE{turnInfo}\nMove pieces to capture opponents!";
                break;
                
            case GameState.GameOver:
                currentStateText = "GAME OVER";
                break;
                
            default:
                currentStateText = $"STATE: {state}";
                break;
        }
    }
}