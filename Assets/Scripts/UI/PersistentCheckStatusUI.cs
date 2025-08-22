using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Persistent UI component that always displays the current check status for both players
/// Shows "White: Safe | Black: CHECK!" style messages that never disappear
/// Uses event-driven updates for instant response to check conditions (no polling)
/// Only updates when check status actually changes via CheckDetectionManager events
/// </summary>
public class PersistentCheckStatusUI : MonoBehaviour
{
    [Header("Persistent Status Settings")]
    public bool enablePersistentStatus = true;
    public bool enableEventDrivenUpdates = true; // Use events instead of polling
    
    [Header("Visual Settings")]
    public int fontSize = 20;
    public Color safeColor = Color.green;
    public Color checkColor = Color.red;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
    
    // UI Components
    private Canvas statusCanvas;
    private GameObject statusPanel;
    private Text statusText;
    private Image backgroundImage;
    
    // Status tracking
    private bool whiteInCheck = false;
    private bool blackInCheck = false;
    private bool lastWhiteStatus = false;
    private bool lastBlackStatus = false;
    
    // Game end tracking
    private bool gameEnded = false;
    private string gameEndMessage = "";
    
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
        
        // Position at top-center of screen (where BoardRotationUI was)
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -40f); // 40 pixels from top
        panelRect.sizeDelta = new Vector2(320f, 60f); // Wide enough for status text
        
        // Create status text
        GameObject textObject = new GameObject("StatusText");
        textObject.transform.SetParent(statusPanel.transform);
        
        statusText = textObject.AddComponent<Text>();
        statusText.text = "White: Safe | Black: Safe";
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
                        Debug.Log("PersistentCheckStatusUI: Successfully subscribed to game end events");
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
    /// Update the status display with current check states
    /// </summary>
    private void UpdateStatusDisplay()
    {
        if (statusText == null) return;
        
        string statusMessage;
        
        // Show game end message if game is over
        if (gameEnded)
        {
            statusMessage = gameEndMessage;
            statusText.text = statusMessage;
            
            // Special background for game end
            if (gameEndMessage.Contains("CHECKMATE"))
            {
                backgroundImage.color = new Color(0.9f, 0.1f, 0.1f, 0.9f); // Bright red for checkmate
            }
            else if (gameEndMessage.Contains("STALEMATE"))
            {
                backgroundImage.color = new Color(0.9f, 0.9f, 0.1f, 0.9f); // Yellow for stalemate
            }
            
            // Add pulsing effect for game end
            StartCoroutine(PulseEffect());
        }
        else
        {
            // Normal game status
            string whiteStatus = whiteInCheck ? "CHECK!" : "Safe";
            string blackStatus = blackInCheck ? "CHECK!" : "Safe";
            
            statusMessage = $"White: {whiteStatus} | Black: {blackStatus}";
            statusText.text = statusMessage;
            
            // Update background color based on check status
            if (whiteInCheck || blackInCheck)
            {
                // Red background if anyone is in check
                backgroundImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
                
                // Add pulsing effect for check status
                StartCoroutine(PulseEffect());
            }
            else
            {
                // Dark background if both players are safe
                backgroundImage.color = backgroundColor;
            }
        }
        
        Debug.Log($"PersistentCheckStatusUI: Display updated - {statusMessage}");
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
    /// Force an immediate status update
    /// </summary>
    public void ForceUpdate()
    {
        if (CheckDetectionManager.Instance != null)
        {
            bool currentWhiteStatus = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.White);
            bool currentBlackStatus = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.Black);
            
            whiteInCheck = currentWhiteStatus;
            blackInCheck = currentBlackStatus;
            UpdateStatusDisplay();
            
            Debug.Log($"PersistentCheckStatusUI.ForceUpdate: White={whiteInCheck}, Black={blackInCheck}");
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
        Debug.Log("PersistentCheckStatusUI: Starting test sequence");
        
        SetCheckStatus(false, false);
        yield return new WaitForSeconds(2f);
        
        SetCheckStatus(true, false);
        yield return new WaitForSeconds(2f);
        
        SetCheckStatus(true, true);
        yield return new WaitForSeconds(2f);
        
        SetCheckStatus(false, true);
        yield return new WaitForSeconds(2f);
        
        SetCheckStatus(false, false);
        
        Debug.Log("PersistentCheckStatusUI: Test sequence complete");
    }
}