using UnityEngine;
using System.Collections.Generic;

public class ChessBoardTester : MonoBehaviour
{
    [Header("Testing Controls")]
    public bool runTestsOnStart = true;
    public bool verboseLogging = true;
    
    private void Start()
    {
        if (runTestsOnStart)
        {
            // Delay tests to ensure board is initialized
            Invoke(nameof(RunAllTests), 1f);
        }
    }
    
    public void RunAllTests()
    {
        Debug.Log("=== Starting Chess Board Tests ===");
        
        TestBoardPositionValidation();
        TestCoordinateConversion();
        TestBoardCellGeneration();
        TestPiecePlacement();
        TestMovementValidation();
        
        Debug.Log("=== Chess Board Tests Complete ===");
    }
    
    private void TestBoardPositionValidation()
    {
        Debug.Log("Testing BoardPosition validation...");
        
        // Valid positions
        Assert(new BoardPosition(0, 0, 0).IsValid(), "Position (0,0,0) should be valid");
        Assert(new BoardPosition(3, 3, 3).IsValid(), "Position (3,3,3) should be valid");
        Assert(new BoardPosition(1, 2, 0).IsValid(), "Position (1,2,0) should be valid");
        
        // Invalid positions
        Assert(!new BoardPosition(-1, 0, 0).IsValid(), "Position (-1,0,0) should be invalid");
        Assert(!new BoardPosition(4, 0, 0).IsValid(), "Position (4,0,0) should be invalid");
        Assert(!new BoardPosition(0, -1, 0).IsValid(), "Position (0,-1,0) should be invalid");
        Assert(!new BoardPosition(0, 0, 4).IsValid(), "Position (0,0,4) should be invalid");
        
        Debug.Log("BoardPosition validation tests passed!");
    }
    
    private void TestCoordinateConversion()
    {
        Debug.Log("Testing coordinate conversion...");
        
        if (ChessBoard.Instance == null)
        {
            Debug.LogError("ChessBoard instance not found!");
            return;
        }
        
        BoardPosition testPos = new BoardPosition(2, 1, 3);
        Vector3 worldPos = ChessBoard.Instance.BoardToWorld(testPos);
        BoardPosition convertedPos = ChessBoard.Instance.WorldToBoard(worldPos);
        
        Assert(testPos == convertedPos, $"Coordinate conversion failed: {testPos} != {convertedPos}");
        
        Debug.Log("Coordinate conversion tests passed!");
    }
    
    private void TestBoardCellGeneration()
    {
        Debug.Log("Testing board cell generation...");
        
        if (ChessBoard.Instance == null)
        {
            Debug.LogError("ChessBoard instance not found!");
            return;
        }
        
        List<BoardPosition> allPositions = ChessBoard.Instance.GetAllPositions();
        Assert(allPositions.Count == 64, $"Expected 64 positions, got {allPositions.Count}");
        
        // Check that all positions are unique
        HashSet<BoardPosition> uniquePositions = new HashSet<BoardPosition>(allPositions);
        Assert(uniquePositions.Count == 64, "All board positions should be unique");
        
        Debug.Log("Board cell generation tests passed!");
    }
    
    private void TestPiecePlacement()
    {
        Debug.Log("Testing piece placement...");
        
        if (ChessBoard.Instance == null)
        {
            Debug.LogError("ChessBoard instance not found!");
            return;
        }
        
        BoardPosition testPos = new BoardPosition(0, 1, 2);
        
        // Initially should be empty
        ChessPiece pieceAt = ChessBoard.Instance.GetPieceAt(testPos);
        Assert(pieceAt == null, "Position should initially be empty");
        
        // Create a test piece
        GameObject testPieceObject = new GameObject("Test Piece");
        TestPiece testPiece = testPieceObject.AddComponent<TestPiece>();
        
        // Place the piece
        bool placed = ChessBoard.Instance.SetPieceAt(testPos, testPiece);
        Assert(placed, "Piece should be placeable on empty square");
        
        // Verify piece is there
        ChessPiece retrievedPiece = ChessBoard.Instance.GetPieceAt(testPos);
        Assert(retrievedPiece == testPiece, "Retrieved piece should match placed piece");
        
        // Clean up
        if (testPieceObject != null)
        {
            DestroyImmediate(testPieceObject);
        }
        
        Debug.Log("Piece placement tests passed!");
    }
    
    private void TestMovementValidation()
    {
        Debug.Log("Testing movement validation...");
        
        if (ChessBoard.Instance == null)
        {
            Debug.LogError("ChessBoard instance not found!");
            return;
        }
        
        // Create a test piece
        GameObject testPieceObject = new GameObject("Movement Test Piece");
        TestPiece testPiece = testPieceObject.AddComponent<TestPiece>();
        
        BoardPosition startPos = new BoardPosition(1, 1, 1);
        testPiece.Initialize(startPos, PieceColor.White);
        
        // Test valid moves
        List<BoardPosition> validMoves = testPiece.GetValidMoves();
        Assert(validMoves.Count > 0, "Test piece should have valid moves");
        
        if (verboseLogging)
        {
            Debug.Log($"Test piece at {startPos} has {validMoves.Count} valid moves:");
            foreach (BoardPosition move in validMoves)
            {
                Debug.Log($"  - {move}");
            }
        }
        
        // Test that piece can move to a valid position
        if (validMoves.Count > 0)
        {
            BoardPosition targetPos = validMoves[0];
            bool canMove = testPiece.CanMoveTo(targetPos);
            Assert(canMove, $"Piece should be able to move to {targetPos}");
        }
        
        // Test invalid move (out of bounds)
        BoardPosition invalidPos = new BoardPosition(-1, -1, -1);
        bool canMoveInvalid = testPiece.CanMoveTo(invalidPos);
        Assert(!canMoveInvalid, "Piece should not be able to move out of bounds");
        
        // Clean up
        if (testPieceObject != null)
        {
            DestroyImmediate(testPieceObject);
        }
        
        Debug.Log("Movement validation tests passed!");
    }
    
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError($"ASSERTION FAILED: {message}");
        }
        else if (verboseLogging)
        {
            Debug.Log($"✓ {message}");
        }
    }
    
    [ContextMenu("Run Tests")]
    public void RunTestsFromContextMenu()
    {
        RunAllTests();
    }
}