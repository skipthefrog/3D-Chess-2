using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Mathematical utilities for performing 3D rotations on the 4×4×4 chess board.
/// Handles position transformations for Rubik's cube-style rotations.
/// </summary>
public static class ChaosMath
{
    /// <summary>
    /// Rotate positions on a cube face (2D rotation within the face plane)
    /// </summary>
    /// <param name="positions">List of positions to rotate</param>
    /// <param name="face">Which face of the cube</param>
    /// <param name="clockwise">Direction of rotation</param>
    /// <param name="boardDimensions">Board size (4x4x4, 6x6x6, or 8x8x8)</param>
    /// <returns>Dictionary mapping old positions to new positions</returns>
    public static Dictionary<BoardPosition, BoardPosition> RotateFacePositions(List<BoardPosition> positions, CubeFace face, bool clockwise, Vector3Int boardDimensions)
    {
        Dictionary<BoardPosition, BoardPosition> rotationMap = new Dictionary<BoardPosition, BoardPosition>();

        foreach (BoardPosition pos in positions)
        {
            BoardPosition newPos = RotateSinglePosition(pos, face, clockwise, boardDimensions);
            rotationMap[pos] = newPos;
        }

        return rotationMap;
    }
    
    /// <summary>
    /// Rotate positions on a layer/slice (3D rotation around an axis)
    /// </summary>
    /// <param name="positions">List of positions to rotate</param>
    /// <param name="axis">Rotation axis</param>
    /// <param name="layer">Which layer (0 to boardSize-1)</param>
    /// <param name="clockwise">Direction of rotation</param>
    /// <param name="boardDimensions">Board size (4x4x4, 6x6x6, or 8x8x8)</param>
    /// <returns>Dictionary mapping old positions to new positions</returns>
    public static Dictionary<BoardPosition, BoardPosition> RotateLayerPositions(List<BoardPosition> positions, RotationAxis axis, int layer, bool clockwise, Vector3Int boardDimensions)
    {
        Dictionary<BoardPosition, BoardPosition> rotationMap = new Dictionary<BoardPosition, BoardPosition>();

        foreach (BoardPosition pos in positions)
        {
            BoardPosition newPos = RotateLayerPosition(pos, axis, layer, clockwise, boardDimensions);
            rotationMap[pos] = newPos;
        }

        return rotationMap;
    }
    
    /// <summary>
    /// Rotate a single position on a cube face
    /// </summary>
    private static BoardPosition RotateSinglePosition(BoardPosition pos, CubeFace face, bool clockwise, Vector3Int boardDimensions)
    {
        switch (face)
        {
            case CubeFace.Front: // Z = 0 plane, rotate in XY
                return RotateInPlane(pos.x, pos.y, pos.z, clockwise, PlaneType.XY, boardDimensions);

            case CubeFace.Back: // Z = max plane, rotate in XY
                return RotateInPlane(pos.x, pos.y, pos.z, clockwise, PlaneType.XY, boardDimensions);

            case CubeFace.Left: // X = 0 plane, rotate in YZ
                return RotateInPlane(pos.x, pos.y, pos.z, clockwise, PlaneType.YZ, boardDimensions);

            case CubeFace.Right: // X = max plane, rotate in YZ
                return RotateInPlane(pos.x, pos.y, pos.z, clockwise, PlaneType.YZ, boardDimensions);

            case CubeFace.Top: // Y = max plane, rotate in XZ
                return RotateInPlane(pos.x, pos.y, pos.z, clockwise, PlaneType.XZ, boardDimensions);

            case CubeFace.Bottom: // Y = 0 plane, rotate in XZ
                return RotateInPlane(pos.x, pos.y, pos.z, clockwise, PlaneType.XZ, boardDimensions);

            default:
                return pos; // No rotation
        }
    }
    
