using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D Chess Pawn that moves forward and captures in any of 8 forward positions.
/// Can move forward to empty space, or capture any enemy piece in the 8 adjacent
/// positions in the forward plane (but not the direct forward position).
/// Forward direction depends on color:
/// - White/Black: Move along X-axis
/// - Green/Purple: Move along Z-axis
/// - Yellow/Orange: Move along Y-axis
/// </summary>
public class Pawn : ChessPiece
{
    /// <summary>
    /// Get the forward direction vector for this pawn based on its color
    /// </summary>
    private Vector3Int GetForwardDirection()
    {
        switch (pieceColor)
        {
            case PieceColor.White:
                return new Vector3Int(1, 0, 0);   // +X direction
            case PieceColor.Black:
                return new Vector3Int(-1, 0, 0);  // -X direction
            case PieceColor.Green:
                return new Vector3Int(0, 0, 1);   // +Z direction
            case PieceColor.Purple:
                return new Vector3Int(0, 0, -1);  // -Z direction
            case PieceColor.Yellow:
                return new Vector3Int(0, 1, 0);   // +Y direction
            case PieceColor.Orange:
                return new Vector3Int(0, -1, 0);  // -Y direction
            default:
                Debug.LogWarning($"Pawn.GetForwardDirection: Unknown color {pieceColor}, defaulting to +X");
                return new Vector3Int(1, 0, 0);
        }
    }

    /// <summary>
    /// Get the 8 capture directions around the forward position
    /// The 8 positions surround the forward square in the plane perpendicular to movement
    /// </summary>
    private Vector3Int[] GetCaptureDirections(Vector3Int forwardDir)
    {
        List<Vector3Int> directions = new List<Vector3Int>();

        // Determine which axis is the forward axis
        if (forwardDir.x != 0)
        {
            // Moving along X-axis (White/Black), capture in 8 positions around forward X
            // Vary Y and Z in the forward plane
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    // Skip the center (direct forward) position - pawns can't capture there
                    if (y == 0 && z == 0) continue;

                    directions.Add(new Vector3Int(forwardDir.x, y, z));
                }
            }
        }
        else if (forwardDir.z != 0)
        {
            // Moving along Z-axis (Green/Purple), capture in 8 positions around forward Z
            // Vary X and Y in the forward plane
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    // Skip the center (direct forward) position
                    if (x == 0 && y == 0) continue;

                    directions.Add(new Vector3Int(x, y, forwardDir.z));
                }
            }
        }
        else if (forwardDir.y != 0)
        {
            // Moving along Y-axis (Yellow/Orange), capture in 8 positions around forward Y
            // Vary X and Z in the forward plane
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    // Skip the center (direct forward) position
                    if (x == 0 && z == 0) continue;

                    directions.Add(new Vector3Int(x, forwardDir.y, z));
                }
            }
        }

        return directions.ToArray();
    }

    public override List<BoardPosition> GetValidMoves()
    {
        List<BoardPosition> validMoves = new List<BoardPosition>();

        // Determine forward direction based on piece color
        Vector3Int forwardDir = GetForwardDirection();

        // Forward movement (non-capture only) - can only move if space is empty
        BoardPosition forwardMove = new BoardPosition(
            currentPosition.x + forwardDir.x,
            currentPosition.y + forwardDir.y,
            currentPosition.z + forwardDir.z
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
        // The 8 capture positions depend on which axis we're moving along
        Vector3Int[] captureDirections = GetCaptureDirections(forwardDir);
        
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
    /// Pawns promote when reaching the opposite side of the board along their movement axis
    /// </summary>
    public bool IsPromotionMove(BoardPosition targetPosition)
    {
        // Get board dimensions dynamically
        Vector3Int boardDimensions = BoardDimensionsManager.Instance != null
            ? BoardDimensionsManager.Instance.GetDimensions()
            : new Vector3Int(4, 4, 4);

        switch (pieceColor)
        {
            case PieceColor.White:
                // White pawns promote when reaching maximum X (opponent's side)
                return targetPosition.x == boardDimensions.x - 1;

            case PieceColor.Black:
                // Black pawns promote when reaching X=0 (opponent's side)
                return targetPosition.x == 0;

            case PieceColor.Green:
                // Green pawns promote when reaching maximum Z (opponent's side)
                return targetPosition.z == boardDimensions.z - 1;

            case PieceColor.Purple:
                // Purple pawns promote when reaching Z=0 (opponent's side)
                return targetPosition.z == 0;

            case PieceColor.Yellow:
                // Yellow pawns promote when reaching maximum Y (opponent's side)
                return targetPosition.y == boardDimensions.y - 1;

            case PieceColor.Orange:
                // Orange pawns promote when reaching Y=0 (opponent's side)
                return targetPosition.y == 0;

            default:
                Debug.LogWarning($"IsPromotionMove: Unknown color {pieceColor}");
                return false;
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