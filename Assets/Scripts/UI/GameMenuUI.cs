using UnityEngine;

/// <summary>
/// Game menu popup that appears when the Menu button is clicked during gameplay.
/// Provides access to forfeit, draw, and return to menu functionality.
/// Uses immediate mode GUI for consistency with ConfirmationDialog.
/// </summary>
public class GameMenuUI : MonoBehaviour
{
    [Header("Menu Settings")]
    public bool enableGameMenu = true;
    public KeyCode menuToggleKey = KeyCode.Escape;
    
    public static GameMenuUI Instance { get; private set; }
    
    // Menu state
    private bool isMenuVisible = false;
    
    // GUI styling
    private GUIStyle menuBoxStyle;
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle overlayStyle;
    private GUIStyle infoStyle;
    private GUIStyle humanDrawOfferStyle;
    private GUIStyle aiDrawOfferStyle;
    
    // Menu dimensions
    private Rect menuRect;
    private Rect overlayRect;
    private bool stylesInitialized = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameMenuUI: Instance created");
        }
        else
        {
            Debug.LogWarning("GameMenuUI: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        InitializeStyles();
    }
    
    private void Update()
    {
        // Handle menu toggle key
        if (enableGameMenu && Input.GetKeyDown(menuToggleKey))
        {
            ToggleMenu();
        }
    }
    
    /// <summary>
    /// Initialize GUI styles for the menu
    /// </summary>
    private void InitializeStyles()
    {
        // Menu box background
        menuBoxStyle = new GUIStyle();
        menuBoxStyle.normal.background = CreateColorTexture(new Color(0.15f, 0.15f, 0.15f, 0.95f));
        menuBoxStyle.border = new RectOffset(10, 10, 10, 10);
        menuBoxStyle.padding = new RectOffset(20, 20, 20, 20);
        
        // Overlay background
        overlayStyle = new GUIStyle();
        overlayStyle.normal.background = CreateColorTexture(new Color(0f, 0f, 0f, 0.6f));
        
        // Title style
        titleStyle = new GUIStyle();
        titleStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.03f);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;
        titleStyle.wordWrap = true;
        
        // Button style
        buttonStyle = new GUIStyle();
        buttonStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.022f);
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.normal.background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 1f));
        buttonStyle.hover.background = CreateColorTexture(new Color(0.5f, 0.5f, 0.5f, 1f));
        buttonStyle.active.background = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f, 1f));
        buttonStyle.border = new RectOffset(5, 5, 5, 5);
        buttonStyle.padding = new RectOffset(15, 15, 12, 12);
        
        // Info style for status text
        infoStyle = new GUIStyle();
        infoStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.018f);
        infoStyle.alignment = TextAnchor.MiddleCenter;
        infoStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        infoStyle.wordWrap = true;
        
        // Human draw offer style (blue tint for human player actions)
        humanDrawOfferStyle = new GUIStyle();
        humanDrawOfferStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.02f);
        humanDrawOfferStyle.fontStyle = FontStyle.Bold;
        humanDrawOfferStyle.alignment = TextAnchor.MiddleCenter;
        humanDrawOfferStyle.normal.textColor = new Color(0.4f, 0.7f, 1f, 1f); // Light blue
        humanDrawOfferStyle.wordWrap = true;
        
        // AI draw offer style (orange tint for AI player actions)
        aiDrawOfferStyle = new GUIStyle();
        aiDrawOfferStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.02f);
        aiDrawOfferStyle.fontStyle = FontStyle.Bold;
        aiDrawOfferStyle.alignment = TextAnchor.MiddleCenter;
        aiDrawOfferStyle.normal.textColor = new Color(1f, 0.6f, 0.3f, 1f); // Orange
        aiDrawOfferStyle.wordWrap = true;
        
        // Calculate menu rect
        float menuWidth = Screen.width * 0.35f;
        float menuHeight = Screen.height * 0.5f;
        float menuX = (Screen.width - menuWidth) / 2f;
        float menuY = (Screen.height - menuHeight) / 2f;
        
        menuRect = new Rect(menuX, menuY, menuWidth, menuHeight);
        overlayRect = new Rect(0, 0, Screen.width, Screen.height);
        
        stylesInitialized = true;
    }
    
    /// <summary>
    /// Create a texture with a solid color
    /// </summary>
    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
    
    /// <summary>
    /// Show the game menu
    /// </summary>
    public void ShowMenu()
    {
        if (!enableGameMenu) return;
        
        // Only show menu during gameplay
        if (GameStateManager.Instance == null || !GameStateManager.Instance.CanMovePieces())
        {
            Debug.LogWarning("GameMenuUI: Cannot show menu - not in playing state");
            return;
        }
        
        isMenuVisible = true;
        Debug.Log("GameMenuUI: Menu shown");
    }
    
    /// <summary>
    /// Hide the game menu
    /// </summary>
    public void HideMenu()
    {
        isMenuVisible = false;
        Debug.Log("GameMenuUI: Menu hidden");
    }
    
    /// <summary>
    /// Toggle menu visibility
    /// </summary>
    public void ToggleMenu()
    {
        if (isMenuVisible)
        {
            HideMenu();
        }
        else
        {
            ShowMenu();
        }
    }
    
    /// <summary>
    /// Check if menu is currently visible
    /// </summary>
    public bool IsMenuVisible()
    {
        return isMenuVisible;
    }
    
    /// <summary>
    /// Handle forfeit with confirmation
    /// </summary>
    private void HandleForfeit()
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.CanCurrentPlayerForfeit())
        {
            Debug.LogWarning("GameMenuUI: Cannot forfeit - conditions not met");
            return;
        }
        
        PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        HideMenu();
        
        // Show confirmation dialog
        if (ConfirmationDialog.Instance != null)
        {
            ConfirmationDialog.Instance.ShowForfeitDialog(currentPlayer, () => {
                Debug.Log($"GameMenuUI: Forfeit confirmed for {currentPlayer}");
                TurnManager.Instance.RequestForfeit();
            });
        }
        else
        {
            // Fallback: direct forfeit
            Debug.LogWarning("GameMenuUI: No confirmation dialog available, forfeiting directly");
            TurnManager.Instance.RequestForfeit();
        }
    }
    
    /// <summary>
    /// Handle draw offer with confirmation
    /// </summary>
    private void HandleDrawOffer()
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.CanCurrentPlayerOfferDraw())
        {
            Debug.LogWarning("GameMenuUI: Cannot offer draw - conditions not met");
            return;
        }
        
        PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        HideMenu();
        
        // Show confirmation dialog
        if (ConfirmationDialog.Instance != null)
        {
            ConfirmationDialog.Instance.ShowDrawOfferDialog(currentPlayer, () => {
                Debug.Log($"GameMenuUI: Draw offer confirmed for {currentPlayer}");
                TurnManager.Instance.RequestDrawOffer();
            });
        }
        else
        {
            // Fallback: direct draw offer
            Debug.LogWarning("GameMenuUI: No confirmation dialog available, offering draw directly");
            TurnManager.Instance.RequestDrawOffer();
        }
    }
    
    /// <summary>
    /// Handle draw accept
    /// </summary>
    private void HandleDrawAccept()
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.CanCurrentPlayerRespondToDraw())
        {
            Debug.LogWarning("GameMenuUI: Cannot accept draw - conditions not met");
            return;
        }
        
        HideMenu();
        TurnManager.Instance.RespondToDrawOffer(true);
        Debug.Log("GameMenuUI: Draw accepted");
    }
    
    /// <summary>
    /// Handle draw decline
    /// </summary>
    private void HandleDrawDecline()
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.CanCurrentPlayerRespondToDraw())
        {
            Debug.LogWarning("GameMenuUI: Cannot decline draw - conditions not met");
            return;
        }
        
        HideMenu();
        TurnManager.Instance.RespondToDrawOffer(false);
        Debug.Log("GameMenuUI: Draw declined");
    }
    
    /// <summary>
    /// Handle new game request
    /// </summary>
    private void HandleNewGame()
    {
        HideMenu();
        
        // Start a new game by returning to main menu first
        // This ensures clean initialization of a new game
        Debug.Log("GameMenuUI: Starting new game by returning to main menu");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReturnToMainMenu();
        }
        else if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("GameMenuUI: Cannot start new game - no scene controller available");
        }
    }
    
    /// <summary>
    /// Handle return to main menu
    /// </summary>
    private void HandleReturnToMenu()
    {
        HideMenu();
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReturnToMainMenu();
        }
        else if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("GameMenuUI: Cannot return to menu - no scene controller available");
        }
    }
    
    private void OnGUI()
    {
        if (!isMenuVisible || !stylesInitialized || !enableGameMenu) return;
        
        // Skip if confirmation dialog is open
        if (ConfirmationDialog.Instance != null && ConfirmationDialog.Instance.IsDialogActive()) return;
        
        // Draw overlay
        GUI.Box(overlayRect, "", overlayStyle);
        
        // Handle clicking outside menu to close
        if (Event.current.type == EventType.MouseDown && !menuRect.Contains(Event.current.mousePosition))
        {
            HideMenu();
            return;
        }
        
        // Begin menu area
        GUILayout.BeginArea(menuRect, menuBoxStyle);
        GUILayout.BeginVertical();
        
        // Check if game is over and show appropriate content
        bool gameOver = IsGameOver();
        
        if (gameOver)
        {
            // Game Over Menu Layout
            GUILayout.Label("GAME OVER", titleStyle);
            GUILayout.Space(20);
            
            // Game over status info
            var (isOver, reason, winner) = GetGameOverInfo();
            if (!string.IsNullOrEmpty(reason))
            {
                GUILayout.Label(reason, infoStyle);
                GUILayout.Space(15);
            }
            
            // New Game button
            if (GUILayout.Button("New Game", buttonStyle, GUILayout.Height(45)))
            {
                HandleNewGame();
            }
            
            GUILayout.Space(10);
        }
        else
        {
            // Normal Game Menu Layout
            GUILayout.Label("GAME MENU", titleStyle);
            GUILayout.Space(20);
            
            // Game status info
            string statusText = GetGameStatusText();
            if (!string.IsNullOrEmpty(statusText))
            {
                GUILayout.Label(statusText, infoStyle);
                GUILayout.Space(15);
            }
            
            // Resume Game button
            if (GUILayout.Button("Resume Game", buttonStyle, GUILayout.Height(45)))
            {
                HideMenu();
            }
            
            GUILayout.Space(10);
        }
        
        // Draw-related buttons (only show during active game)
        if (!gameOver && TurnManager.Instance != null && GameEndDetectionManager.Instance != null)
        {
            bool canOfferDraw = TurnManager.Instance.CanCurrentPlayerOfferDraw();
            bool canRespondToDraw = TurnManager.Instance.CanCurrentPlayerRespondToDraw();
            bool isDrawPending = GameEndDetectionManager.Instance.IsDrawOfferPending();
            bool isHumanVsAI = IsHumanVsAIMode();
            
            if (canRespondToDraw && isDrawPending)
            {
                // Show accept/decline buttons when responding to draw
                PieceColor offeringPlayer = GameEndDetectionManager.Instance.GetDrawOfferingPlayer();
                
                // Enhanced indication for Human vs AI mode
                if (isHumanVsAI)
                {
                    PieceColor humanPlayer = GetHumanPlayerColor();
                    bool aiOfferedDraw = (offeringPlayer != humanPlayer);
                    
                    // Add visual separator for draw offers
                    GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
                    GUILayout.Space(5);
                    
                    if (aiOfferedDraw)
                    {
                        GUILayout.Label("🤖 AI has offered a draw", aiDrawOfferStyle);
                        GUILayout.Label("What would you like to do?", infoStyle);
                    }
                    else
                    {
                        GUILayout.Label("⏳ Draw offer sent", humanDrawOfferStyle);
                        GUILayout.Label("Waiting for AI to respond...", infoStyle);
                    }
                    
                    GUILayout.Space(3);
                    GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
                }
                else
                {
                    // Original behavior for other game modes
                    GUILayout.Label($"Draw offered by {offeringPlayer}", infoStyle);
                }
                
                GUILayout.Space(5);
                
                // Only show accept/decline buttons if human can respond
                if (canRespondToDraw)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Accept Draw (Y)", buttonStyle, GUILayout.Height(40)))
                    {
                        HandleDrawAccept();
                    }
                    if (GUILayout.Button("Decline Draw (N)", buttonStyle, GUILayout.Height(40)))
                    {
                        HandleDrawDecline();
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else if (isDrawPending && isHumanVsAI)
            {
                // Show status when human offered draw and waiting for AI response
                PieceColor offeringPlayer = GameEndDetectionManager.Instance.GetDrawOfferingPlayer();
                PieceColor humanPlayer = GetHumanPlayerColor();
                
                if (offeringPlayer == humanPlayer)
                {
                    // Add visual separator
                    GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
                    GUILayout.Space(5);
                    
                    GUILayout.Label("⏳ Draw offer pending", humanDrawOfferStyle);
                    GUILayout.Label("Waiting for AI response...", infoStyle);
                    
                    // Show how long the offer has been pending
                    if (GameEndDetectionManager.Instance != null)
                    {
                        float offerAge = GameEndDetectionManager.Instance.GetDrawOfferAge();
                        GUILayout.Label($"Offered {offerAge:F0} seconds ago", infoStyle);
                    }
                    
                    GUILayout.Space(3);
                    GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
                    GUILayout.Space(5);
                }
            }
            else if (canOfferDraw)
            {
                // Show offer draw button
                if (GUILayout.Button("Offer Draw (D)", buttonStyle, GUILayout.Height(40)))
                {
                    HandleDrawOffer();
                }
            }
            
            GUILayout.Space(10);
        }
        
        // Forfeit button (only show during active game)
        if (!gameOver)
        {
            bool canForfeit = TurnManager.Instance != null && TurnManager.Instance.CanCurrentPlayerForfeit();
            GUI.enabled = canForfeit;
            
            Color originalColor = buttonStyle.normal.textColor;
            if (!canForfeit)
            {
                buttonStyle.normal.textColor = Color.gray;
            }
            
            if (GUILayout.Button("Forfeit (F)", buttonStyle, GUILayout.Height(40)))
            {
                HandleForfeit();
            }
            
            buttonStyle.normal.textColor = originalColor;
            GUI.enabled = true;
            
            GUILayout.Space(15);
        }
        
        // Return to Main Menu button
        if (GUILayout.Button("Return to Main Menu", buttonStyle, GUILayout.Height(40)))
        {
            HandleReturnToMenu();
        }
        
        GUILayout.FlexibleSpace();
        
        // Instructions
        GUILayout.Label("Press ESC to toggle menu", infoStyle);
        GUILayout.Space(10);
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// Check if current game mode is Human vs AI
    /// </summary>
    private bool IsHumanVsAIMode()
    {
        if (TurnManager.Instance == null) return false;
        return TurnManager.Instance.IsHumanVsAI();
    }
    
    /// <summary>
    /// Check if the game is over
    /// </summary>
    private bool IsGameOver()
    {
        if (GameStateManager.Instance == null) return false;
        return !GameStateManager.Instance.IsGameActive();
    }
    
    /// <summary>
    /// Get the game over reason and winner information
    /// </summary>
    private (bool isGameOver, string reason, PieceColor winner) GetGameOverInfo()
    {
        if (!IsGameOver()) return (false, "", PieceColor.White);
        
        // Try to get information from GameEndDetectionManager or other sources
        // For now, return basic game over info
        return (true, "Game Over", PieceColor.White);
    }
    
    /// <summary>
    /// Get the human player color in Human vs AI mode
    /// </summary>
    private PieceColor GetHumanPlayerColor()
    {
        if (TurnManager.Instance == null) return PieceColor.White;
        
        // In Human vs AI mode, find the human player
        bool whiteIsHuman = !TurnManager.Instance.IsPlayerAI(PieceColor.White);
        bool blackIsHuman = !TurnManager.Instance.IsPlayerAI(PieceColor.Black);
        
        if (whiteIsHuman && !blackIsHuman)
            return PieceColor.White;
        else if (blackIsHuman && !whiteIsHuman)
            return PieceColor.Black;
        
        // Default to White if unclear
        return PieceColor.White;
    }
    
    /// <summary>
    /// Get current game status text for display in menu
    /// </summary>
    private string GetGameStatusText()
    {
        if (TurnManager.Instance == null) return "";
        
        PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        bool isAI = TurnManager.Instance.IsPlayerAI(currentPlayer);
        
        string status = $"Current Turn: {currentPlayer}";
        if (isAI)
        {
            status += " (AI)";
        }
        
        // Add check status
        if (CheckDetectionManager.Instance != null)
        {
            bool inCheck = CheckDetectionManager.Instance.IsKingInCheck(currentPlayer);
            if (inCheck)
            {
                status += " - IN CHECK!";
            }
        }
        
        return status;
    }
}