    /// <summary>
    /// Rotate a single position in a layer around an axis
    /// </summary>
    private static BoardPosition RotateLayerPosition(BoardPosition pos, RotationAxis axis, int layer, bool clockwise, Vector3Int boardDimensions)
    {
        switch (axis)
        {
            case RotationAxis.X: // Rotate around X axis (in YZ plane)
                return RotateInPlane(pos.x, pos.y, pos.z, clockwise, PlaneType.YZ, boardDimensions);

            case RotationAxis.Y: // Rotate around Y axis (in XZ plane)
                return RotateInPlane(pos.x, pos.y, pos.z, clockwise, PlaneType.XZ, boardDimensions);

            case RotationAxis.Z: // Rotate around Z axis (in XY plane)
                return RotateInPlane(pos.x, pos.y, pos.z, clockwise, PlaneType.XY, boardDimensions);

            default:
                return pos; // No rotation
        }
    }
    
    /// <summary>
    /// Types of 2D planes for rotation
    /// </summary>
    private enum PlaneType
    {
        XY, // Z is constant
        XZ, // Y is constant
        YZ  // X is constant
    }
    
    /// <summary>
    /// Rotate coordinates in a 2D plane within the board grid
    /// </summary>
    private static BoardPosition RotateInPlane(int x, int y, int z, bool clockwise, PlaneType plane, Vector3Int boardDimensions)
    {
        int maxCoord = boardDimensions.x - 1; // Assuming cubic board (same size in all dimensions)

        switch (plane)
        {
            case PlaneType.XY: // Rotate X and Y, Z stays constant
                {
                    Vector2 rotated = Rotate2D(new Vector2(x, y), clockwise, maxCoord);
                    return new BoardPosition(Mathf.RoundToInt(rotated.x), Mathf.RoundToInt(rotated.y), z);
                }

            case PlaneType.XZ: // Rotate X and Z, Y stays constant
                {
                    Vector2 rotated = Rotate2D(new Vector2(x, z), clockwise, maxCoord);
                    return new BoardPosition(Mathf.RoundToInt(rotated.x), y, Mathf.RoundToInt(rotated.y));
                }

            case PlaneType.YZ: // Rotate Y and Z, X stays constant
                {
                    Vector2 rotated = Rotate2D(new Vector2(y, z), clockwise, maxCoord);
                    return new BoardPosition(x, Mathf.RoundToInt(rotated.x), Mathf.RoundToInt(rotated.y));
                }

            default:
                return new BoardPosition(x, y, z); // No rotation
        }
    }
    
    /// <summary>
    /// Rotate a 2D point 90 degrees using discrete grid rotation (Rubik's cube style)
    /// MULTI-BOARD SUPPORT: Works with 4x4x4, 6x6x6, and 8x8x8 boards
    /// </summary>
    private static Vector2 Rotate2D(Vector2 point, bool clockwise, int maxCoord)
    {
        // Convert to integer coordinates for discrete grid rotation
        int x = Mathf.RoundToInt(point.x);
        int y = Mathf.RoundToInt(point.y);

        // Discrete grid rotation using dynamic maxCoord (3 for 4x4x4, 5 for 6x6x6, 7 for 8x8x8)
        int newX, newY;
        if (clockwise)
        {
            // 90° clockwise: (x, y) -> (y, maxCoord-x)
            newX = y;
            newY = maxCoord - x;
        }
        else
        {
            // 90° counter-clockwise: (x, y) -> (maxCoord-y, x)
            newX = maxCoord - y;
            newY = x;
        }

        // Clamp to valid grid positions (0 to maxCoord) as safety measure
        newX = Mathf.Clamp(newX, 0, maxCoord);
        newY = Mathf.Clamp(newY, 0, maxCoord);

        return new Vector2(newX, newY);
    }
    
    /// <summary>
    /// Calculate the center position for a set of board positions
    /// </summary>
    public static Vector3 CalculatePositionCenter(List<BoardPosition> positions)
    {
        if (positions.Count == 0) return Vector3.zero;
        
        Vector3 sum = Vector3.zero;
        foreach (BoardPosition pos in positions)
        {
            sum += new Vector3(pos.x, pos.y, pos.z);
        }
        
        return sum / positions.Count;
    }
    
