using UnityEngine;
using System.Collections;

/// <summary>
/// Test the AI move validation fixes to ensure illegal moves don't cause game freezing
/// Specifically tests the enhanced validation system in MinimaxEngine
/// </summary>
public class AIValidationTester : MonoBehaviour
{
    [Header("AI Validation Testing")]
    public bool runTestsOnStart = false;
    public bool enableVerboseLogging = true;
    
    [Header("Test Configuration")]
    public AIDifficulty testDifficulty = AIDifficulty.Easy;
    public int maxTestMoves = 10; // Limit moves to prevent infinite loops
    
    private int testMovesExecuted = 0;
    private bool testInProgress = false;
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            StartCoroutine(RunValidationTestsDelayed());
        }
    }
    
    /// <summary>
    /// Run validation tests after ensuring all systems are initialized
    /// </summary>
    private IEnumerator RunValidationTestsDelayed()
    {
        Debug.Log("🧪 AIValidationTester: Starting AI validation tests...");
        
        // Wait for all systems to initialize
        yield return new WaitForSeconds(2f);
        
        // Validate all required systems are available
        if (!ValidateTestEnvironment())
        {
            Debug.LogError("🚨 AIValidationTester: Test environment validation failed - aborting tests");
            yield break;
        }
        
        Debug.Log("✅ AIValidationTester: Environment validated - starting tests");
        
        // Run the AI validation tests
        yield return StartCoroutine(TestAIValidationSystem());
        
        Debug.Log("🏁 AIValidationTester: All validation tests completed");
    }
    
    /// <summary>
    /// Validate that all required systems are available for testing
    /// </summary>
    private bool ValidateTestEnvironment()
    {
        Debug.Log("🔍 AIValidationTester: Validating test environment...");
        
        if (GameStateManager.Instance == null)
        {
            Debug.LogError("❌ GameStateManager.Instance is null");
            return false;
        }
        
        if (TurnManager.Instance == null)
        {
            Debug.LogError("❌ TurnManager.Instance is null");
            return false;
        }
        
        if (AIPlayer.Instance == null)
        {
            Debug.LogError("❌ AIPlayer.Instance is null");
            return false;
        }
        
        if (ChessBoard.Instance == null)
        {
            Debug.LogError("❌ ChessBoard.Instance is null");
            return false;
        }
        
        if (GameEndDetectionManager.Instance == null)
        {
            Debug.LogError("❌ GameEndDetectionManager.Instance is null");
            return false;
        }
        
        Debug.Log("✅ AIValidationTester: All required systems are available");
        return true;
    }
    
    /// <summary>
    /// Test the AI validation system to ensure illegal moves don't freeze the game
    /// </summary>
    private IEnumerator TestAIValidationSystem()
    {
        Debug.Log("🤖 AIValidationTester: Testing AI validation system...");
        
        testInProgress = true;
        testMovesExecuted = 0;
        
        // Set up AI vs AI mode for testing
        TurnManager.Instance.SetGameMode(PlayerType.Computer, PlayerType.Computer);
        AIPlayer.Instance.SetDifficulty(testDifficulty);
        
        // Subscribe to AI move events to monitor validation
        AIPlayer.Instance.OnAIMoveDecided += OnAIMoveDecided;
        
        // Set up a quick placement configuration to get to gameplay
        yield return StartCoroutine(SetupQuickGameForTesting());
        
        // Start AI vs AI gameplay and monitor for validation issues
        Debug.Log("🎮 AIValidationTester: Starting AI vs AI gameplay test...");
        
        if (GameStateManager.Instance.CanMovePieces())
        {
            Debug.Log("✅ AIValidationTester: Game is in playing state - monitoring AI moves");
            
            // Monitor AI moves for validation issues
            float testStartTime = Time.time;
            float testTimeout = 30f; // 30 second timeout
            
            while (testInProgress && 
                   testMovesExecuted < maxTestMoves && 
                   Time.time - testStartTime < testTimeout &&
                   GameStateManager.Instance.currentState == GameState.Playing)
            {
                yield return new WaitForSeconds(0.5f);
                
                // Check if AI is stuck (no move for too long)
                if (AIPlayer.Instance.IsThinking())
                {
                    if (enableVerboseLogging)
                        Debug.Log($"🧠 AIValidationTester: AI is thinking... (move {testMovesExecuted + 1})");
                }
            }
            
            if (testMovesExecuted >= maxTestMoves)
            {
                Debug.Log($"✅ AIValidationTester: Successfully completed {testMovesExecuted} AI moves without validation issues");
            }
            else if (Time.time - testStartTime >= testTimeout)
            {
                Debug.LogError("⏰ AIValidationTester: Test timed out - possible AI stuck situation");
            }
            else
            {
                Debug.Log($"🏁 AIValidationTester: Game ended after {testMovesExecuted} moves - {GameStateManager.Instance.currentState}");
            }
        }
        else
        {
            Debug.LogError("🚨 AIValidationTester: Game is not in playing state - cannot test AI moves");
        }
        
        // Clean up
        AIPlayer.Instance.OnAIMoveDecided -= OnAIMoveDecided;
        testInProgress = false;
        
        Debug.Log("🏁 AIValidationTester: AI validation testing completed");
    }
    
    /// <summary>
    /// Monitor AI move decisions for validation issues
    /// </summary>
    private void OnAIMoveDecided(PieceColor player, BoardPosition from, BoardPosition to)
    {
        testMovesExecuted++;
        
        Debug.Log($"🎯 AIValidationTester: AI Move {testMovesExecuted} - {player} {from} → {to}");
        
        // Validate that the move is actually legal
        ChessPiece piece = ChessBoard.Instance.GetPieceAt(from);
        if (piece != null)
        {
            var legalMoves = piece.GetLegalMoves();
            bool isMoveLegal = legalMoves.Contains(to);
            
            if (isMoveLegal)
            {
                Debug.Log($"✅ AIValidationTester: Move is legal ({legalMoves.Count} legal moves available)");
            }
            else
            {
                Debug.LogError($"🚨 AIValidationTester: ILLEGAL MOVE DETECTED! {piece.pieceType} at {from} cannot legally move to {to}");
                Debug.LogError($"   Legal moves: {string.Join(", ", legalMoves)}");
                
                // This would indicate the validation fix failed
                testInProgress = false;
            }
        }
        else
        {
            Debug.LogError($"🚨 AIValidationTester: No piece found at {from} for AI move!");
            testInProgress = false;
        }
    }
    
    /// <summary>
    /// Set up a quick game configuration for testing AI validation
    /// </summary>
    private IEnumerator SetupQuickGameForTesting()
    {
        Debug.Log("⚡ AIValidationTester: Setting up quick game configuration...");
        
        // Skip placement phase and manually place a few key pieces for testing
        if (GameStateManager.Instance.currentState == GameState.PiecePlacement)
        {
            Debug.Log("📦 AIValidationTester: Skipping placement phase - manually placing test pieces");
            
            // Place a few pieces manually to create a testable game state
            PlaceTestPieces();
            
            // Transition to playing state
            GameStateManager.Instance.ChangeState(GameState.Playing);
            
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log("✅ AIValidationTester: Game setup complete");
    }
    
    /// <summary>
    /// Place a minimal set of pieces for testing AI validation
    /// </summary>
    private void PlaceTestPieces()
    {
        if (ChessBoard.Instance == null) return;
        
        Debug.Log("🔧 AIValidationTester: Placing test pieces...");
        
        // This is a simplified setup - in reality you'd need proper piece instantiation
        // For now, we'll rely on existing placement systems or skip to playing state
        // where pieces might already be placed from previous gameplay
        
        Debug.Log("🔧 AIValidationTester: Test piece placement completed");
    }
    
    /// <summary>
    /// Manual test trigger for context menu
    /// </summary>
    [ContextMenu("Run AI Validation Tests")]
    public void RunTestsFromContextMenu()
    {
        if (!testInProgress)
        {
            StartCoroutine(RunValidationTestsDelayed());
        }
        else
        {
            Debug.LogWarning("AIValidationTester: Tests already in progress");
        }
    }
    
    /// <summary>
    /// Stop ongoing tests
    /// </summary>
    [ContextMenu("Stop Tests")]
    public void StopTests()
    {
        testInProgress = false;
        Debug.Log("🛑 AIValidationTester: Tests stopped manually");
    }
}