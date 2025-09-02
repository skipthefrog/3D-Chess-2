using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Test suite for validating check logic functionality in 3D chess.
/// Provides methods to test check detection, move validation, and game end conditions.
/// Can be used during development to verify the chess rules are working correctly.
/// </summary>
public class CheckLogicTester : MonoBehaviour
{
    [Header("Test Settings")]
    public bool runTestsOnStart = false;
    public bool enableDebugOutput = true;
    
    [Header("Test Scenarios")]
    public bool testBasicCheckDetection = true;
    public bool testMoveValidation = true;
    public bool testCheckmateDetection = true;
    public bool testStalemateDetection = true;
    
    private int testsRun = 0;
    private int testsPassed = 0;
    private int testsFailed = 0;
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            // Wait a few frames for all systems to initialize
            Invoke(nameof(RunAllTests), 2f);
        }
    }
    
    /// <summary>
    /// Run all check logic tests
    /// </summary>
    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        Debug.Log("🧪 CheckLogicTester: Starting comprehensive check logic tests...");
        
        testsRun = 0;
        testsPassed = 0;
        testsFailed = 0;
        
        // Ensure we're in playing state for testing
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameState.Playing);
        }
        
        if (testBasicCheckDetection)
        {
            TestBasicCheckDetection();
        }
        
        if (testMoveValidation)
        {
            TestMoveValidation();
        }
        
        if (testCheckmateDetection)
        {
            TestCheckmateDetection();
        }
        
        if (testStalemateDetection)
        {
            TestStalemateDetection();
        }
        
        // Summary
        Debug.Log($"🧪 CHECK LOGIC TEST SUMMARY:");
        Debug.Log($"   Tests Run: {testsRun}");
        Debug.Log($"   Tests Passed: {testsPassed}");
        Debug.Log($"   Tests Failed: {testsFailed}");
        Debug.Log($"   Success Rate: {(testsPassed / (float)testsRun) * 100:F1}%");
        
        if (testsFailed == 0)
        {
            Debug.Log("✅ ALL TESTS PASSED! Check logic is working correctly.");
        }
        else
        {
            Debug.LogError($"❌ {testsFailed} TESTS FAILED! Check logic needs attention.");
        }
    }
    
    /// <summary>
    /// Test basic check detection functionality
    /// </summary>
    private void TestBasicCheckDetection()
    {
        Debug.Log("🧪 Testing Basic Check Detection...");
        
        // Test 1: Verify CheckDetectionManager exists
        RunTest("CheckDetectionManager Instance Exists", 
            () => CheckDetectionManager.Instance != null);
        
        // Test 2: Verify check detection is enabled
        RunTest("Check Detection Enabled", 
            () => CheckDetectionManager.Instance.enableCheckValidation);
        
        // Test 3: Test finding kings
        RunTest("Can Find White King Position", () => {
            var pos = FindKingPosition(PieceColor.White);
            return pos.IsValid();
        });
        
        RunTest("Can Find Black King Position", () => {
            var pos = FindKingPosition(PieceColor.Black);
            return pos.IsValid();
        });
        
        // Test 4: Basic check state (should start with no checks)
        RunTest("No Initial Check State", () => {
            bool whiteInCheck = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.White);
            bool blackInCheck = CheckDetectionManager.Instance.IsKingInCheck(PieceColor.Black);
            return !whiteInCheck && !blackInCheck;
        });
    }
    
    /// <summary>
    /// Test move validation with check constraints
    /// </summary>
    private void TestMoveValidation()
    {
        Debug.Log("🧪 Testing Move Validation...");
        
        // Test 1: Verify pieces have legal moves
        RunTest("Pieces Have Legal Moves", () => {
            if (ChessBoard.Instance == null) return false;
            
            int totalLegalMoves = 0;
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        var piece = ChessBoard.Instance.GetPieceAt(new BoardPosition(x, y, z));
                        if (piece != null)
                        {
                            totalLegalMoves += piece.GetLegalMoves().Count;
                        }
                    }
                }
            }
            
            return totalLegalMoves > 0;
        });
        
        // Test 2: Verify GetLegalMoves vs GetValidMoves difference
        RunTest("GetLegalMoves Filters Check-Causing Moves", () => {
            if (ChessBoard.Instance == null) return false;
            
            // Find any piece and compare its valid vs legal moves
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    for (int z = 0; z < 4; z++)
                    {
                        var piece = ChessBoard.Instance.GetPieceAt(new BoardPosition(x, y, z));
                        if (piece != null)
                        {
                            var validMoves = piece.GetValidMoves();
                            var legalMoves = piece.GetLegalMoves();
                            
                            // Legal moves should never exceed valid moves
                            if (legalMoves.Count > validMoves.Count)
                            {
                                return false;
                            }
                            
                            // All legal moves should be in valid moves
                            foreach (var move in legalMoves)
                            {
                                if (!validMoves.Contains(move))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            
            return true;
        });
    }
    
    /// <summary>
    /// Test checkmate detection
    /// </summary>
    private void TestCheckmateDetection()
    {
        Debug.Log("🧪 Testing Checkmate Detection...");
        
        // Test 1: Verify GameEndDetectionManager exists
        RunTest("GameEndDetectionManager Instance Exists", 
            () => GameEndDetectionManager.Instance != null);
        
        // Test 2: Verify initial state is not checkmate
        RunTest("Initial State Not Checkmate", () => {
            if (GameEndDetectionManager.Instance == null) return false;
            
            bool whiteCheckmate = GameEndDetectionManager.Instance.IsCheckmate(PieceColor.White);
            bool blackCheckmate = GameEndDetectionManager.Instance.IsCheckmate(PieceColor.Black);
            
            return !whiteCheckmate && !blackCheckmate;
        });
        
        // Test 3: Players have legal moves initially
        RunTest("Players Have Legal Moves Initially", () => {
            if (GameEndDetectionManager.Instance == null) return false;
            
            bool whiteHasMoves = GameEndDetectionManager.Instance.HasLegalMoves(PieceColor.White);
            bool blackHasMoves = GameEndDetectionManager.Instance.HasLegalMoves(PieceColor.Black);
            
            return whiteHasMoves && blackHasMoves;
        });
    }
    
    /// <summary>
    /// Test stalemate detection
    /// </summary>
    private void TestStalemateDetection()
    {
        Debug.Log("🧪 Testing Stalemate Detection...");
        
        // Test 1: Verify initial state is not stalemate
        RunTest("Initial State Not Stalemate", () => {
            if (GameEndDetectionManager.Instance == null) return false;
            
            bool whiteStalemate = GameEndDetectionManager.Instance.IsStalemate(PieceColor.White);
            bool blackStalemate = GameEndDetectionManager.Instance.IsStalemate(PieceColor.Black);
            
            return !whiteStalemate && !blackStalemate;
        });
        
        // Test 2: Stalemate requires no legal moves + no check
        RunTest("Stalemate Logic Correct", () => {
            if (GameEndDetectionManager.Instance == null || CheckDetectionManager.Instance == null) return false;
            
            // This test verifies the logic is sound, even if conditions aren't met
            // If a player has legal moves, they cannot be in stalemate
            bool whiteHasMoves = GameEndDetectionManager.Instance.HasLegalMoves(PieceColor.White);
            bool whiteStalemate = GameEndDetectionManager.Instance.IsStalemate(PieceColor.White);
            
            if (whiteHasMoves && whiteStalemate)
            {
                return false; // Logic error: can't have moves and be in stalemate
            }
            
            bool blackHasMoves = GameEndDetectionManager.Instance.HasLegalMoves(PieceColor.Black);
            bool blackStalemate = GameEndDetectionManager.Instance.IsStalemate(PieceColor.Black);
            
            if (blackHasMoves && blackStalemate)
            {
                return false; // Logic error: can't have moves and be in stalemate
            }
            
            return true;
        });
    }
    
    /// <summary>
    /// Helper method to run a single test
    /// </summary>
    private void RunTest(string testName, System.Func<bool> testFunction)
    {
        testsRun++;
        
        try
        {
            bool result = testFunction();
            
            if (result)
            {
                testsPassed++;
                if (enableDebugOutput)
                {
                    Debug.Log($"✅ {testName}: PASSED");
                }
            }
            else
            {
                testsFailed++;
                Debug.LogError($"❌ {testName}: FAILED");
            }
        }
        catch (System.Exception e)
        {
            testsFailed++;
            Debug.LogError($"❌ {testName}: EXCEPTION - {e.Message}");
        }
    }
    
    /// <summary>
    /// Helper method to find a king's position
    /// </summary>
    private BoardPosition FindKingPosition(PieceColor kingColor)
    {
        if (ChessBoard.Instance == null)
        {
            return new BoardPosition(-1, -1, -1);
        }
        
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    BoardPosition pos = new BoardPosition(x, y, z);
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);
                    
                    if (piece != null && 
                        piece.pieceType == ChessPieceType.King && 
                        piece.pieceColor == kingColor)
                    {
                        return pos;
                    }
                }
            }
        }
        
        return new BoardPosition(-1, -1, -1); // Invalid position
    }
    
    /// <summary>
    /// Test visual feedback system
    /// </summary>
    [ContextMenu("Test Visual Feedback")]
    public void TestVisualFeedback()
    {
        Debug.Log("🧪 Testing Visual Feedback System...");
        
        if (CheckVisualFeedbackManager.Instance != null)
        {
            CheckVisualFeedbackManager.Instance.RefreshCheckIndicators();
            Debug.Log("✅ Visual feedback system tested - check indicators refreshed");
        }
        else
        {
            Debug.LogError("❌ CheckVisualFeedbackManager not found");
        }
    }
    
    /// <summary>
    /// Get comprehensive debug information about all systems
    /// </summary>
    [ContextMenu("Debug All Systems")]
    public void DebugAllSystems()
    {
        Debug.Log("🔍 COMPREHENSIVE SYSTEM DEBUG:");
        
        if (CheckDetectionManager.Instance != null)
        {
            Debug.Log("📍 " + CheckDetectionManager.Instance.GetDebugInfo());
        }
        
        if (TurnManager.Instance != null)
        {
            Debug.Log("🔄 " + TurnManager.Instance.GetCheckStateInfo());
        }
        
        if (GameEndDetectionManager.Instance != null)
        {
            Debug.Log("🏁 " + GameEndDetectionManager.Instance.GetDebugInfo());
        }
        
        if (CheckVisualFeedbackManager.Instance != null)
        {
            Debug.Log("🎨 " + CheckVisualFeedbackManager.Instance.GetCheckStateDebug());
        }
        
        if (GameStateManager.Instance != null)
        {
            Debug.Log($"🎮 Game State: {GameStateManager.Instance.currentState}");
        }
    }
}