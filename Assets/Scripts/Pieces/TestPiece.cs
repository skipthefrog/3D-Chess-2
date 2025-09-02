using System.Collections.Generic;
using UnityEngine;

public class TestPiece : ChessPiece
{
    [Header("Test Piece Settings")]
    public int moveRange = 1;
    
    public override List<BoardPosition> GetValidMoves()
    {
        List<BoardPosition> validMoves = new List<BoardPosition>();
        
        // Test piece can move in any direction within its range (like a 3D king)
        for (int dx = -moveRange; dx <= moveRange; dx++)
        {
            for (int dy = -moveRange; dy <= moveRange; dy++)
            {
                for (int dz = -moveRange; dz <= moveRange; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;
                    
                    BoardPosition newPosition = new BoardPosition(
                        currentPosition.x + dx,
                        currentPosition.y + dy,
                        currentPosition.z + dz
                    );
                    
                    if (IsValidMove(newPosition))
                    {
                        validMoves.Add(newPosition);
                    }
                }
            }
        }
        
        return validMoves;
    }
    
    protected override void Start()
    {
        base.Start();
        pieceType = ChessPieceType.Pawn; // Using Pawn as placeholder for test piece
    }
}