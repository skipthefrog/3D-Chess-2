using UnityEngine;

/// <summary>
/// Component that stores board position data for move target indicators
/// Used to identify which board position a move indicator represents
/// </summary>
public class MoveTargetData : MonoBehaviour
{
    [Header("Move Target Information")]
    public BoardPosition targetPosition;
    
    private void Start()
    {
        Debug.Log($"MoveTargetData: Created for position {targetPosition} on GameObject {gameObject.name}");
    }
    
    /// <summary>
    /// Get the board position this move target represents
    /// </summary>
    public BoardPosition GetTargetPosition()
    {
        return targetPosition;
    }
}