using System;
using UnityEngine;

[System.Serializable]
public struct BoardPosition : IEquatable<BoardPosition>
{
    public int x;
    public int y;
    public int z;
    
    public BoardPosition(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    
    public bool IsValid()
    {
        return x >= 0 && x < 4 && y >= 0 && y < 4 && z >= 0 && z < 4;
    }
    
    public Vector3 ToWorldPosition(float cellSize = 1f, Vector3 boardOrigin = default)
    {
        return boardOrigin + new Vector3(x * cellSize, y * cellSize, z * cellSize);
    }
    
    public static BoardPosition FromWorldPosition(Vector3 worldPos, float cellSize = 1f, Vector3 boardOrigin = default)
    {
        Vector3 localPos = worldPos - boardOrigin;
        return new BoardPosition(
            Mathf.RoundToInt(localPos.x / cellSize),
            Mathf.RoundToInt(localPos.y / cellSize),
            Mathf.RoundToInt(localPos.z / cellSize)
        );
    }
    
    public float DistanceTo(BoardPosition other)
    {
        return Vector3.Distance(
            new Vector3(x, y, z), 
            new Vector3(other.x, other.y, other.z)
        );
    }
    
    public bool Equals(BoardPosition other)
    {
        return x == other.x && y == other.y && z == other.z;
    }
    
    public override bool Equals(object obj)
    {
        return obj is BoardPosition position && Equals(position);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z);
    }
    
    public static bool operator ==(BoardPosition left, BoardPosition right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(BoardPosition left, BoardPosition right)
    {
        return !left.Equals(right);
    }
    
    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }
}