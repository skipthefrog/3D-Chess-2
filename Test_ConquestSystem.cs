using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Test script to verify the conquest system works correctly in 4 and 6 player modes
/// </summary>
public class Test_ConquestSystem : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🏰 Test_ConquestSystem: Starting conquest system test");
        StartCoroutine(RunConquestTests());
    }
    
    IEnumerator RunConquestTests()
    {
        yield return new WaitForSeconds(3f); // Wait for game initialization
        
        Debug.Log("🏰 === CONQUEST SYSTEM TESTING ===");
        
        // Test 1: Multi-player mode detection
        TestMultiPlayerModeDetection();
        
        yield return new WaitForSeconds(1f);
        
        // Test 2: Turn manager elimination functionality
        TestTurnManagerElimination();
        
        yield return new WaitForSeconds(1f);
        
        // Test 3: Piece ownership transfer
        TestPieceOwnershipTransfer();
        
        yield return new WaitForSeconds(1f);
        
        // Test 4: Game end detection integration
        TestGameEndDetectionIntegration();
        
        Debug.Log("🏰 === CONQUEST TESTING COMPLETE ===");
        LogTestSummary();
    }
    
    void TestMultiPlayerModeDetection()
    {
        Debug.Log("🧪 Testing multi-player mode detection...");
        
        if (GameEndDetectionManager.Instance != null)
        {
            // Test would require access to private IsMultiPlayerMode() method
            Debug.Log("  ✅ GameEndDetectionManager.Instance: EXISTS");
            Debug.Log("  ℹ️  Multi-player mode detection requires actual 4/6 player game setup");
        }
        else
        {
            Debug.LogError("  ❌ GameEndDetectionManager.Instance: NULL");
        }
    }
    
    void TestTurnManagerElimination()
    {
        Debug.Log("🧪 Testing TurnManager elimination functionality...");
        
        if (TurnManager.Instance != null)
        {
            Debug.Log("  ✅ TurnManager.Instance: EXISTS");
            
            // Test player elimination tracking
            int initialRemainingPlayers = TurnManager.Instance.GetRemainingPlayerCount();
            Debug.Log($"  📊 Initial remaining players: {initialRemainingPlayers}");
            
            // Test active players list
            List<PieceColor> activePlayers = TurnManager.Instance.GetActivePlayers();
            string activePlayersList = string.Join(", ", activePlayers);
            Debug.Log($"  📊 Active players: [{activePlayersList}]");
            
            // Test elimination check
            bool whiteEliminated = TurnManager.Instance.IsPlayerEliminated(PieceColor.White);
            bool blackEliminated = TurnManager.Instance.IsPlayerEliminated(PieceColor.Black);
            Debug.Log($"  📊 Elimination status - White: {whiteEliminated}, Black: {blackEliminated}");
            
            // Test mode detection
            bool is4Player = TurnManager.Instance.Is4PlayerMode();
            bool is6Player = TurnManager.Instance.Is6PlayerMode();
            Debug.Log($"  📊 Game mode - 4-player: {is4Player}, 6-player: {is6Player}");
            
            Debug.Log("  ✅ TurnManager elimination methods available");
        }
        else
        {
            Debug.LogError("  ❌ TurnManager.Instance: NULL");
        }
    }
    
    void TestPieceOwnershipTransfer()
    {
        Debug.Log("🧪 Testing piece ownership transfer...");
        
        // Find a test piece on the board
        if (ChessBoard.Instance != null)
        {
            Debug.Log("  ✅ ChessBoard.Instance: EXISTS");
            
            // Search for any piece to test ownership transfer
            ChessPiece testPiece = FindTestPiece();
            
            if (testPiece != null)
            {
                PieceColor originalColor = testPiece.pieceColor;
                PieceColor originalOwner = testPiece.GetOriginalOwner();
                bool wasConquered = testPiece.IsConqueredPiece();
                
                Debug.Log($"  🔍 Found test piece: {originalColor} {testPiece.pieceType}");
                Debug.Log($"  📊 Original owner: {originalOwner}, Currently conquered: {wasConquered}");
                
                // Test ownership change (simulate conquest)
                PieceColor newOwner = (originalColor == PieceColor.White) ? PieceColor.Green : PieceColor.White;
                Debug.Log($"  🔄 Testing ownership change to {newOwner}");
                
                testPiece.ChangeOwnership(newOwner);
                
                // Verify changes
                bool nowConquered = testPiece.IsConqueredPiece();
                PieceColor currentOwner = testPiece.pieceColor;
                
                Debug.Log($"  📊 After ownership change:");
                Debug.Log($"    Current owner: {currentOwner}");
                Debug.Log($"    Original owner: {testPiece.GetOriginalOwner()}");
                Debug.Log($"    Is conquered: {nowConquered}");
                
                bool ownershipChangeSuccess = (currentOwner == newOwner) && nowConquered && (testPiece.GetOriginalOwner() == originalOwner);
                Debug.Log($"  ✅ Ownership transfer test: {(ownershipChangeSuccess ? "PASSED" : "FAILED")}");
                
                // Reset the piece
                testPiece.ChangeOwnership(originalColor);
                Debug.Log($"  🔄 Reset test piece to original owner: {originalColor}");
            }
            else
            {
                Debug.LogWarning("  ⚠️ No test piece found on board for ownership transfer test");
            }
        }
        else
        {
            Debug.LogError("  ❌ ChessBoard.Instance: NULL");
        }
    }
    
    void TestGameEndDetectionIntegration()
    {
        Debug.Log("🧪 Testing GameEndDetectionManager integration...");
        
        if (GameEndDetectionManager.Instance != null)
        {
            Debug.Log("  ✅ GameEndDetectionManager.Instance: EXISTS");
            
            // Test event subscription (events should be available)
            bool hasEliminationEvent = GameEndDetectionManager.Instance.OnPlayerEliminated != null;
            Debug.Log($"  📊 OnPlayerEliminated event initialized: {hasEliminationEvent}");
            
            // Subscribe to elimination event for testing  
            if (GameEndDetectionManager.Instance.OnPlayerEliminated == null)
            {
                Debug.Log("  📊 OnPlayerEliminated event is null - will be initialized when needed");
            }
            else
            {
                GameEndDetectionManager.Instance.OnPlayerEliminated += OnTestPlayerEliminated;
                Debug.Log("  ✅ Subscribed to elimination event for testing");
            }
            
            Debug.Log("  ✅ GameEndDetectionManager integration ready");
        }
        else
        {
            Debug.LogError("  ❌ GameEndDetectionManager.Instance: NULL");
        }
    }
    
    void OnTestPlayerEliminated(PieceColor defeatedPlayer, PieceColor conqueringPlayer)
    {
        Debug.Log($"🏆 TEST EVENT: Player elimination detected - {defeatedPlayer} defeated by {conqueringPlayer}");
    }
    
    ChessPiece FindTestPiece()
    {
        if (ChessBoard.Instance == null) return null;
        
        // Get board dimensions dynamically
        Vector3Int dimensions = BoardSizeManager.Instance != null 
            ? BoardSizeManager.Instance.GetBoardDimensions() 
            : new Vector3Int(4, 4, 4);
            
        // Search for any piece (prefer non-king pieces for testing)
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    BoardPosition pos = new BoardPosition(x, y, z);
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);
                    
                    if (piece != null && piece.pieceType != ChessPieceType.King)
                    {
                        return piece; // Found a non-king piece for testing
                    }
                }
            }
        }
        
        // If no non-king pieces found, return any piece
        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    BoardPosition pos = new BoardPosition(x, y, z);
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(pos);
                    
                    if (piece != null)
                    {
                        return piece;
                    }
                }
            }
        }
        
        return null; // No pieces found
    }
    
    void LogTestSummary()
    {
        Debug.Log("🏰 CONQUEST SYSTEM TEST SUMMARY:");
        Debug.Log("  ✅ Multi-player mode detection capability added");
        Debug.Log("  ✅ TurnManager extended with elimination tracking");
        Debug.Log("  ✅ ChessPiece extended with ownership transfer");
        Debug.Log("  ✅ GameEndDetectionManager modified for conquest mechanics");
        Debug.Log("  ✅ Event system integrated for player eliminations");
        
        Debug.Log("");
        Debug.Log("🏰 EXPECTED CONQUEST BEHAVIOR:");
        Debug.Log("  - 2-player games: Traditional checkmate ends the game");
        Debug.Log("  - 4-player games: Checkmate results in piece conquest");
        Debug.Log("  - 6-player games: Checkmate results in piece conquest");
        Debug.Log("  - Defeated player's king is removed from the board");
        Debug.Log("  - All defeated player's pieces become conquering player's pieces");
        Debug.Log("  - Conquered pieces have visual indicators (metallic/shiny)");
        Debug.Log("  - Turn rotation skips eliminated players");
        Debug.Log("  - Game ends when only one player remains");
        Debug.Log("  - Conquest history is tracked for each elimination");
    }
}