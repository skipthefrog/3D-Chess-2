using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component that provides forfeit and draw action buttons during gameplay.
/// Integrates with TurnManager and GameEndDetectionManager to handle player requests.
/// </summary>
public class GameActionsUI : MonoBehaviour
{
    [Header("UI References")]
    public Button forfeitButton;
    public Button drawOfferButton;
    public Button drawAcceptButton;
    public Button drawDeclineButton;
    
    [Header("UI Positioning")]
    public Canvas gameCanvas;
    public RectTransform buttonContainer;
    
    [Header("Button Styling")]
    public Color forfeitButtonColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
    public Color drawButtonColor = new Color(0.2f, 0.6f, 0.8f, 1f); // Blue
    public Color acceptButtonColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
    public Color declineButtonColor = new Color(0.8f, 0.4f, 0.2f, 1f); // Orange
    
    [Header("Settings")]
    public bool enableStandaloneButtons = false; // Disabled - integrated into GameMenuUI
    public bool enableKeyboardShortcuts = false; // Disabled - handled by InputManager
    public KeyCode forfeitKey = KeyCode.F;
    public KeyCode drawOfferKey = KeyCode.D;
    public KeyCode drawAcceptKey = KeyCode.Y;
    public KeyCode drawDeclineKey = KeyCode.N;
    
    public static GameActionsUI Instance { get; private set; }
    
