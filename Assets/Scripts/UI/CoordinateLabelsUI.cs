using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays chess notation coordinate labels along the edges of the 3D chess board.
/// Shows file labels (A-H), level labels (1-8), and rank labels (a-h) at 25% opacity.
/// Labels are positioned in world space and billboard to face the camera.
/// </summary>
public class CoordinateLabelsUI : MonoBehaviour
{
    [Header("Label Settings")]
    public bool enableLabels = true;
    public int fontSize = 32;
    public float labelOpacity = 0.25f; // 25% transparency
    public Color labelColor = new Color(0.2f, 0.2f, 0.2f); // Dark grey
    public float labelDistance = 2.0f; // Distance from board edge

    [Header("Label Positioning")]
    public float fileLabelsYOffset = -0.5f; // Y offset for file labels (matches rank labels)
    public float levelLabelsYOffset = 1.4f; // Y offset for level labels (centers in cells)
    public float rankLabelsYOffset = -0.5f; // Y offset for rank labels (right edge)

    // Label containers
    private GameObject labelContainer;
    private List<GameObject> fileLabels = new List<GameObject>();   // A-H (X-axis, left edge)
    private List<GameObject> levelLabels = new List<GameObject>();  // 1-8 (Y-axis, bottom edge)
    private List<GameObject> rankLabels = new List<GameObject>();   // a-h (Z-axis, right edge)

    // Camera reference for billboarding
    private Camera mainCamera;

