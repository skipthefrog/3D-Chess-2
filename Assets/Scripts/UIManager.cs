using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main UI coordinator for the 3D Chess game
/// Manages and initializes all UI components including check indicators
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Components")]
    public bool enableCheckIndicator = false; // Disabled - using persistent status instead
    public bool enablePersistentCheckStatus = true;
    public bool enableBoardRotationUI = true;
    public bool enablePlacementUI = true;
    public bool enableReturnToMenuButton = true;
    
    [Header("Draw Offer Indicator")]
    public bool enableDrawOfferIndicator = true;
    public float indicatorSize = 10f;
    public Color indicatorColor = new Color(1f, 0.4f, 0f, 1f); // Orange color
    public float pulseSpeed = 2f;
    
    // UI Component references
    private CheckIndicatorUI checkIndicator;
    private CheckStatusUI checkStatus;
    private PersistentCheckStatusUI persistentCheckStatus;
    private BoardRotationUI boardRotation;
    private PlacementUI placementUI;
    
    // Draw offer indicator
    private GameObject drawOfferIndicator;
    private UnityEngine.UI.Image indicatorImage;
    private Coroutine indicatorPulseCoroutine;
    
    public static UIManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("UIManager: Instance created");
            
            // Initialize UI components
            InitializeUIComponents();
        }
        else
        {
            Debug.LogWarning("UIManager: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Ensure all UI components are properly set up
        ValidateUIComponents();
        
        // Subscribe to draw offer events for indicator updates
        SubscribeToDrawOfferEvents();
    }
    
    /// <summary>
    /// Initialize all UI components
    /// </summary>
    private void InitializeUIComponents()
    {
        Debug.Log("UIManager: Initializing UI components");
        
        // Create Check Indicator UI if enabled
        if (enableCheckIndicator)
        {
            CreateCheckIndicatorUI();
        }
        
        // Create Persistent Check Status UI if enabled
        if (enablePersistentCheckStatus)
        {
            CreatePersistentCheckStatusUI();
        }
        
        // Find existing UI components
        FindExistingUIComponents();
        
        // Create Return to Menu button if enabled
        if (enableReturnToMenuButton)
        {
            CreateReturnToMenuButton();
        }
        
        Debug.Log("UIManager: UI components initialization complete");
    }
    
    /// <summary>
    /// Subscribe to draw offer events for automatic indicator updates
    /// </summary>
    private void SubscribeToDrawOfferEvents()
    {
        if (!enableDrawOfferIndicator) return;
        
        StartCoroutine(SubscribeToDrawOfferEventsWithRetry());
    }
    
    /// <summary>
    /// Subscribe to draw offer events with retry mechanism
    /// </summary>
    private System.Collections.IEnumerator SubscribeToDrawOfferEventsWithRetry()
    {
        int retryCount = 0;
        const int maxRetries = 10;
        const float retryDelay = 0.5f;
        
        while (retryCount < maxRetries)
        {
            if (GameEndDetectionManager.Instance != null)
            {
                try
                {
                    GameEndDetectionManager.Instance.OnDrawOffered += OnDrawOffered;
                    GameEndDetectionManager.Instance.OnDrawAccepted += OnDrawAccepted;
                    GameEndDetectionManager.Instance.OnDrawDeclined += OnDrawDeclined;
                    
                    Debug.Log("UIManager: Successfully subscribed to draw offer events");
                    
                    // Initial update of indicator state
                    UpdateDrawOfferIndicator();
                    
                    yield break; // Success, exit retry loop
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"UIManager: Exception subscribing to draw offer events: {e.Message}");
                }
            }
            
            retryCount++;
            Debug.LogWarning($"UIManager: GameEndDetectionManager not ready, retry {retryCount}/{maxRetries}");
            yield return new WaitForSeconds(retryDelay);
        }
        
        Debug.LogError("UIManager: Failed to subscribe to draw offer events after maximum retries");
    }
    
    /// <summary>
    /// Handle draw offer event
    /// </summary>
    private void OnDrawOffered(PieceColor offeringPlayer)
    {
        Debug.Log($"UIManager: Draw offered by {offeringPlayer}, updating indicator");
        UpdateDrawOfferIndicator();
    }
    
    /// <summary>
    /// Handle draw accepted event
    /// </summary>
    private void OnDrawAccepted(PieceColor acceptingPlayer)
    {
        Debug.Log($"UIManager: Draw accepted by {acceptingPlayer}, hiding indicator");
        UpdateDrawOfferIndicator();
    }
    
    /// <summary>
    /// Handle draw declined event
    /// </summary>
    private void OnDrawDeclined(PieceColor decliningPlayer)
    {
        Debug.Log($"UIManager: Draw declined by {decliningPlayer}, hiding indicator");
        UpdateDrawOfferIndicator();
    }
    
    /// <summary>
    /// Clean up event subscriptions
    /// </summary>
    private void OnDestroy()
    {
        // Clean up draw offer event subscriptions
        if (GameEndDetectionManager.Instance != null)
        {
            GameEndDetectionManager.Instance.OnDrawOffered -= OnDrawOffered;
            GameEndDetectionManager.Instance.OnDrawAccepted -= OnDrawAccepted;
            GameEndDetectionManager.Instance.OnDrawDeclined -= OnDrawDeclined;
        }
        
        // Stop any running coroutines
        if (indicatorPulseCoroutine != null)
        {
            StopCoroutine(indicatorPulseCoroutine);
        }
    }
    
    /// <summary>
    /// Create the new dedicated check indicator
    /// </summary>
    private void CreateCheckIndicatorUI()
    {
        Debug.Log("UIManager.CreateCheckIndicatorUI: ENTRY");
        
        GameObject checkIndicatorObject = new GameObject("CheckIndicatorUI");
        checkIndicatorObject.transform.SetParent(transform);
        
        checkIndicator = checkIndicatorObject.AddComponent<CheckIndicatorUI>();
        
        Debug.Log($"UIManager.CreateCheckIndicatorUI: Created CheckIndicatorUI component on GameObject '{checkIndicatorObject.name}'");
        Debug.Log($"UIManager.CreateCheckIndicatorUI: CheckIndicatorUI.Instance = {(CheckIndicatorUI.Instance != null ? "EXISTS" : "NULL")}");
        Debug.Log($"UIManager.CreateCheckIndicatorUI: checkIndicator reference = {(checkIndicator != null ? "EXISTS" : "NULL")}");
    }
    
    /// <summary>
    /// Create the persistent check status display
    /// </summary>
    private void CreatePersistentCheckStatusUI()
    {
        Debug.Log("UIManager.CreatePersistentCheckStatusUI: ENTRY");
        
        GameObject persistentStatusObject = new GameObject("PersistentCheckStatusUI");
        persistentStatusObject.transform.SetParent(transform);
        
        persistentCheckStatus = persistentStatusObject.AddComponent<PersistentCheckStatusUI>();
        
        Debug.Log($"UIManager.CreatePersistentCheckStatusUI: Created PersistentCheckStatusUI component on GameObject '{persistentStatusObject.name}'");
        Debug.Log($"UIManager.CreatePersistentCheckStatusUI: PersistentCheckStatusUI.Instance = {(PersistentCheckStatusUI.Instance != null ? "EXISTS" : "NULL")}");
        Debug.Log($"UIManager.CreatePersistentCheckStatusUI: persistentCheckStatus reference = {(persistentCheckStatus != null ? "EXISTS" : "NULL")}");
    }
    
    /// <summary>
    /// Find existing UI components in the scene
    /// </summary>
    private void FindExistingUIComponents()
    {
        // Find existing CheckStatusUI
        checkStatus = FindFirstObjectByType<CheckStatusUI>();
        if (checkStatus != null)
        {
            Debug.Log("UIManager: Found existing CheckStatusUI");
        }
        
        // Find BoardRotationUI
        boardRotation = FindFirstObjectByType<BoardRotationUI>();
        if (boardRotation != null)
        {
            Debug.Log("UIManager: Found existing BoardRotationUI");
        }
        
        // Find PlacementUI
        placementUI = FindFirstObjectByType<PlacementUI>();
        if (placementUI != null)
        {
            Debug.Log("UIManager: Found existing PlacementUI");
        }
    }
    
    /// <summary>
    /// Validate that all required UI components are available
    /// </summary>
    private void ValidateUIComponents()
    {
        Debug.Log("UIManager: Validating UI components");
        
        if (enableCheckIndicator && checkIndicator == null)
        {
            Debug.LogWarning("UIManager: CheckIndicatorUI not found but is enabled");
        }
        
        if (enablePersistentCheckStatus && persistentCheckStatus == null)
        {
            Debug.LogWarning("UIManager: PersistentCheckStatusUI not found but is enabled");
        }
        
        if (checkStatus == null)
        {
            Debug.LogWarning("UIManager: CheckStatusUI not found in scene");
        }
        
        // Check if CheckDetectionManager is available
        if (CheckDetectionManager.Instance == null)
        {
            Debug.LogWarning("UIManager: CheckDetectionManager not found - check detection UI will not function");
        }
        
        Debug.Log("UIManager: UI component validation complete");
    }
    
    /// <summary>
    /// Get the check indicator UI component
    /// </summary>
    public CheckIndicatorUI GetCheckIndicator()
    {
        return checkIndicator;
    }
    
    /// <summary>
    /// Get the check status UI component
    /// </summary>
    public CheckStatusUI GetCheckStatus()
    {
        return checkStatus;
    }
    
    /// <summary>
    /// Get the persistent check status UI component
    /// </summary>
    public PersistentCheckStatusUI GetPersistentCheckStatus()
    {
        return persistentCheckStatus;
    }
    
    /// <summary>
    /// Test all check UI components
    /// </summary>
    [ContextMenu("Test Check UI")]
    public void TestCheckUI()
    {
        Debug.Log("UIManager: Testing all check UI components");
        
        if (checkIndicator != null)
        {
            checkIndicator.TestCheckIndicator(PieceColor.White, false);
        }
        
        if (persistentCheckStatus != null)
        {
            persistentCheckStatus.TestCheckStatus();
        }
        
        if (checkStatus != null)
        {
            checkStatus.TestCheckUI();
        }
    }
    
    /// <summary>
    /// Show check for a specific player using persistent status only
    /// </summary>
    public void ShowPlayerCheck(PieceColor playerColor)
    {
        Debug.Log($"UIManager: Showing check for {playerColor} (persistent status only)");
        
        // Update persistent status display (only UI system enabled)
        if (persistentCheckStatus != null && enablePersistentCheckStatus)
        {
            bool whiteInCheck = (playerColor == PieceColor.White);
            bool blackInCheck = (playerColor == PieceColor.Black);
            persistentCheckStatus.SetCheckStatus(whiteInCheck, blackInCheck);
        }
        
        // Note: CheckIndicatorUI and CheckStatusUI disabled for cleaner UI
    }
    
    /// <summary>
    /// Reset all check status to safe (persistent status only)
    /// </summary>
    public void HideAllCheckIndicators()
    {
        Debug.Log("UIManager: Resetting check status to safe (persistent status only)");
        
        // Reset persistent status to safe (only UI system enabled)
        if (persistentCheckStatus != null)
        {
            persistentCheckStatus.SetCheckStatus(false, false);
        }
        
        // Note: CheckIndicatorUI and CheckStatusUI disabled for cleaner UI
    }
    
    /// <summary>
    /// Show checkmate for a specific player (persistent status only)
    /// </summary>
    public void ShowPlayerCheckmate(PieceColor checkmatedPlayer)
    {
        Debug.Log($"UIManager: Showing checkmate for {checkmatedPlayer} (persistent status only)");
        
        // Update persistent status to show checkmate via check status
        if (persistentCheckStatus != null && enablePersistentCheckStatus)
        {
            bool whiteInCheck = (checkmatedPlayer == PieceColor.White);
            bool blackInCheck = (checkmatedPlayer == PieceColor.Black);
            persistentCheckStatus.SetCheckStatus(whiteInCheck, blackInCheck);
            // Note: Persistent status will show "CHECK!" for checkmate - could be enhanced later
        }
        
        // Note: CheckIndicatorUI and CheckStatusUI disabled for cleaner UI
    }
    
    /// <summary>
    /// Create a return to menu button for returning to the main menu
    /// </summary>
    private void CreateReturnToMenuButton()
    {
        Debug.Log("UIManager: Creating Return to Menu button");
        
        // Create dedicated canvas for menu button with high sorting order
        Canvas uiCanvas = CreateMenuButtonCanvas();
        if (uiCanvas == null)
        {
            Debug.LogError("UIManager: Failed to create canvas for menu button");
            return;
        }
        
        // Create the button GameObject
        GameObject buttonObj = new GameObject("MenuButton");
        buttonObj.transform.SetParent(uiCanvas.transform, false);
        
        // Add UI components
        UnityEngine.UI.Button button = buttonObj.AddComponent<UnityEngine.UI.Button>();
        UnityEngine.UI.Image buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
        
        // Position the button to avoid status bar overlap
        // Status bar is at top (35px from top, 70px height), so place button lower and more to the right
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(-70, -100); // Adjusted position for larger button
        rectTransform.sizeDelta = new Vector2(120, 50); // Made button bigger (was 80x35)
        
        // Create button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        TMPro.TextMeshProUGUI buttonText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        buttonText.text = "Menu";
        buttonText.fontSize = 18; // Increased font size for bigger button (was 14)
        buttonText.color = Color.white;
        buttonText.alignment = TMPro.TextAlignmentOptions.Center;
        
        // Position the text
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Style the button with better visibility
        buttonImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f); // Darker, more opaque
        
        // Add button states for better feedback
        UnityEngine.UI.ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        button.colors = colors;
        
        // Add click listener to open game menu or return to main menu based on game state
        button.onClick.AddListener(() => {
            Debug.Log("UIManager: Menu button clicked successfully!");
            Debug.Log($"UIManager: Button position: {rectTransform.anchoredPosition}, size: {rectTransform.sizeDelta}");
            
            // Always show GameMenuUI when available - let it handle different game states internally
            if (GameMenuUI.Instance != null)
            {
                Debug.Log("UIManager: Opening GameMenuUI");
                GameMenuUI.Instance.ShowMenu();
            }
            else
            {
                Debug.LogWarning("UIManager: GameMenuUI not available, returning to menu directly as fallback");
                ReturnToMainMenu();
            }
        });
        
        // Create draw offer indicator if enabled
        if (enableDrawOfferIndicator)
        {
            CreateDrawOfferIndicator(buttonObj);
        }
        
        Debug.Log("UIManager: Menu button created successfully with high priority canvas");
    }
    
    /// <summary>
    /// Create a dedicated canvas for the menu button with proper sorting order
    /// </summary>
    private Canvas CreateMenuButtonCanvas()
    {
        // Create canvas GameObject
        GameObject canvasObj = new GameObject("MenuButtonCanvas");
        
        // Add Canvas component
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120; // Higher than PersistentCheckStatusUI (110) and other UI elements
        
        // Add CanvasScaler for proper scaling
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f; // Balanced scaling
        
        // Add GraphicRaycaster for input handling
        UnityEngine.UI.GraphicRaycaster raycaster = canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Ensure EventSystem exists
        EnsureEventSystemExists();
        
        Debug.Log($"UIManager: Created menu button canvas with sorting order {canvas.sortingOrder}");
        return canvas;
    }
    
    /// <summary>
    /// Create draw offer indicator as child of menu button
    /// </summary>
    private void CreateDrawOfferIndicator(GameObject menuButton)
    {
        // Create indicator GameObject
        drawOfferIndicator = new GameObject("DrawOfferIndicator");
        drawOfferIndicator.transform.SetParent(menuButton.transform, false);
        
        // Add Image component
        indicatorImage = drawOfferIndicator.AddComponent<UnityEngine.UI.Image>();
        indicatorImage.color = indicatorColor;
        
        // Create a circular sprite for the indicator
        indicatorImage.sprite = CreateCircleSprite();
        
        // Position the indicator in the top-right corner of the button
        RectTransform indicatorRect = drawOfferIndicator.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(1, 1);
        indicatorRect.anchorMax = new Vector2(1, 1);
        indicatorRect.anchoredPosition = new Vector2(-4, -4); // Slight offset from corner
        indicatorRect.sizeDelta = new Vector2(indicatorSize, indicatorSize);
        
        // Initially hidden
        drawOfferIndicator.SetActive(false);
        
        Debug.Log("UIManager: Draw offer indicator created");
    }
    
    /// <summary>
    /// Create a simple circle sprite for the indicator
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        // Create a simple white circle texture
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    // Create anti-aliased circle
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// Update draw offer indicator visibility based on current draw offer state
    /// </summary>
    public void UpdateDrawOfferIndicator()
    {
        if (!enableDrawOfferIndicator || drawOfferIndicator == null) return;
        
        bool shouldShow = GameEndDetectionManager.Instance != null && 
                         GameEndDetectionManager.Instance.IsDrawOfferPending();
        
        if (shouldShow && !drawOfferIndicator.activeInHierarchy)
        {
            ShowDrawOfferIndicator();
        }
        else if (!shouldShow && drawOfferIndicator.activeInHierarchy)
        {
            HideDrawOfferIndicator();
        }
    }
    
    /// <summary>
    /// Show the draw offer indicator with pulsing animation
    /// </summary>
    public void ShowDrawOfferIndicator()
    {
        if (drawOfferIndicator == null) return;
        
        drawOfferIndicator.SetActive(true);
        
        // Start pulsing animation
        if (indicatorPulseCoroutine != null)
        {
            StopCoroutine(indicatorPulseCoroutine);
        }
        indicatorPulseCoroutine = StartCoroutine(PulseIndicator());
        
        Debug.Log("UIManager: Draw offer indicator shown");
    }
    
    /// <summary>
    /// Hide the draw offer indicator
    /// </summary>
    public void HideDrawOfferIndicator()
    {
        if (drawOfferIndicator == null) return;
        
        drawOfferIndicator.SetActive(false);
        
        // Stop pulsing animation
        if (indicatorPulseCoroutine != null)
        {
            StopCoroutine(indicatorPulseCoroutine);
            indicatorPulseCoroutine = null;
        }
        
        Debug.Log("UIManager: Draw offer indicator hidden");
    }
    
    /// <summary>
    /// Pulsing animation for the draw offer indicator
    /// </summary>
    private System.Collections.IEnumerator PulseIndicator()
    {
        if (indicatorImage == null) yield break;
        
        while (drawOfferIndicator != null && drawOfferIndicator.activeInHierarchy)
        {
            // Pulse between 70% and 100% alpha
            for (float t = 0; t < 1; t += Time.deltaTime * pulseSpeed)
            {
                if (indicatorImage == null) yield break;
                
                float alpha = Mathf.Lerp(0.7f, 1f, (Mathf.Sin(t * Mathf.PI * 2) + 1) * 0.5f);
                Color currentColor = indicatorImage.color;
                currentColor.a = alpha;
                indicatorImage.color = currentColor;
                
                yield return null;
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Ensure EventSystem exists for UI input handling
    /// </summary>
    private void EnsureEventSystemExists()
    {
        UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        
        if (eventSystem == null)
        {
            Debug.Log("UIManager: Creating EventSystem for UI input handling");
            
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            Debug.Log("UIManager: EventSystem created successfully");
        }
        else
        {
            Debug.Log("UIManager: EventSystem already exists");
        }
    }
    
    /// <summary>
    /// Return to the main menu scene
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("UIManager: Returning to main menu");
        
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("UIManager: SceneController.Instance not found - cannot return to menu");
        }
    }
}