    // UI state
    private bool buttonsCreated = false;
    private bool isVisible = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameActionsUI: Instance created");
        }
        else
        {
            Debug.LogWarning("GameActionsUI: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        Debug.Log("GameActionsUI: Starting initialization...");
        Debug.Log($"GameActionsUI: enableStandaloneButtons = {enableStandaloneButtons}");
        
        if (!enableStandaloneButtons)
        {
            Debug.Log("GameActionsUI: Standalone buttons disabled - functionality integrated into GameMenuUI");
            return;
        }
        
        // Find or create canvas
        SetupCanvas();
        
        // Create UI buttons
        CreateGameActionButtons();
        
        // Debug manager availability
        Debug.Log($"GameActionsUI: GameStateManager.Instance = {(GameStateManager.Instance != null ? "FOUND" : "NULL")}");
        Debug.Log($"GameActionsUI: GameEndDetectionManager.Instance = {(GameEndDetectionManager.Instance != null ? "FOUND" : "NULL")}");
        Debug.Log($"GameActionsUI: TurnManager.Instance = {(TurnManager.Instance != null ? "FOUND" : "NULL")}");
        
        // Subscribe to game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
            Debug.Log("GameActionsUI: Subscribed to GameStateManager events");
        }
        else
        {
            Debug.LogError("GameActionsUI: GameStateManager.Instance is NULL - cannot subscribe to state changes");
        }
        
        // Subscribe to draw events
        if (GameEndDetectionManager.Instance != null)
        {
            GameEndDetectionManager.Instance.OnDrawOffered += OnDrawOffered;
            GameEndDetectionManager.Instance.OnDrawAccepted += OnDrawResolved;
            GameEndDetectionManager.Instance.OnDrawDeclined += OnDrawResolved;
            Debug.Log("GameActionsUI: Subscribed to GameEndDetectionManager events");
        }
        else
        {
            Debug.LogError("GameActionsUI: GameEndDetectionManager.Instance is NULL - draw functionality will not work");
        }
        
        // Subscribe to turn changes to update button availability
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged += OnTurnChanged;
            Debug.Log("GameActionsUI: Subscribed to TurnManager events");
        }
        else
        {
            Debug.LogError("GameActionsUI: TurnManager.Instance is NULL - turn-based button updates will not work");
        }
        
        // Initially hide buttons
        SetButtonsVisible(false);
        Debug.Log("GameActionsUI: Initialization complete - buttons initially hidden");
    }
    
    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
        
        if (GameEndDetectionManager.Instance != null)
        {
            GameEndDetectionManager.Instance.OnDrawOffered -= OnDrawOffered;
            GameEndDetectionManager.Instance.OnDrawAccepted -= OnDrawResolved;
            GameEndDetectionManager.Instance.OnDrawDeclined -= OnDrawResolved;
        }
        
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged -= OnTurnChanged;
        }
    }
    
    private void Update()
    {
        if (!enableKeyboardShortcuts || !enableStandaloneButtons || !isVisible) return;
        
        // Handle keyboard shortcuts
        if (Input.GetKeyDown(forfeitKey))
        {
            OnForfeitButtonClicked();
        }
        else if (Input.GetKeyDown(drawOfferKey))
        {
            OnDrawOfferButtonClicked();
        }
        else if (Input.GetKeyDown(drawAcceptKey))
        {
            OnDrawAcceptButtonClicked();
        }
        else if (Input.GetKeyDown(drawDeclineKey))
        {
            OnDrawDeclineButtonClicked();
        }
    }
    
    /// <summary>
    /// Set up the UI canvas for the game action buttons
    /// </summary>
    private void SetupCanvas()
    {
        // Find existing canvas or create one
        if (gameCanvas == null)
        {
            gameCanvas = FindFirstObjectByType<Canvas>();
        }
        
        if (gameCanvas == null)
        {
            // Create new canvas
            GameObject canvasObject = new GameObject("GameActionsCanvas");
            gameCanvas = canvasObject.AddComponent<Canvas>();
            gameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameCanvas.sortingOrder = 100; // High priority
            
            // Add CanvasScaler and GraphicRaycaster
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObject.AddComponent<GraphicRaycaster>();
            
            Debug.Log("GameActionsUI: Created new canvas for game actions");
        }
        
        // Create container for buttons
        if (buttonContainer == null)
        {
            GameObject containerObject = new GameObject("GameActionButtons");
            containerObject.transform.SetParent(gameCanvas.transform, false);
            
            buttonContainer = containerObject.AddComponent<RectTransform>();
            
            // Position in top-right corner
            buttonContainer.anchorMin = new Vector2(1f, 1f);
            buttonContainer.anchorMax = new Vector2(1f, 1f);
            buttonContainer.pivot = new Vector2(1f, 1f);
            buttonContainer.anchoredPosition = new Vector2(-20f, -20f); // Margin from edge
            buttonContainer.sizeDelta = new Vector2(200f, 300f); // Container size
        }
    }
    
    /// <summary>
    /// Create the game action buttons
    /// </summary>
    private void CreateGameActionButtons()
    {
        if (buttonsCreated) return;
        
        float buttonHeight = 50f;
        float buttonWidth = 180f;
        
        // Create forfeit button
        forfeitButton = CreateButton("Forfeit", 0, buttonWidth, buttonHeight, forfeitButtonColor);
        forfeitButton.onClick.AddListener(OnForfeitButtonClicked);
        
        // Create draw offer button
        drawOfferButton = CreateButton("Offer Draw", 1, buttonWidth, buttonHeight, drawButtonColor);
        drawOfferButton.onClick.AddListener(OnDrawOfferButtonClicked);
        
        // Create draw response buttons (initially hidden)
        drawAcceptButton = CreateButton("Accept Draw", 2, buttonWidth, buttonHeight, acceptButtonColor);
        drawAcceptButton.onClick.AddListener(OnDrawAcceptButtonClicked);
        drawAcceptButton.gameObject.SetActive(false);
        
        drawDeclineButton = CreateButton("Decline Draw", 3, buttonWidth, buttonHeight, declineButtonColor);
        drawDeclineButton.onClick.AddListener(OnDrawDeclineButtonClicked);
        drawDeclineButton.gameObject.SetActive(false);
        
        buttonsCreated = true;
        Debug.Log("GameActionsUI: Created forfeit and draw buttons");
    }
    
    /// <summary>
    /// Create a button with specified properties
    /// </summary>
    private Button CreateButton(string text, int index, float width, float height, Color color)
    {
        GameObject buttonObject = new GameObject($"Button_{text.Replace(" ", "")}");
        buttonObject.transform.SetParent(buttonContainer, false);
        
        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(width, height);
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(0f, -index * (height + 10f));
        
        // Add Button component
        Button button = buttonObject.AddComponent<Button>();
        
        // Add Image component for background
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = color;
        button.targetGraphic = buttonImage;
        
        // Create text child
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Text buttonText = textObject.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        return button;
    }
    
    /// <summary>
    /// Handle game state changes
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        bool shouldShowButtons = (newState == GameState.Playing);
        Debug.Log($"GameActionsUI: Game state changed to {newState}, shouldShowButtons = {shouldShowButtons}");
        SetButtonsVisible(shouldShowButtons);
        
        if (shouldShowButtons)
        {
            UpdateButtonStates();
        }
    }
    
    /// <summary>
    /// Handle turn changes
    /// </summary>
    private void OnTurnChanged(PieceColor previousPlayer, PieceColor newCurrentPlayer)
    {
        if (isVisible)
        {
            UpdateButtonStates();
        }
    }
    
    /// <summary>
    /// Handle draw offer events
    /// </summary>
    private void OnDrawOffered(PieceColor offeringPlayer)
    {
        UpdateButtonStates();
        Debug.Log($"GameActionsUI: Draw offered by {offeringPlayer}, updating UI");
    }
    
    /// <summary>
    /// Handle draw resolution (accepted or declined)
    /// </summary>
    private void OnDrawResolved(PieceColor player)
    {
        UpdateButtonStates();
        Debug.Log("GameActionsUI: Draw resolved, updating UI");
    }
    
    /// <summary>
    /// Set visibility of all game action buttons
    /// </summary>
    private void SetButtonsVisible(bool visible)
    {
        Debug.Log($"GameActionsUI: SetButtonsVisible({visible}) called");
        Debug.Log($"GameActionsUI: buttonContainer = {(buttonContainer != null ? "EXISTS" : "NULL")}");
        
        if (buttonContainer != null)
        {
            buttonContainer.gameObject.SetActive(visible);
            isVisible = visible;
            Debug.Log($"GameActionsUI: Button container set to {visible}, isVisible = {isVisible}");
            
            if (visible)
            {
                UpdateButtonStates();
            }
        }
        else
        {
            Debug.LogError("GameActionsUI: Cannot set button visibility - buttonContainer is NULL");
        }
    }
    
    /// <summary>
    /// Update button states based on current game conditions
    /// </summary>
    private void UpdateButtonStates()
    {
        if (!buttonsCreated || TurnManager.Instance == null) return;
        
        bool canForfeit = TurnManager.Instance.CanCurrentPlayerForfeit();
        bool canOfferDraw = TurnManager.Instance.CanCurrentPlayerOfferDraw();
        bool canRespondToDraw = TurnManager.Instance.CanCurrentPlayerRespondToDraw();
        bool isDrawPending = GameEndDetectionManager.Instance != null && 
                           GameEndDetectionManager.Instance.IsDrawOfferPending();
        
        // Update forfeit button
        if (forfeitButton != null)
        {
            forfeitButton.interactable = canForfeit;
            forfeitButton.gameObject.SetActive(!isDrawPending); // Hide during draw offer
        }
        
        // Update draw offer button
        if (drawOfferButton != null)
        {
            drawOfferButton.interactable = canOfferDraw;
            drawOfferButton.gameObject.SetActive(!isDrawPending); // Hide during draw offer
        }
        
        // Update draw response buttons
        if (drawAcceptButton != null)
        {
            drawAcceptButton.interactable = canRespondToDraw;
            drawAcceptButton.gameObject.SetActive(isDrawPending && canRespondToDraw);
        }
        
        if (drawDeclineButton != null)
        {
            drawDeclineButton.interactable = canRespondToDraw;
            drawDeclineButton.gameObject.SetActive(isDrawPending && canRespondToDraw);
        }
    }
    
    // ===== BUTTON EVENT HANDLERS =====
    
    /// <summary>
    /// Handle forfeit button click
    /// </summary>
    public void OnForfeitButtonClicked()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.CanCurrentPlayerForfeit())
        {
            Debug.Log("GameActionsUI: Forfeit button clicked");
            
            PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();
            
            // Show confirmation dialog
            if (ConfirmationDialog.Instance != null)
            {
                ConfirmationDialog.Instance.ShowForfeitDialog(currentPlayer, () => {
                    Debug.Log($"GameActionsUI: Forfeit confirmed for {currentPlayer}");
                    TurnManager.Instance.RequestForfeit();
                });
            }
            else
            {
                // Fallback: direct forfeit if no dialog system
                Debug.LogWarning("GameActionsUI: No confirmation dialog available, forfeiting directly");
                TurnManager.Instance.RequestForfeit();
            }
        }
        else
        {
            Debug.LogWarning("GameActionsUI: Forfeit button clicked but current player cannot forfeit");
        }
    }
    
    /// <summary>
    /// Handle draw offer button click
    /// </summary>
    public void OnDrawOfferButtonClicked()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.CanCurrentPlayerOfferDraw())
        {
            Debug.Log("GameActionsUI: Draw offer button clicked");
            
            PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();
            
            // Show confirmation dialog
            if (ConfirmationDialog.Instance != null)
            {
                ConfirmationDialog.Instance.ShowDrawOfferDialog(currentPlayer, () => {
                    Debug.Log($"GameActionsUI: Draw offer confirmed for {currentPlayer}");
                    TurnManager.Instance.RequestDrawOffer();
                });
            }
            else
            {
                // Fallback: direct draw offer if no dialog system
                Debug.LogWarning("GameActionsUI: No confirmation dialog available, offering draw directly");
                TurnManager.Instance.RequestDrawOffer();
            }
        }
        else
        {
            Debug.LogWarning("GameActionsUI: Draw offer button clicked but current player cannot offer draw");
        }
    }
    
    /// <summary>
    /// Handle draw accept button click
    /// </summary>
    public void OnDrawAcceptButtonClicked()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.CanCurrentPlayerRespondToDraw())
        {
            Debug.Log("GameActionsUI: Draw accept button clicked");
            TurnManager.Instance.RespondToDrawOffer(true);
        }
        else
        {
            Debug.LogWarning("GameActionsUI: Draw accept button clicked but current player cannot respond");
        }
    }
    
    /// <summary>
    /// Handle draw decline button click
    /// </summary>
    public void OnDrawDeclineButtonClicked()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.CanCurrentPlayerRespondToDraw())
        {
            Debug.Log("GameActionsUI: Draw decline button clicked");
            TurnManager.Instance.RespondToDrawOffer(false);
        }
        else
        {
            Debug.LogWarning("GameActionsUI: Draw decline button clicked but current player cannot respond");
        }
    }
    
    /// <summary>
    /// Get debug information about current button states
    /// </summary>
    public string GetDebugInfo()
    {
        if (!buttonsCreated || TurnManager.Instance == null)
        {
            return "GameActionsUI not initialized";
        }
        
        string info = $"GameActionsUI State:\n";
        info += $"  Visible: {isVisible}\n";
        info += $"  Can Forfeit: {TurnManager.Instance.CanCurrentPlayerForfeit()}\n";
        info += $"  Can Offer Draw: {TurnManager.Instance.CanCurrentPlayerOfferDraw()}\n";
        info += $"  Can Respond to Draw: {TurnManager.Instance.CanCurrentPlayerRespondToDraw()}\n";
        
        if (GameEndDetectionManager.Instance != null)
        {
            info += $"  Draw Pending: {GameEndDetectionManager.Instance.IsDrawOfferPending()}\n";
            if (GameEndDetectionManager.Instance.IsDrawOfferPending())
            {
                info += $"  Draw Offered By: {GameEndDetectionManager.Instance.GetDrawOfferingPlayer()}\n";
            }
        }
        
        return info;
    }
}