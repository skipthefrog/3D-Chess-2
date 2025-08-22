using UnityEngine;
using System.Collections;

/// <summary>
/// Test script to verify the piece movement and synchronization fixes
/// </summary>
public class TestMovementSystem : MonoBehaviour
{
    [Header("Test Settings")]
    public bool autoRunTest = false; // DISABLED: Prevent automatic test execution during gameplay
    public float testDelay = 1f;
    
    private void Start()
    {
        if (autoRunTest)
        {
            StartCoroutine(RunMovementTest());
        }
    }
    
    private System.Collections.IEnumerator RunMovementTest()
    {
        Debug.Log("🧪 TestMovementSystem: Waiting for game initialization...");
        yield return new WaitForSeconds(testDelay);
        
        // Wait for ChessBoard to be ready
        while (ChessBoard.Instance == null)
        {
            Debug.Log("🧪 TestMovementSystem: Waiting for ChessBoard.Instance...");
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.Log("🧪 TestMovementSystem: Starting automated movement test...");
        
        // Find a test piece to move
        ChessPiece[] allPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
        Debug.Log($"🧪 Found {allPieces.Length} pieces for testing");
        
        if (allPieces.Length == 0)
        {
            Debug.LogError("🧪 TestMovementSystem: No pieces found! Cannot run test.");
            yield break;
        }
        
        ChessPiece testPiece = allPieces[0];
        Debug.Log($"🧪 TestMovementSystem: Using {testPiece.pieceColor} {testPiece.pieceType} for test");
        Debug.Log($"🧪 Initial piece position: {testPiece.CurrentPosition}");
        Debug.Log($"🧪 Initial transform position: {testPiece.transform.localPosition}");
        
        // Dump initial board state
        if (ChessBoard.Instance != null)
        {
            ChessBoard.Instance.DebugDumpBoardState("BEFORE TEST MOVE");
        }
        
        // Get valid moves for this piece
        var validMoves = testPiece.GetValidMoves();
        Debug.Log($"🧪 Piece has {validMoves.Count} valid moves: [{string.Join(", ", validMoves)}]");
        
        if (validMoves.Count == 0)
        {
            Debug.LogError("🧪 TestMovementSystem: Test piece has no valid moves! Cannot test movement.");
            yield break;
        }
        
        // Attempt to move to the first valid position
        BoardPosition targetPosition = validMoves[0];
        Debug.Log($"🧪 TestMovementSystem: Attempting automated move to {targetPosition}");
        
        // Test the movement using ChessBoard.MovePiece directly (bypasses input system)
        if (ChessBoard.Instance != null)
        {
            bool moveResult = ChessBoard.Instance.MovePiece(testPiece.CurrentPosition, targetPosition);
            Debug.Log($"🧪 ChessBoard.MovePiece returned: {moveResult}");
            
            // Wait a bit for animation to start
            yield return new WaitForSeconds(0.5f);
            
            // Check if piece is moving
            Debug.Log($"🧪 Piece.IsMoving: {testPiece.IsMoving}");
            Debug.Log($"🧪 Piece currentPosition after move: {testPiece.CurrentPosition}");
            Debug.Log($"🧪 Transform position during move: {testPiece.transform.localPosition}");
            
            // Wait for move to complete
            float timeout = 10f; // 10 second timeout
            float elapsed = 0f;
            
            while (testPiece.IsMoving && elapsed < timeout)
            {
                Debug.Log($"🧪 Waiting for move to complete... elapsed={elapsed:F1}s, transform={testPiece.transform.localPosition}");
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogError("🧪 TestMovementSystem: Move timed out after 10 seconds!");
            }
            else
            {
                Debug.Log($"🧪 TestMovementSystem: Move completed in {elapsed:F1} seconds");
                Debug.Log($"🧪 Final piece position: {testPiece.CurrentPosition}");
                Debug.Log($"🧪 Final transform position: {testPiece.transform.localPosition}");
                
                // Verify the move was successful
                if (testPiece.CurrentPosition == targetPosition)
                {
                    Debug.Log("✅ 🧪 TEST PASSED: Piece position updated correctly!");
                    
                    // Check if piece is at expected world position
                    Vector3 expectedLocalPos = ChessBoard.Instance.BoardToLocalPosition(targetPosition);
                    Vector3 actualLocalPos = testPiece.transform.localPosition;
                    float positionError = Vector3.Distance(expectedLocalPos, actualLocalPos);
                    
                    Debug.Log($"🧪 Expected local position: {expectedLocalPos}");
                    Debug.Log($"🧪 Actual local position: {actualLocalPos}");
                    Debug.Log($"🧪 Position error: {positionError:F3} units");
                    
                    if (positionError < 0.1f)
                    {
                        Debug.Log("✅ 🧪 TEST PASSED: Piece visual position is correct!");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ 🧪 TEST WARNING: Piece visual position has error of {positionError:F3} units");
                    }
                }
                else
                {
                    Debug.LogError($"❌ 🧪 TEST FAILED: Piece position incorrect! Expected {targetPosition}, got {testPiece.CurrentPosition}");
                }
                
                // Dump final board state
                ChessBoard.Instance.DebugDumpBoardState("AFTER TEST MOVE");
            }
        }
        else
        {
            Debug.LogError("🧪 TestMovementSystem: ChessBoard.Instance is null! Cannot test movement.");
        }
        
        Debug.Log("🧪 TestMovementSystem: Automated test completed.");
    }
    
    private System.Collections.IEnumerator RunCaptureTest()
    {
        Debug.Log("🧪 TestMovementSystem: Starting capture test...");
        
        // Wait for game to be ready
        while (ChessBoard.Instance == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Find pieces of different colors for capture test
        ChessPiece[] allPieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
        ChessPiece whitePiece = null;
        ChessPiece blackPiece = null;
        
        foreach (ChessPiece piece in allPieces)
        {
            if (piece.pieceColor == PieceColor.White && whitePiece == null)
                whitePiece = piece;
            else if (piece.pieceColor == PieceColor.Black && blackPiece == null)
                blackPiece = piece;
                
            if (whitePiece != null && blackPiece != null)
                break;
        }
        
        if (whitePiece == null || blackPiece == null)
        {
            Debug.LogError("🧪 TestMovementSystem: Cannot run capture test - need both white and black pieces");
            yield break;
        }
        
        Debug.Log($"🧪 TestMovementSystem: Testing capture with {whitePiece.pieceColor} {whitePiece.pieceType} at {whitePiece.CurrentPosition}");
        Debug.Log($"🧪 TestMovementSystem: Target: {blackPiece.pieceColor} {blackPiece.pieceType} at {blackPiece.CurrentPosition}");
        
        // Try to move white piece to capture black piece position
        BoardPosition capturePosition = blackPiece.CurrentPosition;
        
        // Check if the white piece can move to capture the black piece
        var validMoves = whitePiece.GetValidMoves();
        bool canCapture = validMoves.Contains(capturePosition);
        
        Debug.Log($"🧪 TestMovementSystem: Can {whitePiece.pieceColor} {whitePiece.pieceType} capture at {capturePosition}? {canCapture}");
        
        if (!canCapture)
        {
            Debug.Log($"🧪 TestMovementSystem: Cannot test capture - {whitePiece.pieceColor} piece cannot reach {blackPiece.pieceColor} piece");
            
            // Show distance
            float distance = whitePiece.CurrentPosition.DistanceTo(blackPiece.CurrentPosition);
            Debug.Log($"🧪 TestMovementSystem: Distance between pieces: {distance:F2}");
            Debug.Log($"🧪 TestMovementSystem: White piece valid moves: [{string.Join(", ", validMoves)}]");
        }
        else
        {
            Debug.Log($"🧪 TestMovementSystem: Attempting capture move...");
            
            // Count pieces before capture
            int piecesBefore = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None).Length;
            Debug.Log($"🧪 TestMovementSystem: Pieces before capture: {piecesBefore}");
            
            // Perform the capture move
            bool moveResult = ChessBoard.Instance.MovePiece(whitePiece.CurrentPosition, capturePosition);
            Debug.Log($"🧪 TestMovementSystem: Capture move result: {moveResult}");
            
            // Wait a bit for any cleanup
            yield return new WaitForSeconds(1f);
            
            // Count pieces after capture
            int piecesAfter = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None).Length;
            Debug.Log($"🧪 TestMovementSystem: Pieces after capture: {piecesAfter}");
            
            if (piecesAfter == piecesBefore - 1)
            {
                Debug.Log("✅ 🧪 CAPTURE TEST PASSED: Captured piece was removed!");
            }
            else
            {
                Debug.LogError($"❌ 🧪 CAPTURE TEST FAILED: Expected {piecesBefore - 1} pieces, got {piecesAfter}");
            }
        }
        
        Debug.Log("🧪 TestMovementSystem: Capture test completed.");
    }
    
    private void Update()
    {
        // DISABLED: Test scripts should not run during normal gameplay
        // This was causing unnecessary performance load checking input every frame
        
        /*
        // Manual test trigger with T key
        if (InputHelper.GetKeyDown(KeyCode.T))
        {
            Debug.Log("🧪 TestMovementSystem: Manual test triggered with T key");
            StopAllCoroutines();
            StartCoroutine(RunMovementTest());
        }
        
        // Manual capture test trigger with C key
        if (InputHelper.GetKeyDown(KeyCode.C))
        {
            Debug.Log("🧪 TestMovementSystem: Manual capture test triggered with C key");
            StopAllCoroutines();
            StartCoroutine(RunCaptureTest());
        }
        */
    }
}