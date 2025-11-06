using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mini tray UI that appears at the bottom of screen during placement phase for online games.
/// Shows current player's available pieces as small 2D representations.
/// Only active during placement phase and only for online games.
/// </summary>
public class MiniTrayUI : MonoBehaviour
{
    [Header("UI Configuration")]
    public GameObject miniTrayPanel;
    public Transform pieceIconContainer;
    public GameObject pieceIconPrefab;
    
    [Header("Layout Settings")]
    public float iconSize = 60f;
    public float iconSpacing = 10f;
    public Vector2 panelOffset = new Vector2(0, 100); // Offset from bottom of screen
    
    [Header("Visual Settings")]
    public Color backgroundColor = new Color(0, 0, 0, 0.7f);
    public Color selectedIconColor = Color.yellow;
    public Color normalIconColor = Color.white;
    
    private Canvas miniTrayCanvas;
    private List<MiniPieceIcon> pieceIcons = new List<MiniPieceIcon>();
    private PieceColor currentPlayerColor;
    private ChessPiece selectedPiece;
    
    public static MiniTrayUI Instance { get; private set; }
    
    // Events
    public System.Action<ChessPiece> OnPieceSelected;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("MiniTrayUI: Instance created");
        }
        else
        {
            Debug.LogWarning("MiniTrayUI: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
            return;
        }
        
        CreateMiniTrayUI();
    }
    
    private void Start()
    {
        // Subscribe to game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        }
        
        // Subscribe to placement events
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.OnPlayerTurnChanged += OnPlayerTurnChanged;
        }
        
        // Initially hide the mini tray
        SetVisible(false);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
        
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.OnPlayerTurnChanged -= OnPlayerTurnChanged;
        }
    }
    
    /// <summary>
    /// Create the mini tray UI components
    /// </summary>
    private void CreateMiniTrayUI()
    {
        // Create canvas for overlay
        GameObject canvasObj = new GameObject("MiniTrayCanvas");
        canvasObj.transform.SetParent(transform, false);
        
        miniTrayCanvas = canvasObj.AddComponent<Canvas>();
        miniTrayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        miniTrayCanvas.sortingOrder = 100; // Ensure it appears on top
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create main panel
        GameObject panelObj = new GameObject("MiniTrayPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f); // Bottom center
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = panelOffset;
        panelRect.sizeDelta = new Vector2(500, 80); // Adjust based on piece count
        
        // Add background
        Image panelBackground = panelObj.AddComponent<Image>();
        panelBackground.color = backgroundColor;
        
        miniTrayPanel = panelObj;
        
        // Create container for piece icons
        GameObject containerObj = new GameObject("PieceIconContainer");
        containerObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;
        
        // Add horizontal layout group
        HorizontalLayoutGroup layoutGroup = containerObj.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = iconSpacing;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        
        pieceIconContainer = containerObj.transform;
        
        Debug.Log("✅ MiniTrayUI: UI components created");
    }
    
    /// <summary>
    /// Handle game state changes
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        bool shouldShow = newState == GameState.PiecePlacement && IsOnlineGame();
        
        Debug.Log($"🎮 MiniTrayUI: Game state changed to {newState}, shouldShow: {shouldShow}");
        
        if (shouldShow)
        {
            ShowMiniTray();
        }
        else
        {
            HideMiniTray();
        }
    }
    
    /// <summary>
    /// Handle player turn changes during placement
    /// </summary>
    private void OnPlayerTurnChanged(PieceColor playerColor)
    {
        if (IsLocalPlayer(playerColor))
        {
            Debug.Log($"🎮 MiniTrayUI: Local player turn: {playerColor}");
            UpdateMiniTray(playerColor);
        }
    }
    
    /// <summary>
    /// Show the mini tray and populate with current player's pieces
    /// </summary>
    public void ShowMiniTray()
    {
        if (!IsOnlineGame())
        {
            Debug.Log("🎮 MiniTrayUI: Not an online game, keeping mini tray hidden");
            return;
        }
        
        // Determine current local player
        PieceColor localPlayerColor = GetLocalPlayerColor();
        if (localPlayerColor != PieceColor.White) // Default fallback
        {
            currentPlayerColor = localPlayerColor;
            UpdateMiniTray(currentPlayerColor);
            SetVisible(true);
            
            Debug.Log($"✅ MiniTrayUI: Showing mini tray for {currentPlayerColor} player");
        }
        else
        {
            Debug.LogWarning("⚠️ MiniTrayUI: Could not determine local player color");
        }
    }
    
    /// <summary>
    /// Hide the mini tray
    /// </summary>
    public void HideMiniTray()
    {
        SetVisible(false);
        Debug.Log("🎮 MiniTrayUI: Mini tray hidden");
    }
    
    /// <summary>
    /// Update the mini tray with pieces from the specified player's tray
    /// </summary>
    private void UpdateMiniTray(PieceColor playerColor)
    {
        // Clear existing icons
        ClearPieceIcons();
        
        // Get the player's tray
        PieceTray playerTray = GetTrayForColor(playerColor);
        if (playerTray == null)
        {
            Debug.LogWarning($"⚠️ MiniTrayUI: No tray found for {playerColor}");
            return;
        }
        
        // Create icons for each piece in the tray
        var piecesInTray = playerTray.GetPieces();
        for (int i = 0; i < piecesInTray.Count; i++)
        {
            ChessPiece piece = piecesInTray[i];
            if (piece != null)
            {
                CreatePieceIcon(piece, i);
            }
        }
        
        // Adjust panel size based on piece count
        AdjustPanelSize(piecesInTray.Count);
        
        Debug.Log($"✅ MiniTrayUI: Updated with {piecesInTray.Count} pieces for {playerColor}");
    }
    
    /// <summary>
    /// Create a mini icon for a piece
    /// </summary>
    private void CreatePieceIcon(ChessPiece piece, int index)
    {
        GameObject iconObj = new GameObject($"PieceIcon_{piece.pieceType}_{index}");
        iconObj.transform.SetParent(pieceIconContainer, false);
        
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        
        // Add image component for piece representation
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = GetPieceSprite(piece); // Use PieceSpriteManager
        iconImage.color = normalIconColor;
        
        // Add button for interaction
        Button iconButton = iconObj.AddComponent<Button>();
        
        // Create mini piece icon component
        MiniPieceIcon miniIcon = iconObj.AddComponent<MiniPieceIcon>();
        miniIcon.Initialize(piece, iconImage, iconButton);
        miniIcon.OnIconClicked += OnPieceIconClicked;
        
        pieceIcons.Add(miniIcon);
    }
    
    /// <summary>
    /// Handle piece icon clicks
    /// </summary>
    private void OnPieceIconClicked(ChessPiece piece)
    {
        // Update selection
        if (selectedPiece != null)
        {
            // Deselect previous piece
            UpdateIconSelection(selectedPiece, false);
        }
        
        selectedPiece = piece;
        UpdateIconSelection(selectedPiece, true);
        
        // Notify listeners
        OnPieceSelected?.Invoke(piece);
        
        Debug.Log($"🎯 MiniTrayUI: Selected piece: {piece.pieceType} ({piece.pieceColor})");
    }
    
    /// <summary>
    /// Update visual selection state of an icon
    /// </summary>
    private void UpdateIconSelection(ChessPiece piece, bool selected)
    {
        var icon = pieceIcons.Find(icon => icon.Piece == piece);
        if (icon != null)
        {
            icon.SetSelected(selected);
        }
    }
    
    /// <summary>
    /// Clear all piece icons
    /// </summary>
    private void ClearPieceIcons()
    {
        foreach (var icon in pieceIcons)
        {
            if (icon != null)
            {
                Destroy(icon.gameObject);
            }
        }
        pieceIcons.Clear();
    }
    
    /// <summary>
    /// Adjust panel size based on number of pieces
    /// </summary>
    private void AdjustPanelSize(int pieceCount)
    {
        if (miniTrayPanel != null)
        {
            RectTransform panelRect = miniTrayPanel.GetComponent<RectTransform>();
            float totalWidth = (pieceCount * iconSize) + ((pieceCount - 1) * iconSpacing) + 20; // 20 for padding
            panelRect.sizeDelta = new Vector2(totalWidth, 80);
        }
    }
    
    /// <summary>
    /// Set mini tray visibility
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (miniTrayCanvas != null)
        {
            miniTrayCanvas.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Check if this is an online game
    /// </summary>
    private bool IsOnlineGame()
    {
        // SIMPLIFIED: For 2-player version, check if NetworkManager is connected
        bool isOnline = NetworkManager.Instance != null && NetworkManager.Instance.IsConnected;

        Debug.Log($"🌐 MiniTrayUI: IsOnlineGame check - IsConnected: {isOnline}");
        return isOnline;
    }
    
    /// <summary>
    /// Check if the specified color is the local player
    /// </summary>
    private bool IsLocalPlayer(PieceColor color)
    {
        PieceColor localColor = GetLocalPlayerColor();
        bool isLocal = color == localColor;
        
        Debug.Log($"🎮 MiniTrayUI: IsLocalPlayer check - Color: {color}, LocalColor: {localColor}, IsLocal: {isLocal}");
        return isLocal;
    }
    
    /// <summary>
    /// Get the local player's color
    /// </summary>
    private PieceColor GetLocalPlayerColor()
    {
        // First try to get from NetworkManager for online games
        if (IsOnlineGame() && NetworkManager.Instance != null)
        {
            PieceColor networkColor = NetworkManager.Instance.AssignedColor;
            Debug.Log($"🌐 MiniTrayUI: Local player color from NetworkManager: {networkColor}");
            return networkColor;
        }
        
        // For local games or fallback, assume White is the primary human player
        Debug.Log($"🏠 MiniTrayUI: Using default local player color: White (local game or network fallback)");
        return PieceColor.White;
    }
    
    /// <summary>
    /// Get the tray for a specific color
    /// </summary>
    private PieceTray GetTrayForColor(PieceColor color)
    {
        // Use PieceTrayManager for scalable tray access
        if (PieceTrayManager.Instance != null)
        {
            return PieceTrayManager.Instance.GetTray(color);
        }

        // Fallback to static references for 2-player games
        Debug.LogWarning($"MiniTrayUI: PieceTrayManager not available, using fallback for {color}");
        return color switch
        {
            PieceColor.White => PieceTray.WhiteTray,
            PieceColor.Black => PieceTray.BlackTray,
            _ => null
        };
    }
    
    /// <summary>
    /// Get sprite for piece type using PieceSpriteManager
    /// </summary>
    private Sprite GetPieceSprite(ChessPiece piece)
    {
        if (PieceSpriteManager.Instance != null)
        {
            return PieceSpriteManager.Instance.GetPieceSprite(piece);
        }
        
        Debug.LogWarning("⚠️ MiniTrayUI: PieceSpriteManager not available, returning null sprite");
        return null;
    }
}

/// <summary>
/// Component for individual piece icons in the mini tray
/// </summary>
public class MiniPieceIcon : MonoBehaviour
{
    public ChessPiece Piece { get; private set; }
    
    private Image iconImage;
    private Button iconButton;
    private Color normalColor = Color.white;
    private Color selectedColor = Color.yellow;
    
    public System.Action<ChessPiece> OnIconClicked;
    
    public void Initialize(ChessPiece piece, Image image, Button button)
    {
        Piece = piece;
        iconImage = image;
        iconButton = button;
        
        // Set up button click handler
        iconButton.onClick.AddListener(() => OnIconClicked?.Invoke(Piece));
    }
    
    public void SetSelected(bool selected)
    {
        if (iconImage != null)
        {
            iconImage.color = selected ? selectedColor : normalColor;
        }
    }
}