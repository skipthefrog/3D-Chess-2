using UnityEngine;
using System.Collections;

/// <summary>
/// Test script to verify AI gameplay continuation fixes
/// This script monitors AI behavior to ensure they continue playing without getting stuck
/// </summary>
public class Test_AIContinuationFix : MonoBehaviour
{
    [Header("Test Settings")]
    public bool enableTestMode = false;
    public float monitoringInterval = 5.0f; // Check every 5 seconds
    public float maxTurnTime = 60.0f; // Max time allowed per AI turn
    
    [Header("Test Status")]
    public bool testRunning = false;
    public int turnsCompleted = 0;
    public float currentTurnStartTime = 0f;
    public PieceColor lastCurrentPlayer = PieceColor.White;
    
    private Coroutine monitoringCoroutine;
    
    private void Start()
    {
        if (enableTestMode)
        {
            Debug.Log("🧪 AI Continuation Test: Starting monitoring...");
            StartTest();
        }
    }
    
    private void Update()
    {
        // Toggle test mode with T key for debugging
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (testRunning)
            {
                StopTest();
            }
            else
            {
                StartTest();
            }
        }
    }
    
    /// <summary>
    /// Start the AI continuation test
    /// </summary>
    public void StartTest()
    {
        if (testRunning)
        {
            Debug.LogWarning("🧪 AI Continuation Test: Test already running");
            return;
        }
        
        testRunning = true;
        turnsCompleted = 0;
        
        Debug.Log("🧪 AI Continuation Test: Starting AI gameplay monitoring");
        Debug.Log($"🧪 Monitoring interval: {monitoringInterval}s, Max turn time: {maxTurnTime}s");
        
        // Subscribe to turn change events
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged += OnTurnChanged;
        }
        
        // Subscribe to timer events
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.OnAIThinkingStarted += OnAIThinkingStarted;
            TimerManager.Instance.OnAIThinkingFinished += OnAIThinkingFinished;
        }
        
        // Start monitoring coroutine
        monitoringCoroutine = StartCoroutine(MonitorAIBehavior());
        
        // Record initial state
        if (TurnManager.Instance != null)
        {
            lastCurrentPlayer = TurnManager.Instance.GetCurrentPlayer();
            currentTurnStartTime = Time.time;
        }
    }
    
    /// <summary>
    /// Stop the AI continuation test
    /// </summary>
    public void StopTest()
    {
        if (!testRunning)
        {
            return;
        }
        
        testRunning = false;
        
        Debug.Log($"🧪 AI Continuation Test: Stopping test after {turnsCompleted} turns completed");
        
        // Unsubscribe from events
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnChanged -= OnTurnChanged;
        }
        
        if (TimerManager.Instance != null)
        {
            TimerManager.Instance.OnAIThinkingStarted -= OnAIThinkingStarted;
            TimerManager.Instance.OnAIThinkingFinished -= OnAIThinkingFinished;
        }
        
        // Stop monitoring coroutine
        if (monitoringCoroutine != null)
        {
            StopCoroutine(monitoringCoroutine);
            monitoringCoroutine = null;
        }
    }
    
    /// <summary>
    /// Monitor AI behavior for signs of getting stuck
    /// </summary>
    private IEnumerator MonitorAIBehavior()
    {
        while (testRunning)
        {
            yield return new WaitForSeconds(monitoringInterval);
            
            // Check if game is in playing state
            if (GameStateManager.Instance?.currentState != GameState.Playing)
            {
                Debug.Log("🧪 AI Continuation Test: Game not in Playing state, pausing monitoring");
                continue;
            }
            
            // Check current turn duration
            float currentTurnDuration = Time.time - currentTurnStartTime;
            
            if (TurnManager.Instance != null)
            {
                PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();
                bool isAI = TurnManager.Instance.IsPlayerAI(currentPlayer);
                
                Debug.Log($"🧪 AI Continuation Test: Turn {turnsCompleted + 1}");
                Debug.Log($"🧪   Current player: {currentPlayer} (AI: {isAI})");
                Debug.Log($"🧪   Turn duration: {currentTurnDuration:F1}s");
                
                // Check for stuck AI
                if (isAI && currentTurnDuration > maxTurnTime)
                {
                    Debug.LogError($"🚨 AI Continuation Test: STUCK AI DETECTED!");
                    Debug.LogError($"🚨   {currentPlayer} has been thinking for {currentTurnDuration:F1}s (max: {maxTurnTime}s)");
                    
                    // Check AI state
                    if (AIPlayer.Instance != null)
                    {
                        bool aiThinking = AIPlayer.Instance.IsThinking();
                        Debug.LogError($"🚨   AIPlayer.IsThinking(): {aiThinking}");
                    }
                    
                    // Check timer state
                    if (TimerManager.Instance != null)
                    {
                        bool timerRunning = TimerManager.Instance.IsTimerRunning();
                        bool aiThinking = TimerManager.Instance.IsAIThinking();
                        float remainingTime = TimerManager.Instance.GetRemainingTime(currentPlayer);
                        
                        Debug.LogError($"🚨   Timer running: {timerRunning}, AI thinking: {aiThinking}");
                        Debug.LogError($"🚨   Remaining time: {remainingTime:F1}s");
                    }
                    
                    // CRITICAL: This indicates our fixes didn't work properly
                    ReportTestFailure("AI got stuck despite fixes");
                }
                else if (isAI && currentTurnDuration > 10.0f)
                {
                    Debug.LogWarning($"⚠️ AI Continuation Test: {currentPlayer} thinking for {currentTurnDuration:F1}s (normal but worth monitoring)");
                }
            }
            
            // Check timer integration
            if (TimerManager.Instance != null && TimerManager.Instance.IsTimedPlayActive())
            {
                bool timerRunning = TimerManager.Instance.IsTimerRunning();
                bool aiThinking = TimerManager.Instance.IsAIThinking();
                
                Debug.Log($"🧪   Timer status: running={timerRunning}, AI thinking={aiThinking}");
                
                // Verify timer pauses during AI thinking
                if (TurnManager.Instance != null)
                {
                    PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();
                    bool isAI = TurnManager.Instance.IsPlayerAI(currentPlayer);
                    
                    if (isAI && AIPlayer.Instance != null && AIPlayer.Instance.IsThinking() && !aiThinking)
                    {
                        Debug.LogWarning($"⚠️ AI Continuation Test: Timer integration issue - AI thinking but timer not paused");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Handle turn change events
    /// </summary>
    private void OnTurnChanged(PieceColor newPlayer)
    {
        if (!testRunning) return;
        
        turnsCompleted++;
        currentTurnStartTime = Time.time;
        
        Debug.Log($"✅ AI Continuation Test: Turn changed to {newPlayer} (Turn #{turnsCompleted})");
        
        lastCurrentPlayer = newPlayer;
        
        // Check if this turn change indicates the previous AI completed successfully
        if (TurnManager.Instance != null)
        {
            bool isAI = TurnManager.Instance.IsPlayerAI(newPlayer);
            Debug.Log($"🧪 AI Continuation Test: New player {newPlayer} is AI: {isAI}");
        }
    }
    
    /// <summary>
    /// Handle AI thinking started events
    /// </summary>
    private void OnAIThinkingStarted(PieceColor aiPlayer)
    {
        if (!testRunning) return;
        
        Debug.Log($"🧠 AI Continuation Test: {aiPlayer} AI started thinking");
        
        // Verify timer integration
        if (TimerManager.Instance != null)
        {
            bool aiThinking = TimerManager.Instance.IsAIThinking(aiPlayer);
            Debug.Log($"🧪   Timer reports AI thinking: {aiThinking}");
        }
    }
    
    /// <summary>
    /// Handle AI thinking finished events
    /// </summary>
    private void OnAIThinkingFinished(PieceColor aiPlayer)
    {
        if (!testRunning) return;
        
        float thinkingDuration = Time.time - currentTurnStartTime;
        Debug.Log($"🧠 AI Continuation Test: {aiPlayer} AI finished thinking after {thinkingDuration:F1}s");
        
        // Verify timer integration
        if (TimerManager.Instance != null)
        {
            bool aiThinking = TimerManager.Instance.IsAIThinking(aiPlayer);
            Debug.Log($"🧪   Timer reports AI thinking: {aiThinking} (should be false)");
        }
    }
    
    /// <summary>
    /// Report test failure
    /// </summary>
    private void ReportTestFailure(string reason)
    {
        Debug.LogError($"💥 AI CONTINUATION TEST FAILED: {reason}");
        Debug.LogError($"💥 After {turnsCompleted} completed turns");
        Debug.LogError($"💥 This indicates the AI continuation fixes need further work");
        
        StopTest();
    }
    
    /// <summary>
    /// Get test status summary
    /// </summary>
    public string GetTestStatus()
    {
        if (!testRunning)
        {
            return "AI Continuation Test: Not running (Press T to start)";
        }
        
        float currentTurnDuration = Time.time - currentTurnStartTime;
        string currentPlayerInfo = "Unknown";
        
        if (TurnManager.Instance != null)
        {
            PieceColor currentPlayer = TurnManager.Instance.GetCurrentPlayer();
            bool isAI = TurnManager.Instance.IsPlayerAI(currentPlayer);
            currentPlayerInfo = $"{currentPlayer} (AI: {isAI})";
        }
        
        return $"AI Test: Running | Turns: {turnsCompleted} | Current: {currentPlayerInfo} | Duration: {currentTurnDuration:F1}s";
    }
    
    private void OnDestroy()
    {
        StopTest();
    }
    
    private void OnGUI()
    {
        if (!enableTestMode) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label("=== AI Continuation Test ===");
        GUILayout.Label(GetTestStatus());
        
        if (GUILayout.Button(testRunning ? "Stop Test" : "Start Test"))
        {
            if (testRunning)
            {
                StopTest();
            }
            else
            {
                StartTest();
            }
        }
        
        GUILayout.EndArea();
    }
}