    public static CoordinateLabelsUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("CoordinateLabelsUI: Instance created");
        }
        else
        {
            Debug.LogWarning("CoordinateLabelsUI: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("CoordinateLabelsUI: Main camera not found!");
            return;
        }

        if (enableLabels)
        {
            CreateCoordinateLabels();
        }
    }

    private void LateUpdate()
    {
        // Billboard labels to always face camera
        if (enableLabels && mainCamera != null)
        {
            BillboardLabels();
        }
    }

    /// <summary>
    /// Create all coordinate labels around the board edges
    /// </summary>
    private void CreateCoordinateLabels()
    {
        Debug.Log("CoordinateLabelsUI: Creating coordinate labels");

        // Create parent container
        labelContainer = new GameObject("CoordinateLabels");
        labelContainer.transform.SetParent(transform);
        labelContainer.transform.localPosition = Vector3.zero;

        // Get board dimensions
        Vector3Int boardDims = GetBoardDimensions();
        Debug.Log($"CoordinateLabelsUI: Board dimensions: {boardDims.x}x{boardDims.y}x{boardDims.z}");

        // Create file labels (A-H) along left edge (X-axis)
        CreateFileLabels(boardDims);

        // Create level labels (1-8) along bottom edge (Y-axis)
        CreateLevelLabels(boardDims);

        // Create rank labels (a-h) along right edge (Z-axis)
        CreateRankLabels(boardDims);

        Debug.Log($"CoordinateLabelsUI: Created {fileLabels.Count} file labels, {levelLabels.Count} level labels, {rankLabels.Count} rank labels");
    }

    /// <summary>
    /// Create file labels (A-H) along the front-bottom edge of the board (between Green and Yellow)
    /// </summary>
    private void CreateFileLabels(Vector3Int boardDims)
    {
        string[] fileLabels = ChessNotationConverter.GetFileLabels();

        for (int x = 0; x < boardDims.x; x++)
        {
            // Position along front-bottom edge (varying X, Y=0, Z=0)
            BoardPosition pos = new BoardPosition(x, 0, 0);
            Vector3 worldPos = GetWorldPositionForLabel(pos);

            // Offset back and down from board vertex (matches rank label pattern)
            worldPos += Vector3.back * labelDistance + Vector3.down * labelDistance;
            worldPos.y += fileLabelsYOffset;

            GameObject label = CreateLabel($"FileLabel_{fileLabels[x]}", fileLabels[x], worldPos);
            this.fileLabels.Add(label);
        }
    }

    /// <summary>
    /// Create level labels (1-8) along the left-front vertical edge of the board (between White and Green)
    /// </summary>
    private void CreateLevelLabels(Vector3Int boardDims)
    {
        string[] levelLabels = ChessNotationConverter.GetLevelLabels();

        for (int y = 0; y < boardDims.y; y++)
        {
            // Position along left-front vertical edge (X=0, varying Y, Z=0)
            BoardPosition pos = new BoardPosition(0, y, 0);
            Vector3 worldPos = GetWorldPositionForLabel(pos);

            // Offset left and back from board vertex (away into empty space)
            worldPos += Vector3.left * labelDistance + Vector3.back * labelDistance;
            worldPos.y += levelLabelsYOffset;

            GameObject label = CreateLabel($"LevelLabel_{levelLabels[y]}", levelLabels[y], worldPos);
            this.levelLabels.Add(label);
        }
    }

    /// <summary>
    /// Create rank labels (a-h) along the left-bottom edge of the board (between White and Yellow)
    /// </summary>
    private void CreateRankLabels(Vector3Int boardDims)
    {
        string[] rankLabels = ChessNotationConverter.GetRankLabels();

        for (int z = 0; z < boardDims.z; z++)
        {
            // Position along left-bottom edge (X=0, Y=0, varying Z)
            BoardPosition pos = new BoardPosition(0, 0, z);
            Vector3 worldPos = GetWorldPositionForLabel(pos);

            // Offset left and downward from board
            worldPos += Vector3.left * labelDistance + Vector3.down * labelDistance;
            worldPos.y += rankLabelsYOffset;

            GameObject label = CreateLabel($"RankLabel_{rankLabels[z]}", rankLabels[z], worldPos);
            this.rankLabels.Add(label);
        }
    }

    /// <summary>
    /// Create a single text label
    /// </summary>
    private GameObject CreateLabel(string name, string text, Vector3 position)
    {
        GameObject labelObj = new GameObject(name);
        labelObj.transform.SetParent(labelContainer.transform);
        labelObj.transform.position = position;

        // Add TextMeshPro component
        TextMeshPro textMesh = labelObj.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignmentOptions.Center;

        // Set color with opacity
        Color colorWithOpacity = labelColor;
        colorWithOpacity.a = labelOpacity;
        textMesh.color = colorWithOpacity;

        // Set font
        textMesh.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");

        // Configure for world space
        RectTransform rectTransform = labelObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(2, 2);
        }

        return labelObj;
    }

    /// <summary>
    /// Billboard all labels to face the camera
    /// </summary>
    private void BillboardLabels()
    {
        if (labelContainer == null || mainCamera == null) return;

        // Billboard all file labels
        foreach (GameObject label in fileLabels)
        {
            if (label != null)
            {
                label.transform.LookAt(mainCamera.transform);
                label.transform.Rotate(0, 180, 0); // Flip to face camera correctly
            }
        }

        // Billboard all level labels
        foreach (GameObject label in levelLabels)
        {
            if (label != null)
            {
                label.transform.LookAt(mainCamera.transform);
                label.transform.Rotate(0, 180, 0);
            }
        }

        // Billboard all rank labels
        foreach (GameObject label in rankLabels)
        {
            if (label != null)
            {
                label.transform.LookAt(mainCamera.transform);
                label.transform.Rotate(0, 180, 0);
            }
        }
    }

    /// <summary>
    /// Get world position for a board position (used for label placement)
    /// </summary>
    private Vector3 GetWorldPositionForLabel(BoardPosition pos)
    {
        if (ChessBoard.Instance != null)
        {
            // Get local position from ChessBoard
            Vector3 localPos = ChessBoard.Instance.BoardToLocalPosition(pos);

            // Find Pieces Container to convert to world space
            GameObject piecesContainer = GameObject.Find("Pieces Container");
            if (piecesContainer != null)
            {
                return piecesContainer.transform.TransformPoint(localPos);
            }

            return localPos;
        }

        // Fallback: simple grid positioning
        return new Vector3(pos.x * 2f, pos.y * 2f, pos.z * 2f);
    }

    /// <summary>
    /// Get board dimensions from BoardDimensionsManager
    /// </summary>
    private Vector3Int GetBoardDimensions()
    {
        if (BoardDimensionsManager.Instance != null)
        {
            return BoardDimensionsManager.Instance.GetDimensions();
        }

        // Fallback to default 4x4x4
        return new Vector3Int(4, 4, 4);
    }

    /// <summary>
    /// Enable or disable coordinate labels
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        enableLabels = enabled;

        if (labelContainer != null)
        {
            labelContainer.SetActive(enabled);
        }

        Debug.Log($"CoordinateLabelsUI: Labels {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Update labels when board rotates (chaos mode)
    /// </summary>
    public void RefreshLabels()
    {
        // Labels automatically billboard to camera in LateUpdate
        // This method is a hook for future enhancements if needed
        Debug.Log("CoordinateLabelsUI: Refreshing label positions");
    }

    /// <summary>
    /// Clean up labels
    /// </summary>
    private void OnDestroy()
    {
        if (labelContainer != null)
        {
            Destroy(labelContainer);
        }
    }
}
