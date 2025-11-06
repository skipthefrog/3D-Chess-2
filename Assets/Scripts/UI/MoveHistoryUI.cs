using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Move history panel that displays all moves made during the game
/// Shows moves in chess notation with color-coding and icons
/// Scrollable list with toggle visibility
/// Format: Turn #X: WQ B3c→B3d, Turn #Y: BN A1a×C3c ⚔️
/// </summary>
public class MoveHistoryUI : MonoBehaviour
{
    [Header("Panel Settings")]
    public bool showOnStart = false;
    public KeyCode toggleKey = KeyCode.H; // H for History
    public float panelWidth = 300f;
    public float panelHeight = 600f;

    [Header("Visual Settings")]
    public int fontSize = 14;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.85f);
    public Color scrollbarColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Icons")]
    public string normalMoveIcon = "✓";
    public string captureIcon = "⚔️";
    public string checkIcon = "⚠️";
    public string checkmateIcon = "💀";

    // UI Components
    private Canvas historyCanvas;
    private GameObject historyPanel;
    private ScrollRect scrollRect;
    private GameObject contentContainer;
    private TextMeshProUGUI historyText;
    private Image backgroundImage;

    // Move tracking
    private List<MoveRecord> moveHistory = new List<MoveRecord>();
    private int turnCounter = 0;
    private bool isVisible = false;

    public static MoveHistoryUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("MoveHistoryUI: Instance created");
        }
        else
        {
            Debug.LogWarning("MoveHistoryUI: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CreateMoveHistoryUI();
        SetVisible(showOnStart);
    }

    private void Update()
    {
        // Toggle visibility with hotkey
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleVisibility();
        }
    }

    /// <summary>
    /// Create the move history UI panel
    /// </summary>
    private void CreateMoveHistoryUI()
    {
        Debug.Log("MoveHistoryUI: Creating move history panel");

        // Create canvas
        GameObject canvasObject = new GameObject("MoveHistoryCanvas");
        canvasObject.transform.SetParent(transform);

        historyCanvas = canvasObject.AddComponent<Canvas>();
        historyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        historyCanvas.sortingOrder = 100; // Below status bar

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(375f, 812f); // iPhone X reference
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        // Create background panel
        historyPanel = new GameObject("HistoryPanel");
        historyPanel.transform.SetParent(canvasObject.transform);

        backgroundImage = historyPanel.AddComponent<Image>();
        backgroundImage.color = backgroundColor;

        RectTransform panelRect = historyPanel.GetComponent<RectTransform>();

        // Position on right side of screen
        panelRect.anchorMin = new Vector2(1f, 0.5f);
        panelRect.anchorMax = new Vector2(1f, 0.5f);
        panelRect.pivot = new Vector2(1f, 0.5f);
        panelRect.anchoredPosition = new Vector2(-10f, 0f); // 10 pixels from right edge
        panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);

        // Add header text
        GameObject headerObject = new GameObject("HeaderText");
        headerObject.transform.SetParent(historyPanel.transform);

        TextMeshProUGUI headerText = headerObject.AddComponent<TextMeshProUGUI>();
        headerText.text = "Move History";
        headerText.fontSize = fontSize + 4;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.color = Color.white;

        RectTransform headerRect = headerObject.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -10f);
        headerRect.sizeDelta = new Vector2(-20f, 40f);

        // Create scroll view
        CreateScrollView();

        Debug.Log("MoveHistoryUI: Panel creation complete");
    }

    /// <summary>
    /// Create the scrollable content area
    /// </summary>
    private void CreateScrollView()
    {
        // Create scroll rect container
        GameObject scrollViewObject = new GameObject("ScrollView");
        scrollViewObject.transform.SetParent(historyPanel.transform);

        scrollRect = scrollViewObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 20f;

        RectTransform scrollRectTransform = scrollViewObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(10f, 10f); // Padding
        scrollRectTransform.offsetMax = new Vector2(-10f, -60f); // Leave room for header

        // Create viewport
        GameObject viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollViewObject.transform);

        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Mask mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollRect.viewport = viewportRect;

        // Create content container
        contentContainer = new GameObject("Content");
        contentContainer.transform.SetParent(viewportObject.transform);

        RectTransform contentRect = contentContainer.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f); // Will grow with content

        ContentSizeFitter fitter = contentContainer.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = contentContainer.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 5f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        scrollRect.content = contentRect;

        // Create scrollbar
        CreateScrollbar(scrollViewObject);

        // Create history text inside content
        GameObject textObject = new GameObject("HistoryText");
        textObject.transform.SetParent(contentContainer.transform);

        historyText = textObject.AddComponent<TextMeshProUGUI>();
        historyText.text = "No moves yet";
        historyText.fontSize = fontSize;
        historyText.alignment = TextAlignmentOptions.TopLeft;
        historyText.color = Color.white;
        historyText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = -1; // Auto-size
        layoutElement.flexibleHeight = 1;
    }

    /// <summary>
    /// Create the scrollbar
    /// </summary>
    private void CreateScrollbar(GameObject parent)
    {
        GameObject scrollbarObject = new GameObject("Scrollbar");
        scrollbarObject.transform.SetParent(parent.transform);

        Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        RectTransform scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.anchoredPosition = new Vector2(0f, 0f);
        scrollbarRect.sizeDelta = new Vector2(20f, 0f);

        Image scrollbarImage = scrollbarObject.AddComponent<Image>();
        scrollbarImage.color = scrollbarColor;

        // Create sliding area
        GameObject slidingArea = new GameObject("Sliding Area");
        slidingArea.transform.SetParent(scrollbarObject.transform);

        RectTransform slidingRect = slidingArea.GetComponent<RectTransform>();
        slidingRect.anchorMin = Vector2.zero;
        slidingRect.anchorMax = Vector2.one;
        slidingRect.offsetMin = new Vector2(5f, 5f);
        slidingRect.offsetMax = new Vector2(-5f, -5f);

        // Create handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(slidingArea.transform);

        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);

        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;

        scrollRect.verticalScrollbar = scrollbar;
    }

    /// <summary>
    /// Record a move in the history
    /// </summary>
    public void RecordMove(ChessPiece piece, BoardPosition from, BoardPosition to, bool isCapture = false, bool causedCheck = false, bool causedCheckmate = false)
    {
        if (piece == null) return;

        turnCounter++;

        MoveRecord record = new MoveRecord
        {
            turnNumber = turnCounter,
            piece = piece,
            from = from,
            to = to,
            isCapture = isCapture,
            causedCheck = causedCheck,
            causedCheckmate = causedCheckmate,
            timestamp = Time.time
        };

        moveHistory.Add(record);
        UpdateHistoryDisplay();

        Debug.Log($"MoveHistoryUI: Recorded move #{turnCounter}: {FormatMoveRecord(record)}");

        // Auto-scroll to bottom to show latest move
        StartCoroutine(ScrollToBottom());
    }

    /// <summary>
    /// Format a move record into a display string
    /// </summary>
    private string FormatMoveRecord(MoveRecord record)
    {
        // Get chess notation for the move
        string moveNotation = ChessNotationConverter.FormatMoveWithCapture(record.piece, record.from, record.to, record.isCapture);

        // Add appropriate icon
        string icon = normalMoveIcon;
        if (record.causedCheckmate)
        {
            icon = checkmateIcon;
        }
        else if (record.causedCheck)
        {
            icon = checkIcon;
        }
        else if (record.isCapture)
        {
            icon = captureIcon;
        }

        // Get color for the piece
        Color pieceColor = ChessNotationConverter.GetPieceDisplayColor(record.piece.pieceColor);
        string colorHex = ColorUtility.ToHtmlStringRGB(pieceColor);

        return $"<color=#{colorHex}>Turn #{record.turnNumber}: {moveNotation} {icon}</color>";
    }

    /// <summary>
    /// Update the history display with all moves
    /// </summary>
    private void UpdateHistoryDisplay()
    {
        if (historyText == null) return;

        if (moveHistory.Count == 0)
        {
            historyText.text = "No moves yet";
            return;
        }

        List<string> formattedMoves = new List<string>();
        foreach (MoveRecord record in moveHistory)
        {
            formattedMoves.Add(FormatMoveRecord(record));
        }

        historyText.text = string.Join("\n", formattedMoves);

        Debug.Log($"MoveHistoryUI: Display updated with {moveHistory.Count} moves");
    }

    /// <summary>
    /// Scroll to the bottom of the history (latest moves)
    /// </summary>
    private System.Collections.IEnumerator ScrollToBottom()
    {
        // Wait one frame for layout to update
        yield return null;

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f; // 0 = bottom
        }
    }

    /// <summary>
    /// Clear all move history
    /// </summary>
    public void ClearHistory()
    {
        moveHistory.Clear();
        turnCounter = 0;
        UpdateHistoryDisplay();
        Debug.Log("MoveHistoryUI: History cleared");
    }

    /// <summary>
    /// Toggle panel visibility
    /// </summary>
    public void ToggleVisibility()
    {
        SetVisible(!isVisible);
    }

    /// <summary>
    /// Set panel visibility
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;

        if (historyPanel != null)
        {
            historyPanel.SetActive(visible);
        }

        Debug.Log($"MoveHistoryUI: Visibility set to {visible}");
    }

    /// <summary>
    /// Get current move count
    /// </summary>
    public int GetMoveCount()
    {
        return moveHistory.Count;
    }

    /// <summary>
    /// Get the last move recorded
    /// </summary>
    public MoveRecord GetLastMove()
    {
        if (moveHistory.Count > 0)
        {
            return moveHistory[moveHistory.Count - 1];
        }
        return null;
    }
}

/// <summary>
/// Data structure for storing move information
/// </summary>
[System.Serializable]
public class MoveRecord
{
    public int turnNumber;
    public ChessPiece piece;
    public BoardPosition from;
    public BoardPosition to;
    public bool isCapture;
    public bool causedCheck;
    public bool causedCheckmate;
    public float timestamp;

    public override string ToString()
    {
        return $"Turn {turnNumber}: {ChessNotationConverter.GetPieceNotation(piece)} {ChessNotationConverter.BoardPositionToNotation(from)}→{ChessNotationConverter.BoardPositionToNotation(to)}";
    }
}
