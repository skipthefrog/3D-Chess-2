using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu step enumeration for tracking current menu state
/// </summary>
public enum MenuStep
{
    MainMenu,           // Initial menu with New Game button
    PlayerCount,        // Select number of players
    AIPlayerCount,      // Select number of AI players
    SideSelection,      // Select player positions (only when 0 < AI < total players)
    BoardSize,          // Select board size
    FinalConfirmation   // Review and confirm settings
}

/// <summary>
/// Controls the main menu interface, allowing players to select game modes,
/// player types, and AI difficulty before starting a game.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Menu Flow")]
    public MenuStep currentStep = MenuStep.MainMenu;
    
    [Header("UI References - Game Mode")]
    public Button humanVsHumanButton;
    public Button humanVsAIButton;
    public Button aiVsAIButton;
    public Button newGameButton;
    
    [Header("UI References - Player Color (Human vs AI)")]
    public GameObject playerColorPanel;
    public Button playAsWhiteButton;
    public Button playAsBlackButton;
    public TextMeshProUGUI playerColorText;
    
    [Header("UI References - AI Difficulty")]
    public GameObject aiDifficultyPanel;
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;
    public TextMeshProUGUI difficultyText;
    
    [Header("UI References - Controls")]
    public Button startGameButton;
    public Button quitButton;
    public Button exitButton;
    
    [Header("UI References - Display")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI gameModeSummaryText;
    public TextMeshProUGUI instructionsText;
    
    [Header("UI References - Loading")]
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingText;
    
    [Header("Enhanced Menu Flow - Panels")]
    public GameObject mainMenuPanel;
    public GameObject playerCountPanel;
    public GameObject aiCountPanel;
    public GameObject sideSelectionPanel;
    public GameObject boardSizePanel;
    public GameObject confirmationPanel;
    
    [Header("Enhanced Menu Flow - Controls")]
    public Button backButton;
    public Button nextButton;
    public Slider playerCountSlider;
    public TextMeshProUGUI playerCountText;
    public Slider aiCountSlider;
    public TextMeshProUGUI aiCountText;
    public TextMeshProUGUI boardSizeText;
    public Dropdown boardSizeDropdown;
    public TextMeshProUGUI confirmationText;
    public TextMeshProUGUI stepIndicatorText;
    
    // Current configuration
    private GameConfiguration currentConfig;
    private PieceColor humanPlayerColor = PieceColor.White;
    
    // UI State
    private bool isLoading = false;
    private bool hasSelectedAICount = false; // Track if user has explicitly selected AI count
    private bool hasSelectedBoardSize = false; // Track if user has explicitly selected board size
    
    private void Start()
    {
        InitializeUI();
        
        // Create enhanced menu UI if not assigned
        if (mainMenuPanel == null)
        {
            CreateEnhancedMenuUI();
        }
        
        SetupButtonListeners();
        
        // Start with default configuration
        currentConfig = new GameConfiguration();
        SetGameMode(PlayerType.Human, PlayerType.Human);
        
        // Initialize step to main menu (this will hide all panels except main menu)
        currentStep = MenuStep.MainMenu;
        
        // Show main menu step after UI is created
        ShowStep(MenuStep.MainMenu);
        
        // Debug final UI state after everything is set up
        DebugUIState();
    }
    
    /// <summary>
    /// Initialize UI elements and default states
    /// </summary>
    private void InitializeUI()
    {
        // Set up title
        if (titleText != null)
            titleText.text = "3D Chess";
        
        // Set up instructions
        if (instructionsText != null)
            instructionsText.text = "Choose your game mode and preferences:";
        
        // Hide loading panel initially
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        // Subscribe to scene controller events
        if (SceneController.Instance != null)
        {
            SceneController.Instance.OnSceneLoadStarted += OnSceneLoadStarted;
            SceneController.Instance.OnSceneLoadCompleted += OnSceneLoadCompleted;
        }
    }
    
    /// <summary>
    /// Set up button click listeners
    /// </summary>
    private void SetupButtonListeners()
    {
        // Game mode buttons
        if (humanVsHumanButton != null)
            humanVsHumanButton.onClick.AddListener(() => SetGameMode(PlayerType.Human, PlayerType.Human));
        
        if (humanVsAIButton != null)
            humanVsAIButton.onClick.AddListener(() => SetHumanVsAIMode());
        
        if (aiVsAIButton != null)
            aiVsAIButton.onClick.AddListener(() => SetGameMode(PlayerType.Computer, PlayerType.Computer));
        
        // Player color buttons (for Human vs AI)
        if (playAsWhiteButton != null)
            playAsWhiteButton.onClick.AddListener(() => SetHumanPlayerColor(PieceColor.White));
        
        if (playAsBlackButton != null)
            playAsBlackButton.onClick.AddListener(() => SetHumanPlayerColor(PieceColor.Black));
        
        // AI difficulty buttons
        if (easyButton != null)
            easyButton.onClick.AddListener(() => SetAIDifficulty(AIDifficulty.Easy));
        
        if (mediumButton != null)
            mediumButton.onClick.AddListener(() => SetAIDifficulty(AIDifficulty.Medium));
        
        if (hardButton != null)
            hardButton.onClick.AddListener(() => SetAIDifficulty(AIDifficulty.Hard));
        
        // Control buttons
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        
        // Enhanced flow buttons
        if (newGameButton != null)
            newGameButton.onClick.AddListener(StartEnhancedFlow);
            
        if (backButton != null)
            backButton.onClick.AddListener(GoToPreviousStep);
            
        if (nextButton != null)
            nextButton.onClick.AddListener(GoToNextStep);
            
        // Enhanced flow sliders (event handlers are set during UI creation)
    }
    
    /// <summary>
    /// Set the game mode (player types for white and black)
    /// </summary>
    public void SetGameMode(PlayerType whiteType, PlayerType blackType)
    {
        currentConfig.whitePlayerType = whiteType;
        currentConfig.blackPlayerType = blackType;
        
        Debug.Log($"MainMenuController: Game mode set to {whiteType} vs {blackType}");
        UpdateUI();
    }
    
    /// <summary>
    /// Set Human vs AI mode based on current human player color preference
    /// </summary>
    private void SetHumanVsAIMode()
    {
        if (humanPlayerColor == PieceColor.White)
        {
            SetGameMode(PlayerType.Human, PlayerType.Computer);
        }
        else
        {
            SetGameMode(PlayerType.Computer, PlayerType.Human);
        }
    }
    
    /// <summary>
    /// Set which color the human player wants to play as (for Human vs AI games)
    /// </summary>
    public void SetHumanPlayerColor(PieceColor color)
    {
        humanPlayerColor = color;
        
        // Update game mode if we're currently in Human vs AI mode
        if (currentConfig.IsHumanVsAI())
        {
            SetHumanVsAIMode();
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// Set AI difficulty level
    /// </summary>
    public void SetAIDifficulty(AIDifficulty difficulty)
    {
        currentConfig.aiDifficulty = difficulty;
        Debug.Log($"MainMenuController: AI difficulty set to {difficulty}");
        UpdateUI();
    }
    
    /// <summary>
    /// Start the game with current configuration
    /// </summary>
    public void StartGame()
    {
        if (isLoading)
        {
            Debug.LogWarning("MainMenuController: Already loading, ignoring start game request");
            return;
        }
        
        Debug.Log($"MainMenuController: Starting game with configuration: {currentConfig}");
        
        // Load the game scene with current configuration
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadGameScene(currentConfig);
        }
        else
        {
            Debug.LogError("MainMenuController: SceneController.Instance not found!");
        }
    }
    
    /// <summary>
    /// Quit the game application
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("MainMenuController: Quit game requested");
        
        if (SceneController.Instance != null)
        {
            SceneController.Instance.QuitApplication();
        }
        else
        {
            // Fallback quit method
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
    
    /// <summary>
    /// Update UI elements based on current configuration
    /// </summary>
    private void UpdateUI()
    {
        UpdateGameModeSummary();
        UpdatePlayerColorPanel();
        UpdateAIDifficultyPanel();
        UpdateButtonHighlights();
    }
    
    /// <summary>
    /// Update the game mode summary text
    /// </summary>
    private void UpdateGameModeSummary()
    {
        if (gameModeSummaryText != null)
        {
            gameModeSummaryText.text = $"Selected: {currentConfig.GetGameModeDescription()}";
        }
    }
    
    /// <summary>
    /// Show/hide and update player color selection panel
    /// </summary>
    private void UpdatePlayerColorPanel()
    {
        bool showPlayerColorPanel = currentConfig.IsHumanVsAI();
        
        if (playerColorPanel != null)
            playerColorPanel.SetActive(showPlayerColorPanel);
        
        if (showPlayerColorPanel && playerColorText != null)
        {
            playerColorText.text = $"You play as: {humanPlayerColor}";
        }
    }
    
    /// <summary>
    /// Show/hide and update AI difficulty panel
    /// </summary>
    private void UpdateAIDifficultyPanel()
    {
        bool showAIDifficultyPanel = currentConfig.IsHumanVsAI() || currentConfig.IsAIVsAI();
        
        if (aiDifficultyPanel != null)
            aiDifficultyPanel.SetActive(showAIDifficultyPanel);
        
        if (showAIDifficultyPanel && difficultyText != null)
        {
            difficultyText.text = $"AI Difficulty: {currentConfig.aiDifficulty}";
        }
    }
    
    /// <summary>
    /// Update button visual states to show current selections
    /// </summary>
    private void UpdateButtonHighlights()
    {
        // Game mode button highlights
        SetButtonHighlight(humanVsHumanButton, currentConfig.IsHumanVsHuman());
        SetButtonHighlight(humanVsAIButton, currentConfig.IsHumanVsAI());
        SetButtonHighlight(aiVsAIButton, currentConfig.IsAIVsAI());
        
        // Player color button highlights (for Human vs AI)
        if (currentConfig.IsHumanVsAI())
        {
            SetButtonHighlight(playAsWhiteButton, humanPlayerColor == PieceColor.White);
            SetButtonHighlight(playAsBlackButton, humanPlayerColor == PieceColor.Black);
        }
        
        // AI difficulty button highlights
        if (currentConfig.IsHumanVsAI() || currentConfig.IsAIVsAI())
        {
            SetButtonHighlight(easyButton, currentConfig.aiDifficulty == AIDifficulty.Easy);
            SetButtonHighlight(mediumButton, currentConfig.aiDifficulty == AIDifficulty.Medium);
            SetButtonHighlight(hardButton, currentConfig.aiDifficulty == AIDifficulty.Hard);
        }
    }
    
    /// <summary>
    /// Set visual highlight state for a button
    /// </summary>
    private void SetButtonHighlight(Button button, bool highlighted)
    {
        if (button == null) 
        {
            Debug.LogWarning("🔄 SetButtonHighlight: Button is null");
            return;
        }
        
        // Change button color to indicate selection
        ColorBlock colors = button.colors;
        if (highlighted)
        {
            colors.normalColor = new Color(0.2f, 0.8f, 0.2f, 1.0f); // More vibrant green for selected
            colors.highlightedColor = new Color(0.3f, 0.9f, 0.3f, 1.0f); // Lighter green on hover
            Debug.Log($"🔄 SetButtonHighlight: Highlighted {button.name} with green color");
        }
        else
        {
            colors.normalColor = Color.white; // Default color
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1.0f); // Light gray on hover
            Debug.Log($"🔄 SetButtonHighlight: Reset {button.name} to white color");
        }
        button.colors = colors;
        Debug.Log($"🔄 SetButtonHighlight: Applied colors to {button.name} - normal: {colors.normalColor}, highlighted: {colors.highlightedColor}");
    }
    
    /// <summary>
    /// Called when scene loading starts
    /// </summary>
    private void OnSceneLoadStarted(string sceneName)
    {
        isLoading = true;
        
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
        
        if (loadingText != null)
            loadingText.text = $"Loading {sceneName}...";
        
        // Disable start button during loading
        if (startGameButton != null)
            startGameButton.interactable = false;
    }
    
    /// <summary>
    /// Called when scene loading completes
    /// </summary>
    private void OnSceneLoadCompleted(string sceneName)
    {
        isLoading = false;
        
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        // Re-enable start button
        if (startGameButton != null)
            startGameButton.interactable = true;
    }
    
    /// <summary>
    /// Quick setup methods for testing
    /// </summary>
    [ContextMenu("Quick Setup - Human vs AI (Easy)")]
    public void QuickSetupHumanVsAIEasy()
    {
        SetHumanPlayerColor(PieceColor.White);
        SetGameMode(PlayerType.Human, PlayerType.Computer);
        SetAIDifficulty(AIDifficulty.Easy);
    }
    
    [ContextMenu("Quick Setup - AI vs AI (Medium)")]
    public void QuickSetupAIVsAIMedium()
    {
        SetGameMode(PlayerType.Computer, PlayerType.Computer);
        SetAIDifficulty(AIDifficulty.Medium);
    }
    
    /// <summary>
    /// Create the enhanced menu UI if panels are not assigned
    /// </summary>
    private void CreateEnhancedMenuUI()
    {
        Debug.Log("🎨 MainMenuController.CreateEnhancedMenuUI: ENTRY");
        
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.Log("🎨 MainMenuController: No Canvas found, creating one");
            CreateCanvas();
            canvas = FindFirstObjectByType<Canvas>();
        }
        
        if (canvas == null)
        {
            Debug.LogError("❌ MainMenuController: Failed to create or find Canvas!");
            return;
        }
        
        Debug.Log("🎨 MainMenuController: Creating complete menu UI with working buttons");
        
        // CRITICAL: Ensure EventSystem exists regardless of Canvas source
        EnsureEventSystemExists();
        
        try 
        {
            // Create main container
            GameObject mainContainer = CreateMainContainer(canvas);
            Debug.Log("✅ Main container created successfully");
        
            // Create step indicator
            CreateStepIndicator(mainContainer);
            Debug.Log("✅ Step indicator created successfully");
            
            // Create all menu panels with actual UI elements (initially hidden)
            CreateCompleteMainMenuPanel(mainContainer);
            Debug.Log("✅ Main menu panel created successfully");
            
            CreateCompletePlayerCountPanel(mainContainer);
            Debug.Log("✅ Player count panel created successfully");
            
            CreateCompleteAICountPanel(mainContainer);
            Debug.Log("✅ AI count panel created successfully");
            
            CreateCompleteSideSelectionPanel(mainContainer);
            Debug.Log("✅ Side selection panel created successfully");
            
            CreateCompleteBoardSizePanel(mainContainer);
            Debug.Log("✅ Board size panel created successfully");
            
            CreateCompleteConfirmationPanel(mainContainer);
            Debug.Log("✅ Confirmation panel created successfully");
            
            // Create navigation buttons
            CreateNavigationButtons(mainContainer);
            Debug.Log("✅ Navigation buttons created successfully");
            
            // Hide all panels initially - they'll be shown by ShowStep
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (playerCountPanel) playerCountPanel.SetActive(false);
            if (aiCountPanel) aiCountPanel.SetActive(false);
            if (sideSelectionPanel) sideSelectionPanel.SetActive(false);
            if (boardSizePanel) boardSizePanel.SetActive(false);
            if (confirmationPanel) confirmationPanel.SetActive(false);
            
            Debug.Log("✅ MainMenuController: Complete enhanced menu UI created successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ MainMenuController: Error creating UI: {e.Message}");
            Debug.LogError($"❌ Stack trace: {e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Create Canvas if it doesn't exist
    /// </summary>
    private void CreateCanvas()
    {
        Debug.Log("🖼️ Creating Canvas for UI");
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // Ensure UI is on top
        
        // Add CanvasScaler for proper scaling
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // CRITICAL: Add GraphicRaycaster for click detection
        var raycaster = canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;
        raycaster.blockingObjects = UnityEngine.UI.GraphicRaycaster.BlockingObjects.None;
        
        Debug.Log($"✅ Canvas created: mode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}");
        Debug.Log($"✅ GraphicRaycaster added: enabled={raycaster.enabled}");
        
        Debug.Log("🎨 MainMenuController: Created Canvas");
    }
    
    /// <summary>
    /// Ensure EventSystem exists for UI input (separate from Canvas creation)
    /// </summary>
    private void EnsureEventSystemExists()
    {
        // Create EventSystem if it doesn't exist
        var existingEventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existingEventSystem == null)
        {
            Debug.Log("🎮 Creating EventSystem for UI input");
            GameObject eventSystemObj = new GameObject("EventSystem");
            var eventSystem = eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputModule = eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            // Configure input module properly
            inputModule.horizontalAxis = "Horizontal";
            inputModule.verticalAxis = "Vertical";
            inputModule.submitButton = "Submit";
            inputModule.cancelButton = "Cancel";
            
            Debug.Log($"✅ EventSystem created: {eventSystem != null && eventSystem.enabled}");
            Debug.Log($"✅ Input module created: {inputModule != null && inputModule.enabled}");
            Debug.Log($"✅ EventSystem GameObject active: {eventSystemObj.activeInHierarchy}");
        }
        else
        {
            Debug.Log($"✅ EventSystem already exists: enabled={existingEventSystem.enabled}, active={existingEventSystem.gameObject.activeInHierarchy}");
            var inputModule = existingEventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log($"✅ Input module exists: {inputModule != null && inputModule.enabled}");
        }
    }
    
    /// <summary>
    /// Create main container for all UI
    /// </summary>
    private GameObject CreateMainContainer(Canvas canvas)
    {
        GameObject container = new GameObject("MenuContainer");
        container.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        return container;
    }
    
    /// <summary>
    /// Create step indicator at top
    /// </summary>
    private void CreateStepIndicator(GameObject parent)
    {
        GameObject stepObj = new GameObject("StepIndicator");
        stepObj.transform.SetParent(parent.transform, false);
        
        stepIndicatorText = stepObj.AddComponent<TMPro.TextMeshProUGUI>();
        stepIndicatorText.text = "3D Chess Setup";
        stepIndicatorText.fontSize = 28;
        stepIndicatorText.color = Color.white;
        stepIndicatorText.alignment = TMPro.TextAlignmentOptions.Center;
        
        RectTransform rect = stepObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.9f);
        rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }
    
    /// <summary>
    /// Create complete main menu panel with working New Game button
    /// </summary>
    private void CreateCompleteMainMenuPanel(GameObject parent)
    {
        mainMenuPanel = new GameObject("MainMenuPanel");
        mainMenuPanel.transform.SetParent(parent.transform, false);
        
        RectTransform panelRect = mainMenuPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.25f);  // Better margins
        panelRect.anchorMax = new Vector2(0.85f, 0.8f);   // More space
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        // Title with more space
        CreateUIText("Title", "3D Chess", mainMenuPanel, new Vector2(0, 0.75f), new Vector2(1, 0.95f), 32);
        
        // Subtitle for clarity
        CreateUIText("Subtitle", "Welcome to 3D Chess", mainMenuPanel, new Vector2(0, 0.6f), new Vector2(1, 0.75f), 18);
        
        // New Game Button (this is the key button that must work!) - bigger and more prominent
        newGameButton = CreateUIButton("NewGameButton", "New Game", mainMenuPanel, 
                                     new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.55f),
                                     () => {
                                         Debug.Log("🎮 MainMenuController: New Game button clicked!");
                                         StartEnhancedFlow();
                                     });
        
        // Exit Button - smaller and lower
        exitButton = CreateUIButton("ExitButton", "Exit", mainMenuPanel,
                                   new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.25f),
                                   () => {
                                       Debug.Log("🚪 MainMenuController: Exit button clicked!");
                                       QuitGame();
                                   });
        
        Debug.Log("🎨 MainMenuController: Main menu panel created with working buttons");
    }
    
    /// <summary>
    /// Create complete player count panel
    /// </summary>
    private void CreateCompletePlayerCountPanel(GameObject parent)
    {
        playerCountPanel = new GameObject("PlayerCountPanel");
        playerCountPanel.transform.SetParent(parent.transform, false);
        
        RectTransform panelRect = playerCountPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.25f);  // Consistent margins
        panelRect.anchorMax = new Vector2(0.85f, 0.8f);   // Consistent height
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        // Step title
        CreateUIText("Title", "Step 1: Number of Players", playerCountPanel, new Vector2(0, 0.8f), new Vector2(1, 0.95f), 24);
        
        // Current selection - more prominent
        playerCountText = CreateUIText("PlayerCountText", "2 Players", playerCountPanel, new Vector2(0, 0.6f), new Vector2(1, 0.75f), 28);
        playerCountText.color = new Color(1f, 0.8f, 0.2f, 1f); // Golden yellow
        
        // Info text with better spacing
        CreateUIText("InfoText", "Currently supports 2 players", 
                    playerCountPanel, new Vector2(0, 0.45f), new Vector2(1, 0.6f), 16);
        
        CreateUIText("FutureText", "(4 and 6 player modes coming soon!)", 
                    playerCountPanel, new Vector2(0, 0.3f), new Vector2(1, 0.45f), 14);
    }
    
    /// <summary>
    /// Create complete AI count panel with working slider
    /// </summary>
    private void CreateCompleteAICountPanel(GameObject parent)
    {
        aiCountPanel = new GameObject("AICountPanel");
        aiCountPanel.transform.SetParent(parent.transform, false);
        
        RectTransform panelRect = aiCountPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.25f);  // Consistent margins
        panelRect.anchorMax = new Vector2(0.85f, 0.8f);   // Consistent height
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        // Step title
        CreateUIText("Title", "Step 2: AI Players", aiCountPanel, new Vector2(0, 0.8f), new Vector2(1, 0.95f), 24);
        
        // Instructions
        CreateUIText("Instructions", "How many players should be controlled by AI?", 
                    aiCountPanel, new Vector2(0, 0.65f), new Vector2(1, 0.8f), 16);
        
        // Create AI count selection buttons instead of complex slider
        Debug.Log("🔘 Creating AI count buttons with event handlers...");
        CreateUIButton("0AIButton", "0 AI (Human vs Human)", aiCountPanel,
                      new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.6f),
                      () => {
                          Debug.Log("🔘 0 AI button clicked!");
                          OnAICountChanged(0);
                      });
                      
        CreateUIButton("1AIButton", "1 AI (Human vs AI)", aiCountPanel,
                      new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.45f),
                      () => {
                          Debug.Log("🔘 1 AI button clicked!");
                          OnAICountChanged(1);
                      });
                      
        CreateUIButton("2AIButton", "2 AI (AI vs AI)", aiCountPanel,
                      new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.3f),
                      () => {
                          Debug.Log("🔘 2 AI button clicked!");
                          OnAICountChanged(2);
                      });
        Debug.Log("🔘 AI count buttons created with event handlers");
        
        // Current selection display with better spacing (below the buttons)
        aiCountText = CreateUIText("AICountText", "0 AI Players (Human vs Human)", aiCountPanel, new Vector2(0, 0.05f), new Vector2(1, 0.15f), 16);
        aiCountText.color = new Color(1f, 0.8f, 0.2f, 1f); // Golden yellow
    }
    
    /// <summary>
    /// Create complete side selection panel for scalable player assignment
    /// </summary>
    private void CreateCompleteSideSelectionPanel(GameObject parent)
    {
        // Create panel
        sideSelectionPanel = new GameObject("SideSelectionPanel");
        sideSelectionPanel.transform.SetParent(parent.transform, false);
        sideSelectionPanel.AddComponent<RectTransform>();
        RectTransform sideRT = sideSelectionPanel.GetComponent<RectTransform>();
        sideRT.anchorMin = Vector2.zero;
        sideRT.anchorMax = Vector2.one;
        sideRT.offsetMin = Vector2.zero;
        sideRT.offsetMax = Vector2.zero;
        
        // Title
        CreateUIText("SideSelectionTitle", "Choose Player Positions", sideSelectionPanel, new Vector2(0, 0.7f), new Vector2(1, 0.2f), 24);
        
        // Instructions (will be updated dynamically based on player count)
        CreateUIText("SideSelectionInstructions", "Select how to assign human and AI players", sideSelectionPanel, new Vector2(0, 0.55f), new Vector2(1, 0.1f), 16);
        
        // === SIMPLE 2-PLAYER UI ===
        // Direct White/Black choice buttons (for 2-player Human vs AI)
        // Positioned side-by-side with proper spacing and sizing
        // White chess piece colors: bright white with subtle gray highlights
        Color whiteNormal = new Color(0.95f, 0.95f, 0.95f, 1f);       // Bright white like chess piece
        Color whiteHighlight = new Color(1f, 1f, 1f, 1f);             // Pure white on hover
        Color whitePressed = new Color(0.85f, 0.85f, 0.85f, 1f);      // Slightly darker when pressed
        
        CreateUIButton("PlayAsWhiteButton", "Play as White", sideSelectionPanel, 
            new Vector2(0.05f, 0.4f), new Vector2(0.45f, 0.55f), () => OnPlayAsWhiteSelected(), 
            whiteNormal, whiteHighlight, whitePressed);
        
        // Black chess piece colors: dark gray like chess piece (not pure black for visibility)
        Color blackNormal = new Color(0.25f, 0.25f, 0.25f, 1f);       // Dark gray like chess piece
        Color blackHighlight = new Color(0.35f, 0.35f, 0.35f, 1f);     // Lighter gray on hover
        Color blackPressed = new Color(0.15f, 0.15f, 0.15f, 1f);       // Darker gray when pressed
        
        CreateUIButton("PlayAsBlackButton", "Play as Black", sideSelectionPanel, 
            new Vector2(0.55f, 0.4f), new Vector2(0.95f, 0.55f), () => OnPlayAsBlackSelected(),
            blackNormal, blackHighlight, blackPressed);
        
        // === COMPLEX MULTI-PLAYER UI ===
        // Human player position selection (for multi-players)
        CreateUIButton("ChoosePositionButton", "Choose My Position", sideSelectionPanel, 
            new Vector2(0.1f, 0.25f), new Vector2(0.35f, 0.15f), () => OnManualPositionSelection());
        
        // Random assignment button
        CreateUIButton("RandomAssignButton", "Random Assignment", sideSelectionPanel, 
            new Vector2(0.55f, 0.25f), new Vector2(0.35f, 0.15f), () => OnRandomAssignment());
        
        // Current assignment display
        CreateUIText("CurrentAssignmentTitle", "Current Assignment:", sideSelectionPanel, new Vector2(0, 0.15f), new Vector2(1, 0.08f), 14);
        
        // Assignment details (will be updated by UpdateSideSelectionUI)
        CreateUIText("AssignmentDetails", "", sideSelectionPanel, new Vector2(0, 0.05f), new Vector2(1, 0.1f), 12);
    }
    
    /// <summary>
    /// Create complete board size panel with working buttons
    /// </summary>
    private void CreateCompleteBoardSizePanel(GameObject parent)
    {
        boardSizePanel = new GameObject("BoardSizePanel");
        boardSizePanel.transform.SetParent(parent.transform, false);
        
        RectTransform panelRect = boardSizePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.25f);  // Consistent margins
        panelRect.anchorMax = new Vector2(0.85f, 0.8f);   // Consistent height
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        // Step title
        CreateUIText("Title", "Step 3: Board Size", boardSizePanel, new Vector2(0, 0.8f), new Vector2(1, 0.95f), 24);
        
        // Instructions
        CreateUIText("Instructions", "Choose your board size:", 
                    boardSizePanel, new Vector2(0, 0.7f), new Vector2(1, 0.8f), 16);
        
        // Board size buttons with better spacing
        CreateUIButton("4x4x4Button", "4x4x4 (Compact)", boardSizePanel,
                      new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.65f),
                      () => {
                          Debug.Log("🎲 Board size selected: 4x4x4");
                          SetBoardSize(BoardSize.Small4x4x4);
                      });
                      
        CreateUIButton("6x6x6Button", "6x6x6 (Standard)", boardSizePanel,
                      new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.5f),
                      () => {
                          Debug.Log("🎲 Board size selected: 6x6x6");
                          SetBoardSize(BoardSize.Medium6x6x6);
                      });
                      
        CreateUIButton("8x8x8Button", "8x8x8 (Large)", boardSizePanel,
                      new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.35f),
                      () => {
                          Debug.Log("🎲 Board size selected: 8x8x8");
                          SetBoardSize(BoardSize.Large8x8x8);
                      });
                      
        // Current selection display with better spacing (below the buttons)
        boardSizeText = CreateUIText("BoardSizeText", "⚠️ REQUIRED: Please select a board size", boardSizePanel, 
                                   new Vector2(0, 0.05f), new Vector2(1, 0.15f), 16);
        boardSizeText.color = new Color(1f, 0.6f, 0.0f, 1f); // Orange to indicate required
    }
    
    /// <summary>
    /// Create complete confirmation panel
    /// </summary>
    private void CreateCompleteConfirmationPanel(GameObject parent)
    {
        confirmationPanel = new GameObject("ConfirmationPanel");
        confirmationPanel.transform.SetParent(parent.transform, false);
        
        RectTransform panelRect = confirmationPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.25f);  // Consistent margins
        panelRect.anchorMax = new Vector2(0.85f, 0.8f);   // Consistent height
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        // Step title
        CreateUIText("Title", "Step 4: Review & Start", confirmationPanel, new Vector2(0, 0.8f), new Vector2(1, 0.95f), 24);
        
        // Instructions with better spacing
        CreateUIText("Instructions", "Review your game configuration:", 
                    confirmationPanel, new Vector2(0, 0.7f), new Vector2(1, 0.8f), 16);
        
        // Configuration summary with improved layout
        confirmationText = CreateUIText("ConfigSummary", "Configuration will appear here", 
                                      confirmationPanel, new Vector2(0, 0.45f), new Vector2(1, 0.7f), 18);
        confirmationText.color = new Color(1f, 0.8f, 0.2f, 1f); // Golden yellow for consistency
        
        // Start Game button with better spacing
        startGameButton = CreateUIButton("StartGameButton", "Start Game", confirmationPanel,
                                       new Vector2(0.1f, 0.25f), new Vector2(0.9f, 0.4f),
                                       () => {
                                           Debug.Log("🚀 MainMenuController: Start Game button clicked!");
                                           StartGame();
                                       });
    }
    
    /// <summary>
    /// Create navigation buttons
    /// </summary>
    private void CreateNavigationButtons(GameObject parent)
    {
        GameObject navContainer = new GameObject("NavigationButtons");
        navContainer.transform.SetParent(parent.transform, false);
        
        RectTransform navRect = navContainer.AddComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0, 0);
        navRect.anchorMax = new Vector2(1, 0.15f);
        navRect.sizeDelta = Vector2.zero;
        navRect.anchoredPosition = Vector2.zero;
        
        backButton = CreateUIButton("BackButton", "Back", navContainer,
                                   new Vector2(0.1f, 0.3f), new Vector2(0.4f, 0.7f),
                                   GoToPreviousStep);
                                   
        nextButton = CreateUIButton("NextButton", "Next", navContainer,
                                   new Vector2(0.6f, 0.3f), new Vector2(0.9f, 0.7f),
                                   GoToNextStep);
    }
    
    /// <summary>
    /// Helper method to create UI text
    /// </summary>
    private TMPro.TextMeshProUGUI CreateUIText(string name, string text, GameObject parent, Vector2 anchorMin, Vector2 anchorMax, float fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        
        TMPro.TextMeshProUGUI textComponent = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TMPro.TextAlignmentOptions.Center;
        
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        
        return textComponent;
    }
    
    /// <summary>
    /// Helper method to create UI button
    /// </summary>
    private UnityEngine.UI.Button CreateUIButton(string name, string text, GameObject parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);
        
        // Ensure clean transform to prevent quaternion warnings
        buttonObj.transform.localRotation = Quaternion.identity;
        buttonObj.transform.localScale = Vector3.one;
        
        // Create button image with proper setup for clickability
        UnityEngine.UI.Image buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
        
        // Create a simple white sprite if no built-in sprite is available
        if (buttonImage.sprite == null)
        {
            // Create a 1x1 white texture as fallback
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            buttonImage.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
        
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1.0f); // Dark gray background
        
        // CRITICAL: Enable raycast target for click detection
        buttonImage.raycastTarget = true;
        
        // Create button component with proper setup
        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = buttonImage;
        button.interactable = true; // Ensure button is interactable
        
        // Set up proper button color transitions with better visibility
        var colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.4f, 0.8f, 1.0f);     // Blue
        colors.highlightedColor = new Color(0.3f, 0.5f, 0.9f, 1.0f); // Lighter blue on hover
        colors.pressedColor = new Color(0.1f, 0.2f, 0.6f, 1.0f);     // Darker blue when pressed
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);    // Gray when disabled
        button.colors = colors;
        
        // Set up RectTransform with proper positioning
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.sizeDelta = Vector2.zero;
        buttonRect.anchoredPosition = Vector2.zero;
        
        // Create button text with better positioning
        TMPro.TextMeshProUGUI buttonText = CreateUIText("Text", text, buttonObj, 
            new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), 18);
        buttonText.color = Color.white;
        
        // Add click handler with debug logging
        if (onClick != null)
        {
            button.onClick.AddListener(() => {
                Debug.Log($"🔘 Button clicked: {name}");
                try 
                {
                    onClick();
                    Debug.Log($"✅ Button action completed: {name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Button action failed for {name}: {e.Message}");
                }
            });
            Debug.Log($"🔗 Click handler registered for button: {name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ No onClick action provided for button: {name}");
        }
        
        // Debug button state
        Debug.Log($"🔘 Created clickable button: {name}");
        Debug.Log($"  - Interactable: {button.interactable}");
        Debug.Log($"  - Raycast Target: {buttonImage.raycastTarget}");
        Debug.Log($"  - GameObject Active: {buttonObj.activeInHierarchy}");
        Debug.Log($"  - Has Click Handler: {button.onClick.GetPersistentEventCount() + 1 > 0}");
        
        return button;
    }
    
    /// <summary>
    /// Helper method to create UI button with custom chess piece colors
    /// </summary>
    private UnityEngine.UI.Button CreateUIButton(string name, string text, GameObject parent, Vector2 anchorMin, Vector2 anchorMax, System.Action onClick, Color normalColor, Color highlightColor, Color pressedColor)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);
        
        // Ensure clean transform to prevent quaternion warnings
        buttonObj.transform.localRotation = Quaternion.identity;
        buttonObj.transform.localScale = Vector3.one;
        
        // Create button image with proper setup for clickability
        UnityEngine.UI.Image buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
        
        // Create a simple white sprite if no built-in sprite is available
        if (buttonImage.sprite == null)
        {
            // Create a 1x1 white texture as fallback
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            buttonImage.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
        
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1.0f); // Dark gray background
        
        // CRITICAL: Enable raycast target for click detection
        buttonImage.raycastTarget = true;
        
        // Create button component with proper setup
        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = buttonImage;
        button.interactable = true; // Ensure button is interactable
        
        // Set up chess piece-themed color transitions
        var colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightColor;
        colors.pressedColor = pressedColor;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);    // Gray when disabled
        button.colors = colors;
        
        // Set up RectTransform with proper positioning
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.sizeDelta = Vector2.zero;
        buttonRect.anchoredPosition = Vector2.zero;
        
        // Create button text with better positioning
        TMPro.TextMeshProUGUI buttonText = CreateUIText("Text", text, buttonObj, 
            new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), 18);
        buttonText.color = Color.white;
        
        // Add click handler with debug logging
        if (onClick != null)
        {
            button.onClick.AddListener(() => {
                Debug.Log($"🔘 Button clicked: {name}");
                try 
                {
                    onClick();
                    Debug.Log($"✅ Button action completed: {name}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Button action failed for {name}: {e.Message}");
                }
            });
            Debug.Log($"🔗 Click handler registered for button: {name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ No onClick action provided for button: {name}");
        }
        
        // Debug button state
        Debug.Log($"🔘 Created chess-colored button: {name}");
        Debug.Log($"  - Interactable: {button.interactable}");
        Debug.Log($"  - Raycast Target: {buttonImage.raycastTarget}");
        Debug.Log($"  - GameObject Active: {buttonObj.activeInHierarchy}");
        Debug.Log($"  - Has Click Handler: {button.onClick.GetPersistentEventCount() + 1 > 0}");
        Debug.Log($"  - Normal Color: {normalColor}");
        
        return button;
    }
    
    /// <summary>
    /// Helper method to create UI slider
    /// </summary>
    private UnityEngine.UI.Slider CreateUISlider(string name, GameObject parent, Vector2 anchorMin, Vector2 anchorMax, float minValue, float maxValue, float currentValue, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent.transform, false);
        
        // Add RectTransform component first
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = anchorMin;
        sliderRect.anchorMax = anchorMax;
        sliderRect.sizeDelta = Vector2.zero;
        sliderRect.anchoredPosition = Vector2.zero;
        
        UnityEngine.UI.Slider slider = sliderObj.AddComponent<UnityEngine.UI.Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.wholeNumbers = true;
        slider.value = currentValue;
        
        // Create slider background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform, false);
        UnityEngine.UI.Image bgImage = background.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Create slider fill
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-20, 0);
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        UnityEngine.UI.Image fillImage = fill.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = new Color(0.3f, 0.7f, 0.3f, 0.8f);
        
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        // Create slider handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20, 0);
        handleAreaRect.anchoredPosition = Vector2.zero;
        
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        UnityEngine.UI.Image handleImage = handle.AddComponent<UnityEngine.UI.Image>();
        handleImage.color = Color.white;
        
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(20, 20);
        handleRect.anchoredPosition = Vector2.zero;
        
        // Assign to slider
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        
        if (onValueChanged != null)
        {
            slider.onValueChanged.AddListener(onValueChanged);
        }
        
        return slider;
    }
    
    /// <summary>
    /// Show a specific menu step
    /// </summary>
    private void ShowStep(MenuStep step)
    {
        currentStep = step;
        Debug.Log($"🎯 MainMenuController.ShowStep: Transitioning to {step}");
        
        // Hide all panels first
        Debug.Log("🔇 MainMenuController: Hiding all panels");
        if (mainMenuPanel) { mainMenuPanel.SetActive(false); Debug.Log("  - Main menu panel hidden"); }
        if (playerCountPanel) { playerCountPanel.SetActive(false); Debug.Log("  - Player count panel hidden"); }
        if (aiCountPanel) { aiCountPanel.SetActive(false); Debug.Log("  - AI count panel hidden"); }
        if (sideSelectionPanel) { sideSelectionPanel.SetActive(false); Debug.Log("  - Side selection panel hidden"); }
        if (boardSizePanel) { boardSizePanel.SetActive(false); Debug.Log("  - Board size panel hidden"); }
        if (confirmationPanel) { confirmationPanel.SetActive(false); Debug.Log("  - Confirmation panel hidden"); }
        
        // Show current panel
        Debug.Log($"🔆 MainMenuController: Showing panel for step {step}");
        switch (step)
        {
            case MenuStep.MainMenu:
                if (mainMenuPanel) { mainMenuPanel.SetActive(true); Debug.Log("  ✅ Main menu panel shown"); }
                UpdateStepIndicator("3D Chess - Main Menu");
                break;
                
            case MenuStep.PlayerCount:
                if (playerCountPanel) { playerCountPanel.SetActive(true); Debug.Log("  ✅ Player count panel shown"); }
                UpdateStepIndicator("Step 1: Players");
                currentConfig.playerCount = 2; // Fixed at 2 for now
                break;
                
            case MenuStep.AIPlayerCount:
                if (aiCountPanel) { aiCountPanel.SetActive(true); Debug.Log("  ✅ AI count panel shown"); }
                UpdateStepIndicator("Step 2: AI Players");
                // Reset AI selection flag when entering this step
                Debug.Log($"🔄 ShowStep.AIPlayerCount: Resetting hasSelectedAICount from {hasSelectedAICount} to false");
                hasSelectedAICount = false;
                if (aiCountText != null)
                {
                    aiCountText.text = "⚠️ REQUIRED: Please click one of the buttons below to select AI count";
                    aiCountText.color = new Color(1f, 0.6f, 0.0f, 1f); // Orange to indicate required
                    Debug.Log("🔄 Reset AI count text and color");
                }
                else
                {
                    Debug.LogWarning("🔄 aiCountText is null in ShowStep");
                }
                
                // Clear any previous button highlights
                if (aiCountPanel != null)
                {
                    var buttons = aiCountPanel.GetComponentsInChildren<Button>();
                    foreach (var button in buttons)
                    {
                        ColorBlock colors = button.colors;
                        colors.normalColor = Color.white; // Reset all to default
                        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1.0f);
                        button.colors = colors;
                    }
                    Debug.Log("🔄 Reset all AI count button highlights");
                }
                break;
                
            case MenuStep.SideSelection:
                if (sideSelectionPanel) { sideSelectionPanel.SetActive(true); Debug.Log("  ✅ Side selection panel shown"); }
                UpdateStepIndicator("Step 3: Player Positions");
                UpdateSideSelectionUI();
                break;
                
            case MenuStep.BoardSize:
                if (boardSizePanel) { boardSizePanel.SetActive(true); Debug.Log("  ✅ Board size panel shown"); }
                // Dynamic step numbering based on whether side selection was skipped
                string stepNumber = currentConfig.NeedsSideSelection() ? "Step 4" : "Step 3";
                UpdateStepIndicator($"{stepNumber}: Board Size");
                
                // Reset board size selection flag when entering this step
                Debug.Log($"🔄 ShowStep.BoardSize: Resetting hasSelectedBoardSize from {hasSelectedBoardSize} to false");
                hasSelectedBoardSize = false;
                
                // Reset board size text
                if (boardSizeText != null)
                {
                    boardSizeText.text = "⚠️ REQUIRED: Please select a board size";
                    boardSizeText.color = new Color(1f, 0.6f, 0.0f, 1f); // Orange to indicate required
                    Debug.Log("🔄 Reset board size text and color");
                }
                else
                {
                    Debug.LogWarning("🔄 boardSizeText is null in ShowStep");
                }
                
                // Clear any previous button highlights
                if (boardSizePanel != null)
                {
                    Button[] buttons = boardSizePanel.GetComponentsInChildren<Button>();
                    foreach (Button button in buttons)
                    {
                        ColorBlock colors = button.colors;
                        colors.normalColor = Color.white; // Reset all to default
                        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1.0f);
                        button.colors = colors;
                    }
                    Debug.Log("🔄 Reset all board size button highlights");
                }
                break;
                
            case MenuStep.FinalConfirmation:
                if (confirmationPanel) { confirmationPanel.SetActive(true); Debug.Log("  ✅ Confirmation panel shown"); }
                // Dynamic step numbering based on whether side selection was skipped
                string finalStepNumber = currentConfig.NeedsSideSelection() ? "Step 5" : "Step 4";
                UpdateStepIndicator($"{finalStepNumber}: Confirmation");
                UpdateConfirmationText();
                break;
        }
        
        // Update navigation button visibility
        UpdateNavigationButtons();
        
        // Debug button state after showing panel
        if (step == MenuStep.MainMenu && newGameButton != null)
        {
            Debug.Log($"🔍 After ShowStep - New Game Button: active={newGameButton.gameObject.activeInHierarchy}, panel active={mainMenuPanel?.activeInHierarchy}");
        }
    }
    
    /// <summary>
    /// Update step indicator text
    /// </summary>
    private void UpdateStepIndicator(string text)
    {
        if (stepIndicatorText != null)
        {
            stepIndicatorText.text = text;
        }
    }
    
    /// <summary>
    /// Update navigation button visibility based on current step
    /// </summary>
    private void UpdateNavigationButtons()
    {
        // Hide navigation buttons on main menu, show on other steps
        bool showNavigation = currentStep != MenuStep.MainMenu;
        
        if (backButton != null)
        {
            backButton.gameObject.SetActive(showNavigation);
        }
        
        if (nextButton != null)
        {
            // Show Next button for all steps except confirmation
            bool showNext = showNavigation && currentStep != MenuStep.FinalConfirmation;
            nextButton.gameObject.SetActive(showNext);
            
            // Provide visual feedback for required selections
            if (showNext && currentStep == MenuStep.AIPlayerCount)
            {
                // Make Next button appear disabled if AI count not selected
                nextButton.interactable = hasSelectedAICount;
                
                // Update button appearance based on state
                var buttonImage = nextButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = hasSelectedAICount ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.8f);
                }
                
                // Update button text to provide guidance
                var buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = hasSelectedAICount ? "Next" : "Select AI Count First";
                }
            }
            else if (showNext)
            {
                // Reset button to normal state for other steps
                nextButton.interactable = true;
                var buttonImage = nextButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = Color.white;
                }
                var buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "Next";
                }
            }
        }
    }
    
    /// <summary>
    /// Go to next step in the enhanced flow
    /// </summary>
    public void GoToNextStep()
    {
        Debug.Log($"🚀 MainMenuController.GoToNextStep: ENTRY - currentStep={currentStep}");
        Debug.Log($"🚀 Current state - hasSelectedAICount={hasSelectedAICount}, aiPlayerCount={currentConfig.aiPlayerCount}");
        
        // Validate current step before proceeding
        bool canProceed = CanProceedFromCurrentStep();
        Debug.Log($"🚀 CanProceedFromCurrentStep() returned: {canProceed}");
        
        if (!canProceed)
        {
            Debug.LogWarning("🚀 Cannot proceed from current step - validation failed");
            return; // Don't advance if validation fails
        }
        
        MenuStep nextStep = currentStep switch
        {
            MenuStep.MainMenu => MenuStep.PlayerCount,
            MenuStep.PlayerCount => MenuStep.AIPlayerCount,
            MenuStep.AIPlayerCount => GetNextStepAfterAI(),
            MenuStep.SideSelection => MenuStep.BoardSize,
            MenuStep.BoardSize => MenuStep.FinalConfirmation,
            _ => currentStep
        };
        
        Debug.Log($"🚀 Determined next step: {currentStep} → {nextStep}");
        
        if (currentStep == MenuStep.AIPlayerCount)
        {
            Debug.Log($"🚀 Special handling for AI count step:");
            Debug.Log($"🚀   - NeedsSideSelection: {currentConfig.NeedsSideSelection()}");
            Debug.Log($"🚀   - aiPlayerCount: {currentConfig.aiPlayerCount}");
            Debug.Log($"🚀   - playerCount: {currentConfig.playerCount}");
        }
        
        ShowStep(nextStep);
        Debug.Log($"🚀 MainMenuController.GoToNextStep: EXIT - transitioned to {nextStep}");
    }
    
    /// <summary>
    /// Validate if we can proceed from the current step
    /// </summary>
    private bool CanProceedFromCurrentStep()
    {
        Debug.Log($"🔍 CanProceedFromCurrentStep: Checking step {currentStep}");
        
        switch (currentStep)
        {
            case MenuStep.AIPlayerCount:
                Debug.Log($"🔍 AI Count validation - hasSelectedAICount={hasSelectedAICount}, aiPlayerCount={currentConfig.aiPlayerCount}");
                
                // Primary validation: check the explicit selection flag
                if (!hasSelectedAICount)
                {
                    Debug.LogWarning("🔍 ❌ Cannot proceed - AI count not selected (hasSelectedAICount=false)");
                    Debug.LogWarning("🔍 User must explicitly select an AI count option to proceed");
                    ShowAICountSelectionRequired();
                    return false;
                }
                Debug.Log("🔍 ✅ AI Count validation passed");
                break;
                
            case MenuStep.SideSelection:
                Debug.Log($"🔍 Side Selection validation - playerCount={currentConfig.playerCount}, aiPlayerCount={currentConfig.aiPlayerCount}");
                
                // For 2-player Human vs AI, validate that sides have been assigned
                if (currentConfig.playerCount == 2 && currentConfig.aiPlayerCount == 1)
                {
                    bool hasValidAssignment = (currentConfig.whitePlayerType == PlayerType.Human && currentConfig.blackPlayerType == PlayerType.Computer) ||
                                            (currentConfig.whitePlayerType == PlayerType.Computer && currentConfig.blackPlayerType == PlayerType.Human);
                    
                    if (!hasValidAssignment)
                    {
                        Debug.LogWarning("🔍 ❌ Cannot proceed - Side selection not made for Human vs AI game");
                        Debug.LogWarning("🔍 User must select White or Black to proceed");
                        ShowSideSelectionRequired();
                        return false;
                    }
                    Debug.Log($"🔍 ✅ Side Selection validation passed - Human: {(currentConfig.whitePlayerType == PlayerType.Human ? "White" : "Black")}");
                }
                else
                {
                    // For other scenarios, validate using playerTypes array
                    if (!currentConfig.ValidateAssignment())
                    {
                        Debug.LogWarning("🔍 ❌ Cannot proceed - Invalid player type assignment");
                        ShowSideSelectionRequired();
                        return false;
                    }
                    Debug.Log("🔍 ✅ Side Selection validation passed for multi-player scenario");
                }
                break;
                
            case MenuStep.BoardSize:
                Debug.Log($"🔍 Board Size validation - hasSelectedBoardSize={hasSelectedBoardSize}, boardSize={currentConfig.boardSize}");
                
                // Primary validation: check the explicit selection flag
                if (!hasSelectedBoardSize)
                {
                    Debug.LogWarning("🔍 ❌ Cannot proceed - Board size not selected (hasSelectedBoardSize=false)");
                    Debug.LogWarning("🔍 User must explicitly select a board size option to proceed");
                    ShowBoardSizeSelectionRequired();
                    return false;
                }
                Debug.Log("🔍 ✅ Board Size validation passed");
                break;
                
            // Add other step validations here if needed
            default:
                Debug.Log($"🔍 ✅ No specific validation needed for step {currentStep}");
                break;
        }
        
        Debug.Log("🔍 ✅ CanProceedFromCurrentStep: All validations passed");
        return true;
    }
    
    /// <summary>
    /// Show user feedback that AI count selection is required
    /// </summary>
    private void ShowAICountSelectionRequired()
    {
        // Update the AI count text to show it's required
        if (aiCountText != null)
        {
            aiCountText.text = "⚠️ Please select number of AI players";
            aiCountText.color = Color.red;
        }
    }
    
    /// <summary>
    /// Show user feedback that side selection is required
    /// </summary>
    private void ShowSideSelectionRequired()
    {
        // Update the assignment details to show selection is required
        if (sideSelectionPanel != null)
        {
            Transform assignmentDetails = sideSelectionPanel.transform.Find("AssignmentDetails");
            if (assignmentDetails?.GetComponent<TextMeshProUGUI>() != null)
            {
                assignmentDetails.GetComponent<TextMeshProUGUI>().text = "⚠️ Please make a selection to proceed";
                assignmentDetails.GetComponent<TextMeshProUGUI>().color = Color.red;
            }
        }
    }
    
    /// <summary>
    /// Show user feedback that board size selection is required
    /// </summary>
    private void ShowBoardSizeSelectionRequired()
    {
        // Update the board size text to show it's required
        if (boardSizeText != null)
        {
            boardSizeText.text = "⚠️ Please select a board size";
            boardSizeText.color = Color.red;
        }
    }
    
    /// <summary>
    /// Update board size panel to show current selection
    /// </summary>
    private void UpdateBoardSizePanelSelection()
    {
        if (boardSizePanel == null) return;
        
        // Find and update all board size buttons to show selection
        Button[] buttons = boardSizePanel.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            // Reset all buttons to normal color first
            SetButtonHighlight(button, false);
            
            // Highlight the selected button based on actual button names
            if (button.name.Contains("4x4x4") && currentConfig.boardSize == BoardSize.Small4x4x4)
            {
                SetButtonHighlight(button, true);
                Debug.Log($"🔄 Highlighted board size button: {button.name} for Small4x4x4");
            }
            else if (button.name.Contains("6x6x6") && currentConfig.boardSize == BoardSize.Medium6x6x6)
            {
                SetButtonHighlight(button, true);
                Debug.Log($"🔄 Highlighted board size button: {button.name} for Medium6x6x6");
            }
            else if (button.name.Contains("8x8x8") && currentConfig.boardSize == BoardSize.Large8x8x8)
            {
                SetButtonHighlight(button, true);
                Debug.Log($"🔄 Highlighted board size button: {button.name} for Large8x8x8");
            }
        }
        
        // Update board size text to show selection
        if (boardSizeText != null)
        {
            boardSizeText.text = $"✅ Board Size: {currentConfig.GetBoardSizeDescription()}";
            boardSizeText.color = new Color(0f, 1f, 0f, 1f); // Green to indicate selection made
            Debug.Log($"🔄 Updated board size text: {boardSizeText.text}");
        }
    }
    
    /// <summary>
    /// Determine the next step after AI player count selection
    /// </summary>
    private MenuStep GetNextStepAfterAI()
    {
        // Initialize player types if needed
        if (currentConfig.playerTypes.Count != currentConfig.playerCount)
        {
            currentConfig.InitializePlayerTypes();
        }
        
        // If side selection is needed (mixed AI/human), go to side selection
        if (currentConfig.NeedsSideSelection())
        {
            return MenuStep.SideSelection;
        }
        
        // Otherwise, auto-assign and skip to board size
        if (currentConfig.aiPlayerCount == 0)
        {
            // All human players
            for (int i = 0; i < currentConfig.playerCount; i++)
            {
                currentConfig.SetPlayerTypeAtPosition(i, PlayerType.Human);
            }
        }
        else if (currentConfig.aiPlayerCount == currentConfig.playerCount)
        {
            // All AI players
            for (int i = 0; i < currentConfig.playerCount; i++)
            {
                currentConfig.SetPlayerTypeAtPosition(i, PlayerType.Computer);
            }
        }
        
        currentConfig.ApplyToLegacyFields();
        return MenuStep.BoardSize;
    }
    
    /// <summary>
    /// Go to previous step in the enhanced flow
    /// </summary>
    public void GoToPreviousStep()
    {
        MenuStep previousStep = currentStep switch
        {
            MenuStep.PlayerCount => MenuStep.MainMenu,
            MenuStep.AIPlayerCount => MenuStep.PlayerCount,
            MenuStep.SideSelection => MenuStep.AIPlayerCount,
            MenuStep.BoardSize => GetPreviousStepBeforeBoard(),
            MenuStep.FinalConfirmation => MenuStep.BoardSize,
            _ => MenuStep.MainMenu
        };
        
        ShowStep(previousStep);
    }
    
    /// <summary>
    /// Determine the previous step before board size selection
    /// </summary>
    private MenuStep GetPreviousStepBeforeBoard()
    {
        // If we have side selection in the flow, go back to it
        if (currentConfig.NeedsSideSelection())
        {
            return MenuStep.SideSelection;
        }
        
        // Otherwise, go back to AI player count
        return MenuStep.AIPlayerCount;
    }
    
    /// <summary>
    /// Handle AI count change for enhanced flow
    /// </summary>
    public void OnAICountChanged(int aiCount)
    {
        Debug.Log($"🎯 MainMenuController.OnAICountChanged: ENTRY - aiCount={aiCount}");
        Debug.Log($"🎯 Current state - hasSelectedAICount={hasSelectedAICount}, currentStep={currentStep}");
        
        currentConfig.aiPlayerCount = aiCount;
        hasSelectedAICount = true; // Mark that user has explicitly selected AI count
        
        Debug.Log($"🎯 MainMenuController: AI count selected: {aiCount} (hasSelectedAICount = {hasSelectedAICount})");
        Debug.Log($"🎯 Configuration updated - aiPlayerCount={currentConfig.aiPlayerCount}");
        
        // Update player types based on AI count
        if (aiCount == 0)
        {
            // All human - can assign immediately since no choice needed
            currentConfig.whitePlayerType = PlayerType.Human;
            currentConfig.blackPlayerType = PlayerType.Human;
            Debug.Log("🎯 Set player types: Human vs Human");
        }
        else if (aiCount == currentConfig.playerCount)
        {
            // All AI - can assign immediately since no choice needed
            currentConfig.whitePlayerType = PlayerType.Computer;
            currentConfig.blackPlayerType = PlayerType.Computer;
            Debug.Log("🎯 Set player types: AI vs AI");
        }
        else
        {
            // Mixed human/AI - assignment will be handled by side selection step
            Debug.Log($"🎯 Mixed human/AI configuration ({aiCount} AI, {currentConfig.playerCount - aiCount} human) - side selection required");
            // Do not set whitePlayerType/blackPlayerType here - let user choose in side selection
        }
        
        if (aiCountText != null)
        {
            string description = aiCount switch
            {
                0 => "✅ 0 AI Players (Human vs Human)",
                1 => "✅ 1 AI Player (Human vs AI)",
                2 => "✅ 2 AI Players (AI vs AI)",
                _ => $"✅ {aiCount} AI Players"
            };
            aiCountText.text = description;
            aiCountText.color = new Color(0f, 1f, 0f, 1f); // Green to indicate selection made
            Debug.Log($"🎯 Updated UI text: {description}");
        }
        else
        {
            Debug.LogWarning("🎯 aiCountText is null - cannot update UI");
        }
        
        // Update button highlighting to show selection
        UpdateAICountButtonHighlights(aiCount);
        
        // Update navigation button state since selection is now made
        Debug.Log("🎯 Calling UpdateNavigationButtons()...");
        UpdateNavigationButtons();
        
        // Debug navigation button state after update
        if (nextButton != null)
        {
            Debug.Log($"🎯 Next button state after update: interactable={nextButton.interactable}, active={nextButton.gameObject.activeInHierarchy}");
            var buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                Debug.Log($"🎯 Next button text: '{buttonText.text}'");
            }
        }
        else
        {
            Debug.LogWarning("🎯 nextButton is null");
        }
        
        Debug.Log($"🎯 MainMenuController.OnAICountChanged: EXIT - hasSelectedAICount={hasSelectedAICount}");
    }
    
    /// <summary>
    /// Update AI count button highlights to show which option is selected
    /// </summary>
    private void UpdateAICountButtonHighlights(int selectedCount)
    {
        Debug.Log($"🎨 UpdateAICountButtonHighlights: Highlighting option {selectedCount}");
        
        // Find and highlight the appropriate button
        if (aiCountPanel != null)
        {
            var buttons = aiCountPanel.GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                var button = buttons[i];
                bool isSelected = button.name == $"{selectedCount}AIButton";
                
                ColorBlock colors = button.colors;
                if (isSelected)
                {
                    colors.normalColor = new Color(0.3f, 0.8f, 0.3f, 1.0f); // Light green for selected
                    colors.highlightedColor = new Color(0.2f, 0.9f, 0.2f, 1.0f); // Brighter green on hover
                    Debug.Log($"🎨 Highlighted button: {button.name}");
                }
                else
                {
                    colors.normalColor = Color.white; // Default color
                    colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1.0f); // Light gray on hover
                }
                button.colors = colors;
            }
        }
        else
        {
            Debug.LogWarning("🎨 aiCountPanel is null - cannot update button highlights");
        }
    }
    
    /// <summary>
    /// Handle board size selection
    /// </summary>
    public void SetBoardSize(BoardSize size)
    {
        Debug.Log($"🎯 SetBoardSize: User selected {size}");
        currentConfig.boardSize = size;
        hasSelectedBoardSize = true;
        
        // Update board size panel UI to show selection
        UpdateBoardSizePanelSelection();
        
        Debug.Log($"🎯 Board size set to {size}, hasSelectedBoardSize={hasSelectedBoardSize}");
        // Do NOT automatically advance - let user click Next button
    }
    
    /// <summary>
    /// Update confirmation text with current configuration
    /// </summary>
    private void UpdateConfirmationText()
    {
        if (confirmationText != null && currentConfig != null)
        {
            string summary = $"Players: {currentConfig.GetPlayerSetupDescription()}\n";
            summary += $"Board: {currentConfig.GetBoardSizeDescription()}\n";
            summary += $"AI Difficulty: {currentConfig.aiDifficulty}";
            
            confirmationText.text = summary;
        }
    }
    
    /// <summary>
    /// Start the enhanced game flow (called by New Game button)
    /// </summary>
    public void StartEnhancedFlow()
    {
        Debug.Log("🚀 MainMenuController: Starting enhanced menu flow");
        
        // Initialize configuration with clean defaults
        currentConfig = new GameConfiguration();
        hasSelectedAICount = false;
        hasSelectedBoardSize = false;
        
        Debug.Log($"🚀 Initialized clean state - aiPlayerCount={currentConfig.aiPlayerCount}, hasSelectedAICount={hasSelectedAICount}, hasSelectedBoardSize={hasSelectedBoardSize}");
        
        ShowStep(MenuStep.PlayerCount);
    }
    
    /// <summary>
    /// Debug UI state for troubleshooting
    /// </summary>
    private void DebugUIState()
    {
        Debug.Log("🔍 === UI STATE DEBUG ===");
        
        // Check Canvas
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log($"📋 Canvas found: {canvas.gameObject.name}, enabled={canvas.enabled}");
            Debug.Log($"📋 GraphicRaycaster: {raycaster != null}, enabled={raycaster?.enabled}");
        }
        else
        {
            Debug.LogError("❌ No Canvas found!");
        }
        
        // Check EventSystem
        var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null)
        {
            var inputModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log($"🎮 EventSystem found: enabled={eventSystem.enabled}, current={UnityEngine.EventSystems.EventSystem.current != null}");
            Debug.Log($"🎮 Input Module: {inputModule != null}, enabled={inputModule?.enabled}");
        }
        else
        {
            Debug.LogError("❌ No EventSystem found!");
        }
        
        // Check New Game Button specifically
        if (newGameButton != null)
        {
            Debug.Log($"🔘 New Game Button: interactable={newGameButton.interactable}, active={newGameButton.gameObject.activeInHierarchy}");
            var image = newGameButton.GetComponent<UnityEngine.UI.Image>();
            Debug.Log($"🔘 Button Image: raycastTarget={image?.raycastTarget}");
        }
        else
        {
            Debug.LogError("❌ New Game Button is null!");
        }
        
        Debug.Log("🔍 === END UI DEBUG ===");
    }
    
    // === SIDE SELECTION METHODS ===
    
    /// <summary>
    /// Update the side selection UI based on current configuration
    /// </summary>
    private void UpdateSideSelectionUI()
    {
        if (sideSelectionPanel == null) return;
        
        // Determine if this is a simple 2-player Human vs AI scenario
        bool isSimple2PlayerHumanVsAI = (currentConfig.playerCount == 2 && currentConfig.aiPlayerCount == 1);
        
        // Find UI elements
        Transform instructionsText = sideSelectionPanel.transform.Find("SideSelectionInstructions");
        Transform playAsWhiteButton = sideSelectionPanel.transform.Find("PlayAsWhiteButton");
        Transform playAsBlackButton = sideSelectionPanel.transform.Find("PlayAsBlackButton");
        Transform choosePositionButton = sideSelectionPanel.transform.Find("ChoosePositionButton");
        Transform randomAssignButton = sideSelectionPanel.transform.Find("RandomAssignButton");
        Transform assignmentDetails = sideSelectionPanel.transform.Find("AssignmentDetails");
        
        if (isSimple2PlayerHumanVsAI)
        {
            // === SIMPLE 2-PLAYER HUMAN VS AI UI ===
            
            // Update instructions
            if (instructionsText?.GetComponent<TextMeshProUGUI>() != null)
            {
                instructionsText.GetComponent<TextMeshProUGUI>().text = "Choose which side you want to play as:";
            }
            
            // Show simple White/Black choice buttons
            if (playAsWhiteButton != null) playAsWhiteButton.gameObject.SetActive(true);
            if (playAsBlackButton != null) playAsBlackButton.gameObject.SetActive(true);
            
            // Hide complex multi-player UI
            if (choosePositionButton != null) choosePositionButton.gameObject.SetActive(false);
            if (randomAssignButton != null) randomAssignButton.gameObject.SetActive(false);
            
            // Show current selection status
            if (assignmentDetails?.GetComponent<TextMeshProUGUI>() != null)
            {
                string selectionStatus;
                if (currentConfig.whitePlayerType == PlayerType.Human && currentConfig.blackPlayerType == PlayerType.Computer)
                {
                    selectionStatus = "✅ You selected: White (Human vs AI)";
                }
                else if (currentConfig.whitePlayerType == PlayerType.Computer && currentConfig.blackPlayerType == PlayerType.Human)
                {
                    selectionStatus = "✅ You selected: Black (AI vs Human)";
                }
                else
                {
                    selectionStatus = "⚠️ Please select White or Black";
                }
                assignmentDetails.GetComponent<TextMeshProUGUI>().text = selectionStatus;
            }
        }
        else
        {
            // === COMPLEX MULTI-PLAYER UI ===
            
            // Update instructions for multi-player
            if (instructionsText?.GetComponent<TextMeshProUGUI>() != null)
            {
                instructionsText.GetComponent<TextMeshProUGUI>().text = "Select how to assign human and AI players:";
            }
            
            // Hide simple White/Black choice buttons
            if (playAsWhiteButton != null) playAsWhiteButton.gameObject.SetActive(false);
            if (playAsBlackButton != null) playAsBlackButton.gameObject.SetActive(false);
            
            // Show complex multi-player UI
            if (choosePositionButton != null) choosePositionButton.gameObject.SetActive(true);
            if (randomAssignButton != null) randomAssignButton.gameObject.SetActive(true);
            
            // Show detailed assignment info for multi-player
            if (assignmentDetails?.GetComponent<TextMeshProUGUI>() != null)
            {
                // Initialize player types if needed
                if (currentConfig.playerTypes.Count != currentConfig.playerCount)
                {
                    currentConfig.InitializePlayerTypes();
                }
                
                // Build assignment description
                string description = $"{currentConfig.playerCount} Players Total, {currentConfig.aiPlayerCount} AI:\n";
                
                for (int i = 0; i < currentConfig.playerTypes.Count; i++)
                {
                    string playerName = GetPlayerNameForPosition(i);
                    string playerType = currentConfig.playerTypes[i] == PlayerType.Human ? "Human" : "AI";
                    description += $"Player {i + 1} ({playerName}): {playerType}\n";
                }
                
                assignmentDetails.GetComponent<TextMeshProUGUI>().text = description;
            }
        }
    }
    
    /// <summary>
    /// Get player name for position (White, Black for 2 players; Player 1, 2, 3, 4 for 4 players, etc.)
    /// </summary>
    private string GetPlayerNameForPosition(int position)
    {
        if (currentConfig.playerCount == 2)
        {
            return position == 0 ? "White" : "Black";
        }
        else
        {
            return $"Player {position + 1}";
        }
    }
    
    /// <summary>
    /// Handle user choosing to play as White in a 2-player game
    /// </summary>
    private void OnPlayAsWhiteSelected()
    {
        Debug.Log("🎯 MainMenuController: User chose to play as White");
        
        if (currentConfig.playerCount == 2 && currentConfig.aiPlayerCount == 1)
        {
            currentConfig.whitePlayerType = PlayerType.Human;
            currentConfig.blackPlayerType = PlayerType.Computer;
            currentConfig.useRandomAssignment = false;
            
            Debug.Log("🎯 Set player types: Human (White) vs AI (Black)");
            UpdateSideSelectionUI();
        }
        else
        {
            Debug.LogWarning("OnPlayAsWhiteSelected called for non-2-player-1-AI scenario");
        }
    }
    
    /// <summary>
    /// Handle user choosing to play as Black in a 2-player game
    /// </summary>
    private void OnPlayAsBlackSelected()
    {
        Debug.Log("🎯 MainMenuController: User chose to play as Black");
        
        if (currentConfig.playerCount == 2 && currentConfig.aiPlayerCount == 1)
        {
            currentConfig.whitePlayerType = PlayerType.Computer;
            currentConfig.blackPlayerType = PlayerType.Human;
            currentConfig.useRandomAssignment = false;
            
            Debug.Log("🎯 Set player types: AI (White) vs Human (Black)");
            UpdateSideSelectionUI();
        }
        else
        {
            Debug.LogWarning("OnPlayAsBlackSelected called for non-2-player-1-AI scenario");
        }
    }
    
    /// <summary>
    /// Handle manual position selection (allow user to choose their position)
    /// </summary>
    private void OnManualPositionSelection()
    {
        Debug.Log("MainMenuController: Manual position selection chosen");
        currentConfig.useRandomAssignment = false;
        
        // For 2 players, create a simple position selection dialog
        if (currentConfig.playerCount == 2 && currentConfig.aiPlayerCount == 1)
        {
            // Show position selection buttons for 2-player game
            ShowTwoPlayerPositionSelection();
        }
        else
        {
            // For future 4/6 player games, this would show a more complex UI
            Debug.Log("Manual position selection for >2 players not yet implemented");
            // For now, default to human at position 0
            SetHumanPlayerPosition(0);
        }
        
        UpdateSideSelectionUI();
    }
    
    /// <summary>
    /// Show position selection for 2-player games (White vs Black)
    /// </summary>
    private void ShowTwoPlayerPositionSelection()
    {
        // Create temporary position selection buttons
        GameObject positionPanel = new GameObject("PositionSelection");
        positionPanel.transform.SetParent(sideSelectionPanel.transform, false);
        RectTransform posRT = positionPanel.AddComponent<RectTransform>();
        posRT.anchorMin = Vector2.zero;
        posRT.anchorMax = Vector2.one;
        posRT.offsetMin = Vector2.zero;
        posRT.offsetMax = Vector2.zero;
        
        // White player button
        CreateUIButton("PlayAsWhiteButton", "Play as White", positionPanel, 
            new Vector2(0.1f, 0.2f), new Vector2(0.35f, 0.1f), () => {
                SetHumanPlayerPosition(0); // Position 0 = White
                DestroyImmediate(positionPanel);
            });
        
        // Black player button
        CreateUIButton("PlayAsBlackButton", "Play as Black", positionPanel, 
            new Vector2(0.55f, 0.2f), new Vector2(0.35f, 0.1f), () => {
                SetHumanPlayerPosition(1); // Position 1 = Black
                DestroyImmediate(positionPanel);
            });
    }
    
    /// <summary>
    /// Set human player at specific position and assign AI to remaining positions
    /// </summary>
    private void SetHumanPlayerPosition(int humanPosition)
    {
        // Initialize if needed
        if (currentConfig.playerTypes.Count != currentConfig.playerCount)
        {
            currentConfig.InitializePlayerTypes();
        }
        
        // Set all to AI first
        for (int i = 0; i < currentConfig.playerCount; i++)
        {
            currentConfig.SetPlayerTypeAtPosition(i, PlayerType.Computer);
        }
        
        // Set human at chosen position
        currentConfig.SetPlayerTypeAtPosition(humanPosition, PlayerType.Human);
        currentConfig.humanPlayerStartPosition = humanPosition;
        
        // Apply to legacy fields for compatibility
        currentConfig.ApplyToLegacyFields();
        
        Debug.Log($"MainMenuController: Human assigned to position {humanPosition}");
        UpdateSideSelectionUI();
    }
    
    /// <summary>
    /// Handle random assignment of player types
    /// </summary>
    private void OnRandomAssignment()
    {
        Debug.Log("MainMenuController: Random assignment chosen");
        currentConfig.useRandomAssignment = true;
        currentConfig.RandomlyAssignPlayers();
        currentConfig.ApplyToLegacyFields();
        UpdateSideSelectionUI();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (SceneController.Instance != null)
        {
            SceneController.Instance.OnSceneLoadStarted -= OnSceneLoadStarted;
            SceneController.Instance.OnSceneLoadCompleted -= OnSceneLoadCompleted;
        }
    }
}