using UnityEngine;

/// <summary>
/// UI component for pawn promotion piece selection
/// </summary>
public class PawnPromotionUI : MonoBehaviour
{
    [Header("UI Settings")]
    public bool showPromotionUI = false;
    public float buttonWidth = 100f;
    public float buttonHeight = 100f; // Square buttons for piece visuals
    public float spacing = 15f;
    
    [Header("Visual Settings")]
    public bool useVisualPieces = true;
    public float pieceIconSize = 80f;
    
    private System.Action<ChessPieceType> onPieceSelected;
    private PieceColor promotingPlayerColor;
    private Rect backgroundRect;
    private Rect queenButtonRect;
    private Rect rookButtonRect;
    private Rect bishopButtonRect;
    private Rect knightButtonRect;
    private Rect titleRect;
    
    private GUIStyle buttonStyle;
    private GUIStyle titleStyle;
    private GUIStyle backgroundStyle;
    
    // Piece preview textures
    private Texture2D queenTexture;
    private Texture2D rookTexture;
    private Texture2D bishopTexture;
    private Texture2D knightTexture;
    
    private void Start()
    {
        SetupGUIStyles();
        CalculateRects();
    }
    
    private void SetupGUIStyles()
    {
        // Button style - optimized for visual pieces
        buttonStyle = new GUIStyle();
        buttonStyle.fontSize = 12; // Smaller text for labels under pieces
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.LowerCenter;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.4f, 0.6f, 0.8f));
        buttonStyle.hover.background = CreateColorTexture(new Color(0.3f, 0.5f, 0.7f, 0.9f));
        buttonStyle.active.background = CreateColorTexture(new Color(0.4f, 0.6f, 0.8f, 1f));
        buttonStyle.border = new RectOffset(4, 4, 4, 4);
        buttonStyle.padding = new RectOffset(4, 4, 4, 4);
        
        // Title style
        titleStyle = new GUIStyle();
        titleStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.025f); // Responsive font size
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;
        
        // Background style
        backgroundStyle = new GUIStyle();
        backgroundStyle.normal.background = CreateColorTexture(new Color(0f, 0f, 0f, 0.8f));
    }
    
    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
    
    private void CalculateRects()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        // Calculate total width needed for 4 buttons with spacing
        float totalWidth = (buttonWidth * 4) + (spacing * 3);
        float startX = (screenWidth - totalWidth) / 2f;
        float centerY = screenHeight / 2f;
        
        // Background covers the entire selection area with more padding
        float backgroundWidth = totalWidth + (spacing * 4);
        float backgroundHeight = buttonHeight + 120f; // More space to prevent overlap
        backgroundRect = new Rect(
            (screenWidth - backgroundWidth) / 2f,
            centerY - (backgroundHeight / 2f),
            backgroundWidth,
            backgroundHeight
        );
        
        // Title positioned higher to avoid button overlap
        titleRect = new Rect(
            (screenWidth - totalWidth) / 2f,
            centerY - 70f, // Moved up from -50f
            totalWidth,
            25f
        );
        
        // Button positions
        float buttonY = centerY - (buttonHeight / 2f);
        queenButtonRect = new Rect(startX, buttonY, buttonWidth, buttonHeight);
        rookButtonRect = new Rect(startX + buttonWidth + spacing, buttonY, buttonWidth, buttonHeight);
        bishopButtonRect = new Rect(startX + (buttonWidth + spacing) * 2, buttonY, buttonWidth, buttonHeight);
        knightButtonRect = new Rect(startX + (buttonWidth + spacing) * 3, buttonY, buttonWidth, buttonHeight);
    }
    
    /// <summary>
    /// Show the promotion selection UI
    /// </summary>
    public void ShowPromotionSelection(PieceColor playerColor, System.Action<ChessPieceType> onSelection)
    {
        Debug.Log($"PawnPromotionUI.ShowPromotionSelection: ENTRY - playerColor={playerColor}");
        Debug.Log($"PawnPromotionUI.ShowPromotionSelection: onSelection callback is {(onSelection != null ? "NOT NULL" : "NULL")}");
        
        showPromotionUI = true;
        promotingPlayerColor = playerColor;
        onPieceSelected = onSelection;
        
        // Load piece preview textures if using visual pieces
        if (useVisualPieces)
        {
            LoadPiecePreviewTextures();
        }
        
        Debug.Log($"PawnPromotionUI.ShowPromotionSelection: Stored callback, onPieceSelected is now {(onPieceSelected != null ? "NOT NULL" : "NULL")}");
        Debug.Log($"PawnPromotionUI.ShowPromotionSelection: showPromotionUI={showPromotionUI}, promotingPlayerColor={promotingPlayerColor}");
    }
    
    /// <summary>
    /// Load piece preview textures for visual buttons
    /// </summary>
    private void LoadPiecePreviewTextures()
    {
        if (PiecePreviewRenderer.Instance == null)
        {
            Debug.LogWarning("PawnPromotionUI: PiecePreviewRenderer not available, falling back to text buttons");
            useVisualPieces = false;
            return;
        }
        
        try
        {
            queenTexture = PiecePreviewRenderer.Instance.GetCachedPiecePreview(ChessPieceType.Queen, promotingPlayerColor);
            rookTexture = PiecePreviewRenderer.Instance.GetCachedPiecePreview(ChessPieceType.Rook, promotingPlayerColor);
            bishopTexture = PiecePreviewRenderer.Instance.GetCachedPiecePreview(ChessPieceType.Bishop, promotingPlayerColor);
            knightTexture = PiecePreviewRenderer.Instance.GetCachedPiecePreview(ChessPieceType.Knight, promotingPlayerColor);
            
            Debug.Log("PawnPromotionUI: Piece preview textures loaded successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PawnPromotionUI: Failed to load piece textures: {e.Message}");
            useVisualPieces = false;
        }
    }
    
    /// <summary>
    /// Hide the promotion selection UI
    /// </summary>
    public void HidePromotionSelection()
    {
        showPromotionUI = false;
        onPieceSelected = null;
        
        Debug.Log("PawnPromotionUI: Hiding promotion selection");
    }
    
    private void OnGUI()
    {
        if (!showPromotionUI) return;
        
        // Ensure styles are set up
        if (buttonStyle == null)
        {
            SetupGUIStyles();
            CalculateRects();
        }
        
        // Draw semi-transparent background
        GUI.Box(backgroundRect, "", backgroundStyle);
        
        // Draw title
        string titleText = $"{promotingPlayerColor} Pawn Promotion";
        GUI.Label(titleRect, titleText, titleStyle);
        
        // Draw piece selection buttons
        if (useVisualPieces && queenTexture != null)
        {
            DrawVisualPieceButton(queenButtonRect, queenTexture, "QUEEN", ChessPieceType.Queen);
            DrawVisualPieceButton(rookButtonRect, rookTexture, "ROOK", ChessPieceType.Rook);
            DrawVisualPieceButton(bishopButtonRect, bishopTexture, "BISHOP", ChessPieceType.Bishop);
            DrawVisualPieceButton(knightButtonRect, knightTexture, "KNIGHT", ChessPieceType.Knight);
        }
        else
        {
            // Fallback to text buttons
            if (GUI.Button(queenButtonRect, "QUEEN", buttonStyle))
            {
                OnPieceButtonClicked(ChessPieceType.Queen);
            }
            
            if (GUI.Button(rookButtonRect, "ROOK", buttonStyle))
            {
                OnPieceButtonClicked(ChessPieceType.Rook);
            }
            
            if (GUI.Button(bishopButtonRect, "BISHOP", buttonStyle))
            {
                OnPieceButtonClicked(ChessPieceType.Bishop);
            }
            
            if (GUI.Button(knightButtonRect, "KNIGHT", buttonStyle))
            {
                OnPieceButtonClicked(ChessPieceType.Knight);
            }
        }
        
        // Draw instructions positioned below title but above buttons
        Rect instructionsRect = new Rect(
            titleRect.x,
            titleRect.y + 30f, // Closer to title
            titleRect.width,
            20f
        );
        
        // Create smaller style for instructions
        GUIStyle instructionStyle = new GUIStyle(titleStyle);
        instructionStyle.fontSize = Mathf.RoundToInt(titleStyle.fontSize * 0.7f);
        instructionStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f); // Slightly dimmer
        
        GUI.Label(instructionsRect, "Choose your new piece:", instructionStyle);
    }
    
    private void OnPieceButtonClicked(ChessPieceType pieceType)
    {
        Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: ENTRY - Player selected {pieceType}");
        Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: showPromotionUI={showPromotionUI}");
        Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: promotingPlayerColor={promotingPlayerColor}");
        Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: onPieceSelected callback is {(onPieceSelected != null ? "NOT NULL" : "NULL")}");
        
        // Validate promotion is still active
        if (PawnPromotionManager.Instance != null && !PawnPromotionManager.Instance.IsPromotionInProgress())
        {
            Debug.LogWarning($"PawnPromotionUI.OnPieceButtonClicked: Promotion no longer in progress - ignoring click");
            HidePromotionSelection(); // Hide UI since promotion is no longer active
            return;
        }
        
        if (onPieceSelected == null)
        {
            Debug.LogError($"PawnPromotionUI.OnPieceButtonClicked: ERROR - onPieceSelected callback is NULL! Cannot proceed with promotion.");
            return;
        }
        
        // CRITICAL FIX: Store the callback locally BEFORE hiding the UI
        // This prevents the race condition where HidePromotionSelection() nulls the callback
        System.Action<ChessPieceType> localCallback = onPieceSelected;
        Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: Stored callback locally to prevent race condition");
        
        // Hide the UI 
        Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: About to hide promotion selection");
        HidePromotionSelection();
        Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: UI hidden, showPromotionUI={showPromotionUI}");
        
        // Notify the callback using the locally stored reference
        Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: About to invoke callback with {pieceType}");
        try
        {
            Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: Invoking locally stored callback...");
            localCallback.Invoke(pieceType);
            Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: Callback invocation completed successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PawnPromotionUI.OnPieceButtonClicked: Exception during callback invocation: {ex.Message}");
            Debug.LogError($"PawnPromotionUI.OnPieceButtonClicked: Exception stack trace: {ex.StackTrace}");
            
            // Failsafe: Try to find PawnPromotionManager and call it directly
            Debug.LogError($"PawnPromotionUI.OnPieceButtonClicked: Attempting direct call to PawnPromotionManager");
            PawnPromotionManager manager = FindFirstObjectByType<PawnPromotionManager>();
            if (manager != null)
            {
                Debug.LogWarning($"PawnPromotionUI.OnPieceButtonClicked: Found PawnPromotionManager, trying direct call");
                // Use reflection to call the private OnPieceSelected method
                var method = typeof(PawnPromotionManager).GetMethod("OnPieceSelected", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(manager, new object[] { pieceType });
                    Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: Direct method call successful");
                }
                else
                {
                    Debug.LogError($"PawnPromotionUI.OnPieceButtonClicked: OnPieceSelected method not found via reflection");
                }
            }
            else
            {
                Debug.LogError($"PawnPromotionUI.OnPieceButtonClicked: PawnPromotionManager not found for emergency call");
            }
        }
        
        Debug.Log($"PawnPromotionUI.OnPieceButtonClicked: EXIT");
    }
    
    /// <summary>
    /// Draw a visual piece button with texture and label
    /// </summary>
    private void DrawVisualPieceButton(Rect buttonRect, Texture2D pieceTexture, string label, ChessPieceType pieceType)
    {
        // Create button background
        if (GUI.Button(buttonRect, "", buttonStyle))
        {
            OnPieceButtonClicked(pieceType);
            return;
        }
        
        // Draw piece texture in the center of the button
        if (pieceTexture != null)
        {
            float iconMargin = (buttonWidth - pieceIconSize) / 2f;
            Rect iconRect = new Rect(
                buttonRect.x + iconMargin,
                buttonRect.y + iconMargin - 10f, // Slightly higher to leave room for label
                pieceIconSize,
                pieceIconSize - 20f // Make room for text at bottom
            );
            
            GUI.DrawTexture(iconRect, pieceTexture, ScaleMode.ScaleToFit, true);
        }
        
        // Draw piece label at bottom of button
        Rect labelRect = new Rect(
            buttonRect.x + 2f,
            buttonRect.y + buttonRect.height - 20f,
            buttonRect.width - 4f,
            18f
        );
        
        GUIStyle labelStyle = new GUIStyle(buttonStyle);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontSize = 10;
        labelStyle.normal.textColor = Color.white;
        
        GUI.Label(labelRect, label, labelStyle);
    }
}