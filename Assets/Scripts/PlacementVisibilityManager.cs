using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages piece visibility during the placement phase to implement blind/hidden placement.
/// Only applies during GameState.PiecePlacement - all pieces become visible during gameplay.
/// </summary>
public class PlacementVisibilityManager : MonoBehaviour
{
    [Header("Visibility Settings")]
    public bool enableBlindPlacement = true;
    
    [Header("Player Perspective")]
    public PieceColor currentPlayerView = PieceColor.White; // Which player's view to show
    
    [Header("Materials")]
    public Material hiddenPieceMaterial; // Material for hidden opponent pieces
    
    public static PlacementVisibilityManager Instance { get; private set; }
    
    // Track original materials for restoration
    private Dictionary<ChessPiece, Material> originalMaterials = new Dictionary<ChessPiece, Material>();
    
    // Events
    public System.Action OnPiecesRevealed;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("PlacementVisibilityManager: Instance created");
        }
        else
        {
            Debug.LogWarning("PlacementVisibilityManager: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Subscribe to game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        }
        
        // Initialize visibility if already in placement phase
        if (GameStateManager.Instance != null && GameStateManager.Instance.currentState == GameState.PiecePlacement)
        {
            EnableBlindPlacement();
        }
    }
    
    /// <summary>
    /// Called when game state changes
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.PiecePlacement:
                if (enableBlindPlacement)
                {
                    EnableBlindPlacement();
                }
                break;
            case GameState.Playing:
                // Reveal all pieces when gameplay starts
                RevealAllPieces();
                break;
        }
    }
    
    /// <summary>
    /// Enable blind placement - hide opponent pieces from current player view
    /// </summary>
    public void EnableBlindPlacement()
    {
        if (!enableBlindPlacement) return;
        
        Debug.Log($"🙈 PlacementVisibilityManager: Enabling blind placement for {currentPlayerView} player view");
        
        UpdatePieceVisibility();
    }
    
    /// <summary>
    /// Update visibility of all pieces based on current player view
    /// </summary>
    private void UpdatePieceVisibility()
    {
        if (!enableBlindPlacement) return;
        
        // Find all pieces on the board
        if (ChessBoard.Instance == null) return;
        
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                for (int z = 0; z < 4; z++)
                {
                    ChessPiece piece = ChessBoard.Instance.GetPieceAt(new BoardPosition(x, y, z));
                    if (piece != null)
                    {
                        UpdatePieceVisibility(piece);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Update visibility of a specific piece
    /// </summary>
    public void UpdatePieceVisibility(ChessPiece piece)
    {
        if (!enableBlindPlacement) return;
        if (piece == null) return;
        
        bool shouldBeVisible = ShouldPieceBeVisible(piece);
        
        if (shouldBeVisible)
        {
            ShowPiece(piece);
        }
        else
        {
            HidePiece(piece);
        }
    }
    
    /// <summary>
    /// Determine if a piece should be visible to the current player view
    /// </summary>
    private bool ShouldPieceBeVisible(ChessPiece piece)
    {
        // During placement phase, only show own pieces
        return piece.pieceColor == currentPlayerView;
    }
    
    /// <summary>
    /// Hide a piece from view
    /// </summary>
    private void HidePiece(ChessPiece piece)
    {
        if (piece == null) return;
        
        MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        
        // Store original material if not already stored
        if (!originalMaterials.ContainsKey(piece))
        {
            originalMaterials[piece] = renderer.material;
        }
        
        // Apply hidden material or make invisible
        if (hiddenPieceMaterial != null)
        {
            renderer.material = hiddenPieceMaterial;
        }
        else
        {
            // Make completely invisible
            renderer.enabled = false;
        }
        
        Debug.Log($"🙈 Hidden {piece.pieceColor} {piece.pieceType} from {currentPlayerView} player view");
    }
    
    /// <summary>
    /// Show a piece (restore original material)
    /// </summary>
    private void ShowPiece(ChessPiece piece)
    {
        if (piece == null) return;
        
        MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        
        // Restore original material if we have it
        if (originalMaterials.ContainsKey(piece))
        {
            renderer.material = originalMaterials[piece];
        }
        
        // Ensure renderer is enabled
        renderer.enabled = true;
        
        Debug.Log($"👁️ Showing {piece.pieceColor} {piece.pieceType} to {currentPlayerView} player view");
    }
    
    /// <summary>
    /// Switch the player perspective (for testing or different views)
    /// </summary>
    public void SwitchPlayerView(PieceColor newPlayerView)
    {
        if (currentPlayerView == newPlayerView) return;
        
        Debug.Log($"🔄 PlacementVisibilityManager: Switching view from {currentPlayerView} to {newPlayerView}");
        
        currentPlayerView = newPlayerView;
        UpdatePieceVisibility();
    }
    
    /// <summary>
    /// Reveal all pieces - called when placement phase ends
    /// </summary>
    public void RevealAllPieces()
    {
        Debug.Log("🎭 PlacementVisibilityManager: Revealing all pieces - placement phase complete!");
        
        // Restore all pieces to visible state
        foreach (var kvp in originalMaterials)
        {
            ChessPiece piece = kvp.Key;
            Material originalMaterial = kvp.Value;
            
            if (piece != null)
            {
                MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = originalMaterial;
                    renderer.enabled = true;
                }
            }
        }
        
        // Clear the stored materials
        originalMaterials.Clear();
        
        OnPiecesRevealed?.Invoke();
        
        Debug.Log("✨ All pieces revealed - normal gameplay begins!");
    }
    
    /// <summary>
    /// Called when a new piece is placed - update its visibility
    /// </summary>
    public void OnPiecePlaced(ChessPiece piece)
    {
        if (enableBlindPlacement && piece != null)
        {
            UpdatePieceVisibility(piece);
        }
    }
    
    /// <summary>
    /// Get visibility status for debugging
    /// </summary>
    public bool IsBlindPlacementActive()
    {
        return enableBlindPlacement && 
               GameStateManager.Instance != null && 
               GameStateManager.Instance.currentState == GameState.PiecePlacement;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }
}