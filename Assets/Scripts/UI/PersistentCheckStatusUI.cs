using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Enhanced status bar UI component that displays comprehensive game status
/// Shows check status, current turn, and game phase in a persistent top bar
/// Features: Turn indicators, check status icons, responsive design for mobile
/// Uses event-driven updates for instant response to all game state changes
/// </summary>
public class PersistentCheckStatusUI : MonoBehaviour
{
    [Header("Persistent Status Settings")]
    public bool enablePersistentStatus = true;
    public bool enableEventDrivenUpdates = true; // Use events instead of polling
    
    [Header("Visual Settings")]
    public int fontSize = 18;
    public Color safeColor = Color.green;
    public Color checkColor = Color.red;
    public Color currentTurnColor = new Color(0.3f, 0.6f, 1f, 1f); // Blue for current turn
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.8f);
    
    [Header("Status Icons")]
    public string safeIcon = "✓";
    public string checkIcon = "⚠️";
    public string checkmateIcon = "💀";
    public string turnIcon = "▶";
    
    // UI Components
    private Canvas statusCanvas;
    private GameObject statusPanel;
    private Text statusText;
    private Image backgroundImage;
    
    // New Game Button Components
    private GameObject newGameButton;
    private UnityEngine.UI.Button newGameButtonComponent;
    private UnityEngine.UI.Image newGameButtonImage;
    private TMPro.TextMeshProUGUI newGameButtonText;
    
    // Status tracking
    private bool whiteInCheck = false;
    private bool blackInCheck = false;
    private bool lastWhiteStatus = false;
    private bool lastBlackStatus = false;
    
    // Turn tracking
    private PieceColor currentTurn = PieceColor.White;
    private GameState currentGameState = GameState.PiecePlacement;
    
    // Game end tracking
    private bool gameEnded = false;
    private string gameEndMessage = "";
    
    // Chaos mode tracking
    private bool chaosEventActive = false;
    private string chaosMessage = "";
    private float chaosMessageTimestamp = 0f;
    private const float chaosMessageDuration = 3f;
    
    // Update coroutine
    private Coroutine statusUpdateCoroutine;
    
    public static PersistentCheckStatusUI Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("PersistentCheckStatusUI: Instance created");
            CreatePersistentStatusUI();
        }
        else
        {
            Debug.LogWarning("PersistentCheckStatusUI: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (enablePersistentStatus)
        {
            if (enableEventDrivenUpdates)
            {
                StartEventDrivenMonitoring();
            }
            else
            {
                StartStatusMonitoring();
            }
        }
    }
    
    private void OnDestroy()
    {
        // Clean up polling coroutine if used
        if (statusUpdateCoroutine != null)
        {
            StopCoroutine(statusUpdateCoroutine);
        }
        
        // Clean up event subscriptions
        if (enableEventDrivenUpdates && CheckDetectionManager.Instance != null)
        {
            CheckDetectionManager.Instance.OnKingInCheck -= OnKingInCheckEvent;
            CheckDetectionManager.Instance.OnCheckResolved -= OnCheckResolvedEvent;
        }
        
        // Clean up game end event subscriptions
        if (GameEndDetectionManager.Instance != null)
        {
            GameEndDetectionManager.Instance.OnCheckmate -= OnCheckmateEvent;
            GameEndDetectionManager.Instance.OnStalemate -= OnStalemateEvent;
            GameEndDetectionManager.Instance.OnDrawAccepted -= OnDrawAcceptedEvent;
            GameEndDetectionManager.Instance.OnForfeit -= OnForfeitEvent;
        }
        
        // Clean up turn manager event subscriptions
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged -= OnTurnChangedEvent;
        }
        
        // Clean up game state manager event subscriptions
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChangedEvent;
        }
        
        // Clean up chaos mode event subscriptions
        if (ChaosRotationManager.Instance != null)
        {
            ChaosRotationManager.Instance.OnChaosEvent -= OnChaosEvent;
        }
    }
    
    /// <summary>
    /// Create the persistent status UI elements
    /// </summary>
    private void CreatePersistentStatusUI()
    {
        Debug.Log("PersistentCheckStatusUI: Creating persistent status UI");
        
        // Create canvas for status display
        GameObject canvasObject = new GameObject("PersistentCheckStatusCanvas");
        canvasObject.transform.SetParent(transform);
        
        statusCanvas = canvasObject.AddComponent<Canvas>();
        statusCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        statusCanvas.sortingOrder = 110; // Higher than other UI elements
        
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(375f, 812f); // iPhone X reference
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObject.AddComponent<GraphicRaycaster>();
        
        // Create background panel
        statusPanel = new GameObject("StatusPanel");
        statusPanel.transform.SetParent(canvasObject.transform);
        
        backgroundImage = statusPanel.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        
        RectTransform panelRect = statusPanel.GetComponent<RectTransform>();
        
        // Position at top-center of screen as full-width status bar
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -35f); // 35 pixels from top
        panelRect.sizeDelta = new Vector2(0f, 70f); // Full width, taller for better visibility
        
        // Create status text
        GameObject textObject = new GameObject("StatusText");
        textObject.transform.SetParent(statusPanel.transform);
        
        statusText = textObject.AddComponent<Text>();
        statusText.text = "✓ White: Safe | Turn: White ▶ | Black: Safe ✓";
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = fontSize;
        statusText.fontStyle = FontStyle.Bold;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        Debug.Log("PersistentCheckStatusUI: UI creation complete");
        
        // Create the new game button (initially hidden)
        CreateNewGameButton();
    }
    
    /// <summary>
    /// Create the new game button that appears on game over
    /// </summary>
    private void CreateNewGameButton()
    {
        Debug.Log("PersistentCheckStatusUI: Creating new game button");
        
        // Create the button GameObject
        newGameButton = new GameObject("NewGameButton");
        newGameButton.transform.SetParent(statusCanvas.transform, false);
        
        // Add UI components
        newGameButtonComponent = newGameButton.AddComponent<UnityEngine.UI.Button>();
        newGameButtonImage = newGameButton.AddComponent<UnityEngine.UI.Image>();
        
        // Position the button on the left side of the screen to mirror menu button
        RectTransform buttonRect = newGameButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 1);
        buttonRect.anchorMax = new Vector2(0, 1);
        buttonRect.anchoredPosition = new Vector2(70, -100); // Left side, same vertical position as menu button
        buttonRect.sizeDelta = new Vector2(120, 50); // Same size as menu button
        
        // Create button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(newGameButton.transform, false);
        
        newGameButtonText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        newGameButtonText.text = "New Game";
        newGameButtonText.fontSize = 18;
        newGameButtonText.color = Color.white;
        newGameButtonText.alignment = TMPro.TextAlignmentOptions.Center;
        
        // Position the text
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Style the button with a green tint to distinguish from menu button
        newGameButtonImage.color = new Color(0.1f, 0.4f, 0.1f, 0.9f); // Dark green
        
        // Add button states for better feedback
        UnityEngine.UI.ColorBlock colors = newGameButtonComponent.colors;
        colors.normalColor = new Color(0.1f, 0.4f, 0.1f, 0.9f); // Dark green
        colors.highlightedColor = new Color(0.2f, 0.6f, 0.2f, 1f); // Lighter green on hover
        colors.pressedColor = new Color(0.05f, 0.3f, 0.05f, 1f); // Darker green when pressed
        colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        newGameButtonComponent.colors = colors;
        
        // Add click listener
        newGameButtonComponent.onClick.AddListener(() => {
            Debug.Log("PersistentCheckStatusUI: New Game button clicked");
            HandleNewGame();
        });
        
        // Initially hide the button
        newGameButton.SetActive(false);
        
        Debug.Log("PersistentCheckStatusUI: New Game button created and hidden");
    }
    
    /// <summary>
    /// Show the new game button when game ends
    /// </summary>
    private void ShowNewGameButton()
    {
        if (newGameButton != null)
        {
            newGameButton.SetActive(true);
            Debug.Log("PersistentCheckStatusUI: New Game button shown");
        }
        else
        {
            Debug.LogWarning("PersistentCheckStatusUI: Cannot show New Game button - button is null");
        }
    }
    
    /// <summary>
    /// Hide the new game button when starting a new game
    /// </summary>
    private void HideNewGameButton()
    {
        if (newGameButton != null)
        {
            newGameButton.SetActive(false);
            Debug.Log("PersistentCheckStatusUI: New Game button hidden");
        }
        else
        {
            Debug.LogWarning("PersistentCheckStatusUI: Cannot hide New Game button - button is null");
        }
    }
    
    /// <summary>
    /// Handle new game button click - return to main menu for clean restart
    /// </summary>
    private void HandleNewGame()
    {
        Debug.Log("PersistentCheckStatusUI: Handling new game request");
        
        // Hide the button immediately to prevent multiple clicks
        HideNewGameButton();
        
        // Use SceneController to return to main menu for clean restart
        if (SceneController.Instance != null)
        {
            Debug.Log("PersistentCheckStatusUI: Loading main menu for new game");
            SceneController.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("PersistentCheckStatusUI: Cannot start new game - SceneController.Instance not available");
            // Show the button again if we failed to start new game
            ShowNewGameButton();
        }
    }
    
    /// <summary>
    /// Start event-driven status monitoring (recommended)
    /// </summary>
    private void StartEventDrivenMonitoring()
    {
        Debug.Log("PersistentCheckStatusUI: Starting event-driven status monitoring");
        
        // Wait for CheckDetectionManager to be available
        StartCoroutine(SubscribeToEventsWithRetry());
        
        // Set initial status
        UpdateInitialStatus();
    }
    
    /// <summary>
    /// Start continuous status monitoring (legacy fallback)
    /// </summary>
    private void StartStatusMonitoring()
    {
        Debug.Log("PersistentCheckStatusUI: Starting continuous status monitoring");
        
        if (statusUpdateCoroutine != null)
        {
            StopCoroutine(statusUpdateCoroutine);
        }
        
        statusUpdateCoroutine = StartCoroutine(ContinuousStatusUpdate());
    }
    
    /// <summary>
    /// Subscribe to CheckDetectionManager events with retry mechanism
    /// </summary>
    private System.Collections.IEnumerator SubscribeToEventsWithRetry()
    {
        int retryCount = 0;
        const int maxRetries = 10;
        const float retryDelay = 0.5f;
        
        while (retryCount < maxRetries)
        {
            if (CheckDetectionManager.Instance != null)
            {
                try
                {
                    CheckDetectionManager.Instance.OnKingInCheck += OnKingInCheckEvent;
                    CheckDetectionManager.Instance.OnCheckResolved += OnCheckResolvedEvent;
                    Debug.Log($"PersistentCheckStatusUI: Successfully subscribed to check detection events (attempt {retryCount + 1})");
                    
                    // Also subscribe to game end events from GameEndDetectionManager
                    if (GameEndDetectionManager.Instance != null)
                    {
                        GameEndDetectionManager.Instance.OnCheckmate += OnCheckmateEvent;
                        GameEndDetectionManager.Instance.OnStalemate += OnStalemateEvent;
                        GameEndDetectionManager.Instance.OnDrawAccepted += OnDrawAcceptedEvent;
                        GameEndDetectionManager.Instance.OnForfeit += OnForfeitEvent;
                        Debug.Log("PersistentCheckStatusUI: Successfully subscribed to game end and draw/forfeit events");
                    }
                    
                    // Subscribe to turn manager events for current player tracking
                    if (TurnManager.Instance != null)
                    {
                        TurnManager.Instance.OnTurnChanged += OnTurnChangedEvent;
                        currentTurn = TurnManager.Instance.GetCurrentPlayer();
                        Debug.Log($"PersistentCheckStatusUI: Successfully subscribed to turn events, current turn: {currentTurn}");
                    }
                    
                    // Subscribe to game state manager events for phase tracking
                    if (GameStateManager.Instance != null)
                    {
                        GameStateManager.Instance.OnStateChanged += OnGameStateChangedEvent;
                        currentGameState = GameStateManager.Instance.currentState;
                        Debug.Log($"PersistentCheckStatusUI: Successfully subscribed to game state events, current state: {currentGameState}");
                    }
                    
                    // Subscribe to chaos mode events for visual feedback
                    if (ChaosRotationManager.Instance != null)
                    {
                        ChaosRotationManager.Instance.OnChaosEvent += OnChaosEvent;
                        Debug.Log("PersistentCheckStatusUI: Successfully subscribed to chaos events");
                    }
                    
                    // Validate subscription
                    int kingInCheckHandlers = CheckDetectionManager.Instance.OnKingInCheck?.GetInvocationList()?.Length ?? 0;
                    Debug.Log($"PersistentCheckStatusUI: OnKingInCheck now has {kingInCheckHandlers} handlers");
                    
                    yield break; // Success, exit retry loop
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"PersistentCheckStatusUI: Exception subscribing to events: {e.Message}");
                }
            }
            
            retryCount++;
            Debug.LogWarning($"PersistentCheckStatusUI: CheckDetectionManager not ready, retry {retryCount}/{maxRetries}");
            yield return new WaitForSeconds(retryDelay);
        }
        
        Debug.LogError("PersistentCheckStatusUI: Failed to subscribe to check detection events after maximum retries");
    }
    
    /// <summary>
    /// Set initial status display
    /// </summary>
    private void UpdateInitialStatus()
    {
        Debug.Log("PersistentCheckStatusUI: Setting initial status display");
        
        if (CheckDetectionManager.Instance != null && 
            GameStateManager.Instance != null && 
            GameStateManager.Instance.CanMovePieces())
        {
            // Check actual status during gameplay
            bool currentWhiteStatus = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.White);
            bool currentBlackStatus = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.Black);
            
            whiteInCheck = currentWhiteStatus;
            blackInCheck = currentBlackStatus;
            
            UpdateStatusDisplay();
            Debug.Log($"PersistentCheckStatusUI: Initial status set - White: {(whiteInCheck ? "CHECK" : "Safe")}, Black: {(blackInCheck ? "CHECK" : "Safe")}");
        }
        else
        {
            // During placement phase or when manager not ready, show safe
            whiteInCheck = false;
            blackInCheck = false;
            UpdateStatusDisplay();
            Debug.Log("PersistentCheckStatusUI: Initial status set to Safe for both players");
        }
    }
    
    /// <summary>
    /// Force synchronization with actual check detection state
    /// </summary>
    private void SynchronizeWithCheckDetection()
    {
        if (CheckDetectionManager.Instance == null || 
            GameStateManager.Instance == null || 
            !GameStateManager.Instance.CanMovePieces())
        {
            return;
        }
        
        // Get current actual check states
        bool actualWhiteInCheck = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.White);
        bool actualBlackInCheck = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.Black);
        
        // Update UI state to match actual detection
        if (whiteInCheck != actualWhiteInCheck || blackInCheck != actualBlackInCheck)
        {
            Debug.Log($"PersistentCheckStatusUI: Synchronizing state - UI had W:{whiteInCheck}/B:{blackInCheck}, Detection has W:{actualWhiteInCheck}/B:{actualBlackInCheck}");
            
            whiteInCheck = actualWhiteInCheck;
            blackInCheck = actualBlackInCheck;
            UpdateStatusDisplay();
            
            Debug.Log($"PersistentCheckStatusUI: Synchronized - White: {(whiteInCheck ? "CHECK" : "Safe")}, Black: {(blackInCheck ? "CHECK" : "Safe")}");
        }
    }
    
    /// <summary>
    /// Event handler for when a king is in check
    /// </summary>
    private void OnKingInCheckEvent(PieceColor kingColor)
    {
        Debug.Log($"🚨 PersistentCheckStatusUI.OnKingInCheckEvent: {kingColor} king is in check!");
        
        if (kingColor == PieceColor.White)
        {
            whiteInCheck = true;
        }
        else
        {
            blackInCheck = true;
        }
        
        UpdateStatusDisplay();
        Debug.Log($"PersistentCheckStatusUI: Event-driven update completed - White: {(whiteInCheck ? "CHECK" : "Safe")}, Black: {(blackInCheck ? "CHECK" : "Safe")}");
    }
    
    /// <summary>
    /// Event handler for when check is resolved
    /// </summary>
    private void OnCheckResolvedEvent(PieceColor kingColor)
    {
        Debug.Log($"✅ PersistentCheckStatusUI.OnCheckResolvedEvent: {kingColor} check resolved!");
        
        if (kingColor == PieceColor.White)
        {
            whiteInCheck = false;
        }
        else
        {
            blackInCheck = false;
        }
        
        UpdateStatusDisplay();
        Debug.Log($"PersistentCheckStatusUI: Event-driven update completed - White: {(whiteInCheck ? "CHECK" : "Safe")}, Black: {(blackInCheck ? "CHECK" : "Safe")}");
    }
    
    /// <summary>
    /// Event handler for checkmate
    /// </summary>
    private void OnCheckmateEvent(PieceColor checkmatedPlayer)
    {
        PieceColor winner = (checkmatedPlayer == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        gameEnded = true;
        gameEndMessage = $"CHECKMATE! {winner.ToString().ToUpper()} WINS!";
        
        Debug.Log($"🏁 PersistentCheckStatusUI.OnCheckmateEvent: {gameEndMessage}");
        UpdateStatusDisplay();
        ShowNewGameButton();
    }
    
    /// <summary>
    /// Event handler for stalemate
    /// </summary>
    private void OnStalemateEvent(PieceColor stalematedPlayer)
    {
        gameEnded = true;
        gameEndMessage = "STALEMATE! GAME IS A DRAW!";
        
        Debug.Log($"🏁 PersistentCheckStatusUI.OnStalemateEvent: {gameEndMessage}");
        UpdateStatusDisplay();
        ShowNewGameButton();
    }
    
    /// <summary>
    /// Event handler for draw accepted
    /// </summary>
    private void OnDrawAcceptedEvent(PieceColor acceptingPlayer)
    {
        gameEnded = true;
        gameEndMessage = "⚖️ DRAW ACCEPTED! GAME IS A DRAW!";
        
        Debug.Log($"🏁 PersistentCheckStatusUI.OnDrawAcceptedEvent: {gameEndMessage}");
        UpdateStatusDisplay();
        ShowNewGameButton();
    }
    
    /// <summary>
    /// Event handler for forfeit
    /// </summary>
    private void OnForfeitEvent(PieceColor forfeitingPlayer)
    {
        PieceColor winner = (forfeitingPlayer == PieceColor.White) ? PieceColor.Black : PieceColor.White;
        gameEnded = true;
        gameEndMessage = $"🏆 {winner.ToString().ToUpper()} WINS! ({forfeitingPlayer} Forfeited)";
        
        Debug.Log($"🏁 PersistentCheckStatusUI.OnForfeitEvent: {gameEndMessage}");
        UpdateStatusDisplay();
        ShowNewGameButton();
    }
    
    /// <summary>
    /// Event handler for turn changes
    /// </summary>
    private void OnTurnChangedEvent(PieceColor newCurrentPlayer)
    {
        Debug.Log($"🔄 PersistentCheckStatusUI.OnTurnChangedEvent: Turn changed to {newCurrentPlayer}");
        
        currentTurn = newCurrentPlayer;
        UpdateStatusDisplay();
    }
    
    /// <summary>
    /// Event handler for game state changes
    /// </summary>
    private void OnGameStateChangedEvent(GameState newState)
    {
        Debug.Log($"🎮 PersistentCheckStatusUI.OnGameStateChangedEvent: Game state changed to {newState}");
        
        currentGameState = newState;
        
        // Reset game end status when transitioning to new game
        if (newState == GameState.PiecePlacement || newState == GameState.Playing)
        {
            gameEnded = false;
            gameEndMessage = "";
            HideNewGameButton();
        }
        
        UpdateStatusDisplay();
    }
    
    /// <summary>
    /// Event handler for chaos mode events
    /// </summary>
    private void OnChaosEvent(string message)
    {
        Debug.Log($"🌪️ PersistentCheckStatusUI.OnChaosEvent: {message}");
        
        chaosEventActive = true;
        chaosMessage = message;
        chaosMessageTimestamp = Time.time;
        
        UpdateStatusDisplay();
    }
    
    /// <summary>
    /// Stop status monitoring
    /// </summary>
    private void StopStatusMonitoring()
    {
        Debug.Log("PersistentCheckStatusUI: Stopping status monitoring");
        
        if (statusUpdateCoroutine != null)
        {
            StopCoroutine(statusUpdateCoroutine);
            statusUpdateCoroutine = null;
        }
    }
    
    /// <summary>
    /// Continuous status monitoring coroutine
    /// </summary>
    private System.Collections.IEnumerator ContinuousStatusUpdate()
    {
        while (enablePersistentStatus)
        {
            // Wait for CheckDetectionManager to be available
            if (CheckDetectionManager.Instance != null && 
                GameStateManager.Instance != null && 
                GameStateManager.Instance.CanMovePieces())
            {
                // Get current check status for both players
                bool currentWhiteStatus = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.White);
                bool currentBlackStatus = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.Black);
                
                // Update status if changed
                if (currentWhiteStatus != lastWhiteStatus || currentBlackStatus != lastBlackStatus)
                {
                    whiteInCheck = currentWhiteStatus;
                    blackInCheck = currentBlackStatus;
                    
                    UpdateStatusDisplay();
                    
                    lastWhiteStatus = currentWhiteStatus;
                    lastBlackStatus = currentBlackStatus;
                    
                    Debug.Log($"PersistentCheckStatusUI: Status updated - White: {(whiteInCheck ? "CHECK" : "Safe")}, Black: {(blackInCheck ? "CHECK" : "Safe")}");
                }
            }
            else if (GameStateManager.Instance != null && GameStateManager.Instance.CanPlacePieces())
            {
                // During placement phase, show "Safe" for both players
                if (whiteInCheck || blackInCheck)
                {
                    whiteInCheck = false;
                    blackInCheck = false;
                    UpdateStatusDisplay();
                    Debug.Log("PersistentCheckStatusUI: Placement phase - both players safe");
                }
            }
            
            yield return new WaitForSeconds(1.0f); // Fallback polling interval
        }
        
        Debug.Log("PersistentCheckStatusUI: Status monitoring stopped");
    }
    
    /// <summary>
    /// Update the enhanced status display with check states, turn indicator, and game phase
    /// </summary>
    private void UpdateStatusDisplay()
    {
        if (statusText == null) return;
        
        string statusMessage;
        
        // Check if chaos message should timeout
        if (chaosEventActive && Time.time - chaosMessageTimestamp > chaosMessageDuration)
        {
            chaosEventActive = false;
            chaosMessage = "";
        }
        
        // Show chaos message if active (highest priority)
        if (chaosEventActive)
        {
            statusMessage = chaosMessage;
            statusText.text = statusMessage;
            
            // Orange background for chaos events
            backgroundImage.color = new Color(1f, 0.5f, 0f, 0.9f); // Bright orange
        }
        // Show game end message if game is over
        else if (gameEnded)
        {
            statusMessage = gameEndMessage;
            statusText.text = statusMessage;
            
            // Special background for game end
            if (gameEndMessage.Contains("CHECKMATE"))
            {
                backgroundImage.color = new Color(0.9f, 0.1f, 0.1f, 0.9f); // Bright red for checkmate
            }
            else if (gameEndMessage.Contains("STALEMATE") || gameEndMessage.Contains("DRAW"))
            {
                backgroundImage.color = new Color(0.9f, 0.9f, 0.1f, 0.9f); // Yellow for stalemate/draw
            }
            else if (gameEndMessage.Contains("WINS"))
            {
                backgroundImage.color = new Color(0.1f, 0.8f, 0.1f, 0.9f); // Green for forfeit win
            }
            
            // Add pulsing effect for game end
            StartCoroutine(PulseEffect());
        }
        else
        {
            // Enhanced status bar format with icons and turn indicator
            string whiteIcon = whiteInCheck ? checkIcon : safeIcon;
            string blackIcon = blackInCheck ? checkIcon : safeIcon;
            string whiteStatus = whiteInCheck ? "CHECK!" : "Safe";
            string blackStatus = blackInCheck ? "CHECK!" : "Safe";
            
            // Add turn indicator highlighting
            string turnIndicator = "";
            if (currentGameState == GameState.Playing)
            {
                turnIndicator = $"Turn: {currentTurn} {turnIcon}";
            }
            else if (currentGameState == GameState.PiecePlacement)
            {
                turnIndicator = "Placement Phase";
            }
            else
            {
                turnIndicator = $"State: {currentGameState}";
            }
            
            // Build enhanced status message
            statusMessage = $"{whiteIcon} White: {whiteStatus} | {turnIndicator} | Black: {blackStatus} {blackIcon}";
            statusText.text = statusMessage;
            
            // Update background color based on game state and check status
            if (whiteInCheck || blackInCheck)
            {
                // Red background if anyone is in check
                backgroundImage.color = new Color(0.8f, 0.2f, 0.2f, 0.85f);
                
                // Add pulsing effect for check status
                StartCoroutine(PulseEffect());
            }
            else if (currentGameState == GameState.PiecePlacement)
            {
                // Blue-tinted background for placement phase
                backgroundImage.color = new Color(0.2f, 0.3f, 0.6f, 0.8f);
            }
            else
            {
                // Default dark background for normal gameplay
                backgroundImage.color = backgroundColor;
            }
        }
        
        Debug.Log($"PersistentCheckStatusUI: Enhanced display updated - {statusMessage}");
    }
    
    /// <summary>
    /// Pulse effect for check status
    /// </summary>
    private System.Collections.IEnumerator PulseEffect()
    {
        if (!whiteInCheck && !blackInCheck && !gameEnded) yield break;
        
        float pulseSpeed = 2f;
        float elapsed = 0f;
        
        while ((whiteInCheck || blackInCheck || gameEnded) && elapsed < 2f) // Pulse for 2 seconds max
        {
            float pulse = (Mathf.Sin(elapsed * pulseSpeed) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.6f, 0.9f, pulse);
            
            if (backgroundImage != null)
            {
                Color currentColor = backgroundImage.color;
                currentColor.a = alpha;
                backgroundImage.color = currentColor;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset to normal alpha
        if (backgroundImage != null && (whiteInCheck || blackInCheck || gameEnded))
        {
            Color currentColor = backgroundImage.color;
            currentColor.a = 0.8f;
            backgroundImage.color = currentColor;
        }
    }
    
    /// <summary>
    /// Manually set check status (for testing or external control)
    /// </summary>
    public void SetCheckStatus(bool whiteCheck, bool blackCheck)
    {
        Debug.Log($"PersistentCheckStatusUI.SetCheckStatus: White={whiteCheck}, Black={blackCheck}");
        
        whiteInCheck = whiteCheck;
        blackInCheck = blackCheck;
        UpdateStatusDisplay();
    }
    
    /// <summary>
    /// Manually set turn status (for testing or external control)
    /// </summary>
    public void SetCurrentTurn(PieceColor turn)
    {
        Debug.Log($"PersistentCheckStatusUI.SetCurrentTurn: {turn}");
        
        currentTurn = turn;
        UpdateStatusDisplay();
    }
    
    /// <summary>
    /// Enable or disable the persistent status display
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        enablePersistentStatus = enabled;
        
        if (statusPanel != null)
        {
            statusPanel.SetActive(enabled);
        }
        
        if (enabled)
        {
            if (enableEventDrivenUpdates)
            {
                StartEventDrivenMonitoring();
            }
            else
            {
                StartStatusMonitoring();
            }
        }
        else
        {
            StopStatusMonitoring();
            
            // Clean up event subscriptions
            if (enableEventDrivenUpdates && CheckDetectionManager.Instance != null)
            {
                CheckDetectionManager.Instance.OnKingInCheck -= OnKingInCheckEvent;
                CheckDetectionManager.Instance.OnCheckResolved -= OnCheckResolvedEvent;
            }
        }
        
        Debug.Log($"PersistentCheckStatusUI: Set enabled = {enabled} (event-driven = {enableEventDrivenUpdates})");
    }
    
    /// <summary>
    /// Get current check status
    /// </summary>
    public (bool whiteInCheck, bool blackInCheck) GetCurrentStatus()
    {
        return (whiteInCheck, blackInCheck);
    }
    
    /// <summary>
    /// Force an immediate status update with direct synchronization
    /// </summary>
    public void ForceUpdate()
    {
        if (CheckDetectionManager.Instance != null && 
            GameStateManager.Instance != null && 
            GameStateManager.Instance.CanMovePieces())
        {
            bool currentWhiteStatus = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.White);
            bool currentBlackStatus = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.Black);
            
            // Direct synchronization - bypass event system
            bool stateChanged = (whiteInCheck != currentWhiteStatus) || (blackInCheck != currentBlackStatus);
            
            if (stateChanged)
            {
                Debug.Log($"PersistentCheckStatusUI.ForceUpdate: Direct sync - UI had W:{whiteInCheck}/B:{blackInCheck}, Detection has W:{currentWhiteStatus}/B:{currentBlackStatus}");
                
                whiteInCheck = currentWhiteStatus;
                blackInCheck = currentBlackStatus;
                UpdateStatusDisplay();
                
                Debug.Log($"PersistentCheckStatusUI.ForceUpdate: Synchronized - White: {(whiteInCheck ? "CHECK" : "Safe")}, Black: {(blackInCheck ? "CHECK" : "Safe")}");
            }
            else
            {
                Debug.Log($"PersistentCheckStatusUI.ForceUpdate: No changes needed - White={whiteInCheck}, Black={blackInCheck}");
            }
        }
        else
        {
            Debug.Log("PersistentCheckStatusUI.ForceUpdate: Cannot update - managers not ready or not in playing state");
        }
    }
    
    /// <summary>
    /// Test method for manual verification
    /// </summary>
    [ContextMenu("Test Check Status")]
    public void TestCheckStatus()
    {
        Debug.Log("PersistentCheckStatusUI: Testing check status display");
        
        // Test sequence: Safe -> White Check -> Both Check -> Black Check -> Safe
        StartCoroutine(TestSequence());
    }
    
    private System.Collections.IEnumerator TestSequence()
    {
        Debug.Log("PersistentCheckStatusUI: Starting enhanced status bar test sequence");
        
        // Test normal gameplay with turn changes
        currentGameState = GameState.Playing;
        
        SetCheckStatus(false, false);
        SetCurrentTurn(PieceColor.White);
        yield return new WaitForSeconds(2f);
        
        SetCheckStatus(true, false);  // White in check
        yield return new WaitForSeconds(2f);
        
        SetCurrentTurn(PieceColor.Black);  // Turn change
        yield return new WaitForSeconds(2f);
        
        SetCheckStatus(false, true);  // Black in check
        yield return new WaitForSeconds(2f);
        
        SetCheckStatus(true, true);   // Both in check
        yield return new WaitForSeconds(2f);
        
        SetCheckStatus(false, false); // Both safe
        yield return new WaitForSeconds(2f);
        
        // Test placement phase
        currentGameState = GameState.PiecePlacement;
        UpdateStatusDisplay();
        yield return new WaitForSeconds(2f);
        
        // Back to normal
        currentGameState = GameState.Playing;
        UpdateStatusDisplay();
        
        Debug.Log("PersistentCheckStatusUI: Enhanced test sequence complete");
    }
}