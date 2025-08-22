using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<BoardPosition> GetValidMoves()
    {
        List<BoardPosition> validMoves = new List<BoardPosition>();
        
        // King can move one cell in any direction (26 possible moves in 3D)
        // This includes all adjacent cells: straight, diagonal, and 3D diagonal moves
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    // Skip the current position
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
        pieceType = ChessPieceType.King;
    }
}