    /// <summary>
    /// Get world position offset for smooth rotation animation
    /// </summary>
    public static Vector3 CalculateRotationOffset(BoardPosition originalPos, BoardPosition newPos, float progress)
    {
        Vector3 start = new Vector3(originalPos.x, originalPos.y, originalPos.z);
        Vector3 end = new Vector3(newPos.x, newPos.y, newPos.z);
        
        // For 90-degree rotations, we want to arc through 3D space
        Vector3 center = (start + end) * 0.5f;
        Vector3 arc = Vector3.Cross((end - start).normalized, Vector3.up) * 0.5f;
        
        // Create smooth arc motion
        float t = progress;
        Vector3 midPoint = center + arc * Mathf.Sin(t * Mathf.PI);
        
        return Vector3.Lerp(Vector3.Lerp(start, midPoint, t * 2), 
                           Vector3.Lerp(midPoint, end, t * 2 - 1), 
                           Mathf.Clamp01(t * 2 - 1));
    }
    
    /// <summary>
    /// Validate that a rotation mapping is valid (no conflicts)
    /// </summary>
    public static bool ValidateRotationMap(Dictionary<BoardPosition, BoardPosition> rotationMap)
    {
        HashSet<BoardPosition> targetPositions = new HashSet<BoardPosition>();
        
        foreach (var kvp in rotationMap)
        {
            BoardPosition target = kvp.Value;
            
            // Check for valid target position
            if (!target.IsValid())
            {
                Debug.LogError($"ChaosMath: Invalid target position {target} in rotation map");
                return false;
            }
            
            // Check for duplicate targets (position conflicts)
            if (targetPositions.Contains(target))
            {
                Debug.LogError($"ChaosMath: Position conflict - multiple pieces trying to move to {target}");
                return false;
            }
            
            targetPositions.Add(target);
        }
        
        return true;
    }
    
    /// <summary>
    /// Create a circular permutation for positions (for complex rotations)
    /// </summary>
    public static Dictionary<BoardPosition, BoardPosition> CreateCircularPermutation(List<BoardPosition> cycle)
    {
        Dictionary<BoardPosition, BoardPosition> permutation = new Dictionary<BoardPosition, BoardPosition>();
        
        if (cycle.Count < 2) return permutation;
        
        for (int i = 0; i < cycle.Count; i++)
        {
            BoardPosition current = cycle[i];
            BoardPosition next = cycle[(i + 1) % cycle.Count];
            permutation[current] = next;
        }
        
        return permutation;
    }
    
    /// <summary>
    /// Combine multiple rotation mappings (for complex multi-step rotations)
    /// </summary>
    public static Dictionary<BoardPosition, BoardPosition> CombineRotations(
        Dictionary<BoardPosition, BoardPosition> rotation1,
        Dictionary<BoardPosition, BoardPosition> rotation2)
    {
        Dictionary<BoardPosition, BoardPosition> combined = new Dictionary<BoardPosition, BoardPosition>();
        
        // Apply rotation1 first, then rotation2
        foreach (var kvp in rotation1)
        {
            BoardPosition start = kvp.Key;
            BoardPosition intermediate = kvp.Value;
            
            // Check if the intermediate position is also rotated by rotation2
            BoardPosition final = rotation2.ContainsKey(intermediate) ? rotation2[intermediate] : intermediate;
            
            combined[start] = final;
        }
        
        // Add any positions only in rotation2
        foreach (var kvp in rotation2)
        {
            if (!combined.ContainsKey(kvp.Key))
            {
                combined[kvp.Key] = kvp.Value;
            }
        }
        
        return combined;
    }
    
    /// <summary>
    /// Get debug information about a rotation mapping
    /// </summary>
    public static string GetRotationDebugInfo(Dictionary<BoardPosition, BoardPosition> rotationMap)
    {
        string info = $"Rotation Map ({rotationMap.Count} moves):\n";
        
        foreach (var kvp in rotationMap)
        {
            info += $"  {kvp.Key} → {kvp.Value}\n";
        }
        
        bool isValid = ValidateRotationMap(rotationMap);
        info += $"Valid: {isValid}";
        
        return info;
    }
}