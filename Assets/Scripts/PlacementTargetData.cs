using UnityEngine;

/// <summary>
/// Component that stores board position data for placement target indicators
/// Used to identify which board position a placement indicator represents
/// </summary>
public class PlacementTargetData : MonoBehaviour
{
    [Header("Placement Target Information")]
    public BoardPosition targetPosition;
    
    private void Start()
    {
        // Component initialized for placement target at {targetPosition}
    }
    
    /// <summary>
    /// Get the board position this placement target represents
    /// </summary>
    public BoardPosition GetTargetPosition()
    {
        return targetPosition;
    }
}