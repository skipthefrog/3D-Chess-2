using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D Chess Pawn that moves forward and captures in any of 8 forward positions.
/// Can move forward to empty space, or capture any enemy piece in the 8 adjacent
/// positions in the forward X plane (but not the direct forward position).
/// White pawns advance +X (towards X=3), Black pawns advance -X (towards X=0).
/// </summary>
public class Pawn : ChessPiece
{
    public override List<BoardPosition> GetValidMoves()
    {
        List<BoardPosition> validMoves = new List<BoardPosition>();
        
        // Determine forward direction based on piece color
        int forwardX = (pieceColor == PieceColor.White) ? 1 : -1;
        
        // Forward movement (non-capture only) - can only move if space is empty
        BoardPosition forwardMove = new BoardPosition(
            currentPosition.x + forwardX,
            currentPosition.y,
            currentPosition.z
        );
        
        if (forwardMove.IsValid())
        {
            ChessPiece pieceAhead = ChessBoard.Instance?.GetPieceAt(forwardMove);
            if (pieceAhead == null) // Can only move forward if empty
            {
                validMoves.Add(forwardMove);
            }
        }
        
        // Capture moves in all 8 surrounding positions in the forward plane
        // Pawn can capture any enemy piece in the 8 adjacent positions in the forward X plane
        Vector3Int[] captureDirections = {
            new Vector3Int(forwardX, -1, -1), // Forward + Down + Z-
            new Vector3Int(forwardX, -1, 0),  // Forward + Down
            new Vector3Int(forwardX, -1, 1),  // Forward + Down + Z+
            new Vector3Int(forwardX, 0, -1),  // Forward + Z-
            // Skip (forwardX, 0, 0) - that's the forward move position, handled separately
            new Vector3Int(forwardX, 0, 1),   // Forward + Z+
            new Vector3Int(forwardX, 1, -1),  // Forward + Up + Z-
            new Vector3Int(forwardX, 1, 0),   // Forward + Up
            new Vector3Int(forwardX, 1, 1),   // Forward + Up + Z+
        };
        
        foreach (Vector3Int direction in captureDirections)
        {
            BoardPosition capturePosition = new BoardPosition(
                currentPosition.x + direction.x,
                currentPosition.y + direction.y,
                currentPosition.z + direction.z
            );
            
            if (capturePosition.IsValid())
            {
                ChessPiece pieceAtCapture = ChessBoard.Instance?.GetPieceAt(capturePosition);
                // Can only capture enemy pieces, not move to empty spaces
                if (pieceAtCapture != null && pieceAtCapture.pieceColor != pieceColor)
                {
                    validMoves.Add(capturePosition);
                }
            }
        }
        
        return validMoves;
    }
    
    /// <summary>
    /// Check if moving to the target position would result in pawn promotion
    /// </summary>
    public bool IsPromotionMove(BoardPosition targetPosition)
    {
        if (pieceColor == PieceColor.White)
        {
            // White pawns promote when reaching X=3 (opponent's side)
            return targetPosition.x == 3;
        }
        else
        {
            // Black pawns promote when reaching X=0 (opponent's side)
            return targetPosition.x == 0;
        }
    }
    
    /// <summary>
    /// Override to flag promotion moves for special handling
    /// </summary>
    public override bool CanMoveTo(BoardPosition targetPosition)
    {
        bool canMove = base.CanMoveTo(targetPosition);
        
        if (canMove && IsPromotionMove(targetPosition))
        {
            Debug.Log($"Pawn promotion detected: {pieceColor} pawn moving to {targetPosition}");
        }
        
        return canMove;
    }
    
    protected override void Start()
    {
        base.Start();
        pieceType = ChessPieceType.Pawn;
    }
}