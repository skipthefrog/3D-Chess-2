using UnityEngine;

public class BoardCell : MonoBehaviour
{
    public BoardPosition Position { get; private set; }
    
    public void Initialize(BoardPosition position)
    {
        Position = position;
    }
    
    private void OnMouseDown()
    {
        if (ChessBoard.Instance != null)
        {
            // Notify the input manager that this cell was clicked
            InputManager inputManager = FindFirstObjectByType<InputManager>();
            if (inputManager != null)
            {
                inputManager.OnCellClicked(Position);
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        ChessPiece piece = other.GetComponent<ChessPiece>();
        if (piece != null)
        {
            // Handle piece entering this cell
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        ChessPiece piece = other.GetComponent<ChessPiece>();
        if (piece != null)
        {
            // Handle piece exiting this cell
        }
    }
}