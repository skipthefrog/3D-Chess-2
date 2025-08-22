using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Dedicated UI component to display CHECK status with neon glow effects
/// Positioned prominently on screen to clearly indicate when a player is in check
/// </summary>
public class CheckIndicatorUI : MonoBehaviour
{
    [Header("Check Indicator Settings")]
    public bool enableIndicator = true;
    public float glowIntensity = 2.0f;
    public float pulseSpeed = 2.0f;
    
    // UI Components
    private Canvas checkCanvas;
    private GameObject checkPanel;
    private Text checkText;
    private Image backgroundImage;
    
    // Animation state
    private Coroutine glowAnimation;
    private bool isShowingCheck = false;
    private PieceColor currentCheckPlayer = PieceColor.White;
    
    // Materials and effects
    private Material glowMaterial;
    private Color baseGlowColor = Color.red;
    private Color currentGlowColor = Color.red;
    
    public static CheckIndicatorUI Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("CheckIndicatorUI: Instance created");
        }
        else
        {
            Debug.LogWarning("CheckIndicatorUI: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
            return;
        }
        
        CreateCheckIndicatorUI();
    }
    
    private void Start()
    {
        // Initially hide the indicator
        HideCheckIndicator();
        
        // Start delayed event subscription with retry mechanism
        StartCoroutine(SubscribeToEventsWithRetry());
        
        // Coordinate with existing CheckStatusUI - disable its automatic display
        if (CheckStatusUI.Instance != null)
        {
            Debug.Log("CheckIndicatorUI: Found existing CheckStatusUI, coordinating displays");
            // Let both systems coexist - CheckStatusUI shows temporary messages, 
            // CheckIndicatorUI shows persistent indicator
        }
    }
    
    /// <summary>
    /// Retry mechanism to ensure event subscription happens after CheckDetectionManager is ready
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
                    CheckDetectionManager.Instance.OnKingInCheck += OnKingInCheck;
                    CheckDetectionManager.Instance.OnCheckResolved += OnCheckResolved;
                    CheckDetectionManager.Instance.OnCheckmate += OnCheckmate;
                    Debug.Log($"CheckIndicatorUI: Successfully subscribed to check detection events (attempt {retryCount + 1})");
                    
                    // Validate subscription by checking if events have handlers
                    int kingInCheckHandlers = CheckDetectionManager.Instance.OnKingInCheck?.GetInvocationList()?.Length ?? 0;
                    Debug.Log($"CheckIndicatorUI: OnKingInCheck now has {kingInCheckHandlers} handlers");
                    
                    yield break; // Success, exit retry loop
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"CheckIndicatorUI: Exception subscribing to events: {e.Message}");
                }
            }
            
            retryCount++;
            Debug.LogWarning($"CheckIndicatorUI: CheckDetectionManager not ready, retry {retryCount}/{maxRetries}");
            yield return new WaitForSeconds(retryDelay);
        }
        
        Debug.LogError("CheckIndicatorUI: Failed to subscribe to check detection events after maximum retries");
    }
    
    private void OnDestroy()
    {
        if (CheckDetectionManager.Instance != null)
        {
            CheckDetectionManager.Instance.OnKingInCheck -= OnKingInCheck;
            CheckDetectionManager.Instance.OnCheckResolved -= OnCheckResolved;
            CheckDetectionManager.Instance.OnCheckmate -= OnCheckmate;
        }
        
        // Clean up animation
        if (glowAnimation != null)
        {
            StopCoroutine(glowAnimation);
        }
    }
    
    /// <summary>
    /// Create the check indicator UI elements
    /// </summary>
    private void CreateCheckIndicatorUI()
    {
        Debug.Log("CheckIndicatorUI: Creating check indicator UI");
        
        // Create canvas for check indicator
        GameObject canvasObject = new GameObject("CheckIndicatorCanvas");
        canvasObject.transform.SetParent(transform);
        
        checkCanvas = canvasObject.AddComponent<Canvas>();
        checkCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        checkCanvas.sortingOrder = 100; // Ensure it's on top
        
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(375f, 812f); // iPhone X reference
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObject.AddComponent<GraphicRaycaster>();
        
        // Create background panel
        checkPanel = new GameObject("CheckPanel");
        checkPanel.transform.SetParent(canvasObject.transform);
        
        backgroundImage = checkPanel.AddComponent<Image>();
        RectTransform panelRect = checkPanel.GetComponent<RectTransform>();
        
        // Position at center-top of screen
        panelRect.anchorMin = new Vector2(0.5f, 0.85f);
        panelRect.anchorMax = new Vector2(0.5f, 0.85f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(200f, 60f);
        
        // Create check text
        GameObject textObject = new GameObject("CheckText");
        textObject.transform.SetParent(checkPanel.transform);
        
        checkText = textObject.AddComponent<Text>();
        checkText.text = "CHECK!";
        checkText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        checkText.fontSize = 24;
        checkText.fontStyle = FontStyle.Bold;
        checkText.alignment = TextAnchor.MiddleCenter;
        checkText.color = Color.white;
        
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Create glow materials
        CreateGlowMaterials();
        
        Debug.Log("CheckIndicatorUI: UI creation complete");
    }
    
    /// <summary>
    /// Create materials for glow effects
    /// </summary>
    private void CreateGlowMaterials()
    {
        // Try different shader options for best compatibility
        Shader glowShader = Shader.Find("UI/Unlit/Transparent");
        if (glowShader == null)
        {
            glowShader = Shader.Find("UI/Default");
        }
        if (glowShader == null)
        {
            glowShader = Shader.Find("Sprites/Default");
        }
        
        glowMaterial = new Material(glowShader);
        
        Debug.Log($"CheckIndicatorUI: Created glow material with shader: {glowShader.name}");
    }
    
    /// <summary>
    /// Event handler for when a king is in check
    /// </summary>
    private void OnKingInCheck(PieceColor kingColor)
    {
        Debug.Log($"🚨 CheckIndicatorUI.OnKingInCheck: ENTRY - {kingColor} king is in check!");
        Debug.Log($"CheckIndicatorUI.OnKingInCheck: enableIndicator = {enableIndicator}");
        Debug.Log($"CheckIndicatorUI.OnKingInCheck: checkPanel = {(checkPanel != null ? checkPanel.name : "NULL")}");
        Debug.Log($"CheckIndicatorUI.OnKingInCheck: Instance = {(Instance != null ? "EXISTS" : "NULL")}");
        
        if (!enableIndicator)
        {
            Debug.Log("CheckIndicatorUI: Indicator disabled, skipping display");
            return;
        }
        
        currentCheckPlayer = kingColor;
        Debug.Log($"CheckIndicatorUI.OnKingInCheck: About to call ShowCheckIndicator({kingColor}, false)");
        ShowCheckIndicator(kingColor, false);
        Debug.Log($"CheckIndicatorUI.OnKingInCheck: ShowCheckIndicator call completed");
    }
    
    /// <summary>
    /// Event handler for when check is resolved
    /// </summary>
    private void OnCheckResolved(PieceColor kingColor)
    {
        Debug.Log($"CheckIndicatorUI.OnCheckResolved: {kingColor} check resolved");
        HideCheckIndicator();
    }
    
    /// <summary>
    /// Event handler for checkmate
    /// </summary>
    private void OnCheckmate(PieceColor kingColor)
    {
        Debug.Log($"CheckIndicatorUI.OnCheckmate: {kingColor} is in checkmate!");
        ShowCheckIndicator(kingColor, true);
    }
    
    /// <summary>
    /// Show the check indicator with glow effects
    /// </summary>
    /// <param name="playerInCheck">Color of player in check</param>
    /// <param name="isCheckmate">Whether this is checkmate (more dramatic effect)</param>
    public void ShowCheckIndicator(PieceColor playerInCheck, bool isCheckmate = false)
    {
        if (checkPanel == null)
        {
            Debug.LogError("CheckIndicatorUI: Check panel not created, cannot show indicator");
            return;
        }
        
        Debug.Log($"CheckIndicatorUI: Showing check indicator for {playerInCheck} (checkmate: {isCheckmate})");
        
        isShowingCheck = true;
        currentCheckPlayer = playerInCheck;
        
        // Update text based on check type
        if (checkText != null)
        {
            checkText.text = isCheckmate ? "CHECKMATE!" : "CHECK!";
            checkText.fontSize = isCheckmate ? 20 : 24; // Slightly smaller for longer text
        }
        
        // Set color based on player
        baseGlowColor = playerInCheck == PieceColor.White ? Color.red : Color.yellow;
        currentGlowColor = baseGlowColor;
        
        // Show the panel
        checkPanel.SetActive(true);
        
        // Start glow animation
        if (glowAnimation != null)
        {
            StopCoroutine(glowAnimation);
        }
        glowAnimation = StartCoroutine(GlowAnimation(isCheckmate));
        
        Debug.Log($"CheckIndicatorUI: Check indicator shown for {playerInCheck}");
    }
    
    /// <summary>
    /// Hide the check indicator
    /// </summary>
    public void HideCheckIndicator()
    {
        if (checkPanel == null) return;
        
        Debug.Log("CheckIndicatorUI: Hiding check indicator");
        
        isShowingCheck = false;
        
        // Stop glow animation
        if (glowAnimation != null)
        {
            StopCoroutine(glowAnimation);
            glowAnimation = null;
        }
        
        // Hide the panel
        checkPanel.SetActive(false);
        
        Debug.Log("CheckIndicatorUI: Check indicator hidden");
    }
    
    /// <summary>
    /// Glow animation coroutine
    /// </summary>
    private IEnumerator GlowAnimation(bool isCheckmate)
    {
        Debug.Log($"CheckIndicatorUI: Starting glow animation (checkmate: {isCheckmate})");
        
        float animationSpeed = isCheckmate ? pulseSpeed * 1.5f : pulseSpeed;
        float maxIntensity = isCheckmate ? glowIntensity * 1.5f : glowIntensity;
        
        while (isShowingCheck)
        {
            // Pulse the glow intensity
            float time = Time.time * animationSpeed;
            float pulseValue = (Mathf.Sin(time) + 1f) * 0.5f; // 0 to 1
            float currentIntensity = Mathf.Lerp(0.3f, maxIntensity, pulseValue);
            
            // Apply glow effect to background
            if (backgroundImage != null)
            {
                Color glowColor = currentGlowColor;
                glowColor.a = 0.7f + (pulseValue * 0.3f); // Vary alpha for glow effect
                backgroundImage.color = glowColor;
            }
            
            // Apply glow effect to text
            if (checkText != null)
            {
                Color textColor = Color.white;
                textColor.a = 0.8f + (pulseValue * 0.2f); // Subtle text glow
                checkText.color = textColor;
                
                // Scale text slightly for emphasis
                float scale = 1.0f + (pulseValue * 0.1f);
                checkText.transform.localScale = Vector3.one * scale;
            }
            
            yield return null;
        }
        
        Debug.Log("CheckIndicatorUI: Glow animation stopped");
    }
    
    /// <summary>
    /// Test method to manually trigger check indicator (for debugging)
    /// </summary>
    public void TestCheckIndicator(PieceColor playerColor, bool isCheckmate = false)
    {
        Debug.Log($"CheckIndicatorUI.TestCheckIndicator: Testing indicator for {playerColor} (checkmate: {isCheckmate})");
        Debug.Log($"CheckIndicatorUI.TestCheckIndicator: checkPanel null? {checkPanel == null}");
        Debug.Log($"CheckIndicatorUI.TestCheckIndicator: checkText null? {checkText == null}");
        Debug.Log($"CheckIndicatorUI.TestCheckIndicator: Canvas null? {checkCanvas == null}");
        
        ShowCheckIndicator(playerColor, isCheckmate);
        
        // Auto-hide after 3 seconds for testing
        StartCoroutine(TestAutoHide());
    }
    
    /// <summary>
    /// Test method accessible from console - call this to test if UI works at all
    /// </summary>
    [ContextMenu("Test White Check")]
    public void TestWhiteCheck()
    {
        Debug.Log("CheckIndicatorUI: Manual test - showing White check");
        TestCheckIndicator(PieceColor.White, false);
    }
    
    [ContextMenu("Test Black Check")]
    public void TestBlackCheck()
    {
        Debug.Log("CheckIndicatorUI: Manual test - showing Black check");
        TestCheckIndicator(PieceColor.Black, false);
    }
    
    private IEnumerator TestAutoHide()
    {
        yield return new WaitForSeconds(3f);
        HideCheckIndicator();
    }
    
    /// <summary>
    /// Get current check status
    /// </summary>
    public bool IsShowingCheck()
    {
        return isShowingCheck;
    }
    
    /// <summary>
    /// Get the player currently shown as in check
    /// </summary>
    public PieceColor GetCurrentCheckPlayer()
    {
        return currentCheckPlayer;
    }
}