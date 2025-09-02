using UnityEngine;

/// <summary>
/// Helper component for rendering 3D chess pieces as 2D textures for UI buttons.
/// Creates miniature piece previews for pawn promotion selection.
/// </summary>
public class PiecePreviewRenderer : MonoBehaviour
{
    [Header("Rendering Settings")]
    public int textureSize = 128;
    public LayerMask previewLayer = 1 << 31; // Use layer 31 for preview pieces
    public Color backgroundColor = new Color(0f, 0f, 0f, 0f); // Transparent background
    
    [Header("Camera Settings")]
    public float cameraDistance = 3f;
    public Vector3 lightDirection = new Vector3(-0.5f, -1f, -0.5f);
    public float lightIntensity = 1.2f;
    
    private Camera previewCamera;
    private Light previewLight;
    private RenderTexture renderTexture;
    
    public static PiecePreviewRenderer Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SetupPreviewRender();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void SetupPreviewRender()
    {
        Debug.Log("PiecePreviewRenderer.SetupPreviewRender: ENTRY");
        
        try
        {
            // Create render texture
            Debug.Log($"PiecePreviewRenderer.SetupPreviewRender: Creating render texture {textureSize}x{textureSize}");
            renderTexture = new RenderTexture(textureSize, textureSize, 24);
            renderTexture.antiAliasing = 4; // Anti-aliasing for better quality
            
            // Create preview camera
            Debug.Log("PiecePreviewRenderer.SetupPreviewRender: Creating preview camera");
            GameObject cameraObject = new GameObject("Piece Preview Camera");
            cameraObject.transform.SetParent(transform);
            previewCamera = cameraObject.AddComponent<Camera>();
            
            previewCamera.targetTexture = renderTexture;
            previewCamera.backgroundColor = backgroundColor;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.cullingMask = previewLayer;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 1.5f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 10f;
            previewCamera.enabled = false; // Only render when requested
            
            // Position camera for good piece view
            previewCamera.transform.position = new Vector3(0, 0, -cameraDistance);
            previewCamera.transform.rotation = Quaternion.Euler(15f, 45f, 0f);
            
            Debug.Log($"PiecePreviewRenderer.SetupPreviewRender: Camera positioned at {previewCamera.transform.position} with rotation {previewCamera.transform.rotation}");
            Debug.Log($"PiecePreviewRenderer.SetupPreviewRender: Camera culling mask: {previewLayer.value} (layer {Mathf.RoundToInt(Mathf.Log(previewLayer.value, 2))})");
            
            // Create preview light
            Debug.Log("PiecePreviewRenderer.SetupPreviewRender: Creating preview light");
            GameObject lightObject = new GameObject("Piece Preview Light");
            lightObject.transform.SetParent(transform);
            previewLight = lightObject.AddComponent<Light>();
            
            previewLight.type = LightType.Directional;
            previewLight.intensity = lightIntensity;
            previewLight.color = Color.white;
            previewLight.cullingMask = previewLayer;
            previewLight.transform.rotation = Quaternion.LookRotation(lightDirection);
            
            Debug.Log($"PiecePreviewRenderer.SetupPreviewRender: Light direction: {lightDirection}, intensity: {lightIntensity}");
            Debug.Log("PiecePreviewRenderer.SetupPreviewRender: Setup complete successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PiecePreviewRenderer.SetupPreviewRender: Exception during setup: {e.Message}");
            Debug.LogError($"PiecePreviewRenderer.SetupPreviewRender: Stack trace: {e.StackTrace}");
        }
    }
    
    /// <summary>
    /// Render a piece as a 2D texture for UI buttons
    /// </summary>
    /// <param name="pieceType">Type of piece to render</param>
    /// <param name="pieceColor">Color of the piece</param>
    /// <returns>Texture2D of the rendered piece</returns>
    public Texture2D RenderPiecePreview(ChessPieceType pieceType, PieceColor pieceColor)
    {
        Debug.Log($"PiecePreviewRenderer.RenderPiecePreview: ENTRY - {pieceColor} {pieceType}");
        
        if (previewCamera == null)
        {
            Debug.LogError("PiecePreviewRenderer.RenderPiecePreview: Preview camera not set up");
            return CreateFallbackTexture(pieceType);
        }
        
        if (renderTexture == null)
        {
            Debug.LogError("PiecePreviewRenderer.RenderPiecePreview: Render texture not set up");
            return CreateFallbackTexture(pieceType);
        }
        
        // Create temporary piece for preview
        Debug.Log($"PiecePreviewRenderer.RenderPiecePreview: Creating preview piece...");
        GameObject tempPiece = CreatePreviewPiece(pieceType, pieceColor);
        if (tempPiece == null)
        {
            Debug.LogError($"PiecePreviewRenderer.RenderPiecePreview: Failed to create preview piece for {pieceColor} {pieceType}");
            return CreateFallbackTexture(pieceType);
        }
        
        try
        {
            // Position piece in front of camera
            tempPiece.transform.position = Vector3.zero;
            tempPiece.transform.rotation = Quaternion.identity;
            
            Debug.Log($"PiecePreviewRenderer.RenderPiecePreview: Piece positioned at {tempPiece.transform.position}, layer: {tempPiece.layer}");
            
            // Check if piece has any renderers
            Renderer[] renderers = tempPiece.GetComponentsInChildren<Renderer>();
            Debug.Log($"PiecePreviewRenderer.RenderPiecePreview: Found {renderers.Length} renderers on piece");
            foreach (Renderer r in renderers)
            {
                Debug.Log($"PiecePreviewRenderer.RenderPiecePreview: Renderer on layer {r.gameObject.layer}, enabled: {r.enabled}");
            }
            
            // Render the piece
            Debug.Log("PiecePreviewRenderer.RenderPiecePreview: Rendering piece...");
            previewCamera.Render();
            
            // Convert render texture to Texture2D
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            
            Texture2D result = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            result.Apply();
            
            RenderTexture.active = previousActive;
            
            Debug.Log($"PiecePreviewRenderer.RenderPiecePreview: Successfully rendered {pieceColor} {pieceType} to {textureSize}x{textureSize} texture");
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PiecePreviewRenderer.RenderPiecePreview: Exception during rendering: {e.Message}");
            Debug.LogError($"PiecePreviewRenderer.RenderPiecePreview: Stack trace: {e.StackTrace}");
            return CreateFallbackTexture(pieceType);
        }
        finally
        {
            // Clean up temporary piece
            if (tempPiece != null)
            {
                Debug.Log("PiecePreviewRenderer.RenderPiecePreview: Cleaning up temporary piece");
                DestroyImmediate(tempPiece);
            }
        }
    }
    
    /// <summary>
    /// Create a temporary piece for preview rendering
    /// </summary>
    private GameObject CreatePreviewPiece(ChessPieceType pieceType, PieceColor pieceColor)
    {
        Debug.Log($"PiecePreviewRenderer.CreatePreviewPiece: ENTRY - {pieceColor} {pieceType}");
        
        GameObject prefab = GetPiecePrefab(pieceType);
        if (prefab == null)
        {
            Debug.LogError($"PiecePreviewRenderer.CreatePreviewPiece: No prefab found for {pieceType}");
            return null;
        }
        
        Debug.Log($"PiecePreviewRenderer.CreatePreviewPiece: Found prefab '{prefab.name}' for {pieceType}");
        
        try
        {
            // Instantiate the piece
            GameObject piece = Instantiate(prefab);
            piece.name = $"Preview_{pieceColor}_{pieceType}";
            
            Debug.Log($"PiecePreviewRenderer.CreatePreviewPiece: Instantiated piece '{piece.name}'");
            
            // Set to preview layer
            int layerNumber = Mathf.RoundToInt(Mathf.Log(previewLayer.value, 2));
            Debug.Log($"PiecePreviewRenderer.CreatePreviewPiece: Setting piece to layer {layerNumber}");
            SetLayerRecursively(piece, layerNumber);
            
            // Configure the piece
            ChessPiece chessPiece = piece.GetComponent<ChessPiece>();
            if (chessPiece != null)
            {
                Debug.Log($"PiecePreviewRenderer.CreatePreviewPiece: Found ChessPiece component, configuring...");
                chessPiece.pieceColor = pieceColor;
                
                try
                {
                    chessPiece.ApplyMaterial();
                    Debug.Log($"PiecePreviewRenderer.CreatePreviewPiece: Applied material for {pieceColor}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"PiecePreviewRenderer.CreatePreviewPiece: Failed to apply material: {e.Message}");
                }
                
                // Disable any scripts that might interfere
                chessPiece.enabled = false;
                Debug.Log($"PiecePreviewRenderer.CreatePreviewPiece: Disabled ChessPiece script");
            }
            else
            {
                Debug.LogWarning($"PiecePreviewRenderer.CreatePreviewPiece: No ChessPiece component found on {pieceType} prefab");
            }
            
            // Remove any colliders to prevent interference
            Collider[] colliders = piece.GetComponentsInChildren<Collider>();
            Debug.Log($"PiecePreviewRenderer.CreatePreviewPiece: Disabling {colliders.Length} colliders");
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            
            Debug.Log($"PiecePreviewRenderer.CreatePreviewPiece: Successfully created preview piece for {pieceColor} {pieceType}");
            return piece;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PiecePreviewRenderer.CreatePreviewPiece: Exception creating piece: {e.Message}");
            Debug.LogError($"PiecePreviewRenderer.CreatePreviewPiece: Stack trace: {e.StackTrace}");
            return null;
        }
    }
    
    /// <summary>
    /// Get the prefab for a specific piece type
    /// </summary>
    private GameObject GetPiecePrefab(ChessPieceType pieceType)
    {
        Debug.Log($"PiecePreviewRenderer.GetPiecePrefab: ENTRY - {pieceType}");
        
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("PiecePreviewRenderer.GetPiecePrefab: GameManager not found");
            return null;
        }
        
        Debug.Log($"PiecePreviewRenderer.GetPiecePrefab: GameManager found, calling GetPiecePrefab");
        
        // Use the new GameManager method that ensures prefabs exist
        GameObject prefab = gameManager.GetPiecePrefab(pieceType);
        
        if (prefab != null)
        {
            Debug.Log($"PiecePreviewRenderer.GetPiecePrefab: Successfully got {pieceType} prefab: {prefab.name}");
        }
        else
        {
            Debug.LogError($"PiecePreviewRenderer.GetPiecePrefab: GameManager returned NULL prefab for {pieceType}");
        }
        
        return prefab;
    }
    
    /// <summary>
    /// Set layer recursively for all child objects
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    /// <summary>
    /// Create a fallback texture with piece type text if 3D rendering fails
    /// </summary>
    private Texture2D CreateFallbackTexture(ChessPieceType pieceType)
    {
        Texture2D fallback = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[textureSize * textureSize];
        
        // Create a simple colored square as fallback
        Color pieceColor = GetFallbackColor(pieceType);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = pieceColor;
        }
        
        fallback.SetPixels(pixels);
        fallback.Apply();
        
        Debug.LogWarning($"PiecePreviewRenderer: Using fallback texture for {pieceType}");
        return fallback;
    }
    
    /// <summary>
    /// Get a fallback color for each piece type
    /// </summary>
    private Color GetFallbackColor(ChessPieceType pieceType)
    {
        switch (pieceType)
        {
            case ChessPieceType.Queen:
                return new Color(0.8f, 0.2f, 0.8f, 1f); // Purple
            case ChessPieceType.Rook:
                return new Color(0.2f, 0.2f, 0.8f, 1f); // Blue
            case ChessPieceType.Bishop:
                return new Color(0.2f, 0.8f, 0.2f, 1f); // Green
            case ChessPieceType.Knight:
                return new Color(0.8f, 0.6f, 0.2f, 1f); // Orange
            default:
                return Color.gray;
        }
    }
    
    /// <summary>
    /// Cache for rendered piece textures to avoid re-rendering
    /// </summary>
    private System.Collections.Generic.Dictionary<string, Texture2D> textureCache = 
        new System.Collections.Generic.Dictionary<string, Texture2D>();
    
    /// <summary>
    /// Get cached piece preview or render new one
    /// </summary>
    public Texture2D GetCachedPiecePreview(ChessPieceType pieceType, PieceColor pieceColor)
    {
        string key = $"{pieceColor}_{pieceType}";
        
        if (textureCache.ContainsKey(key))
        {
            return textureCache[key];
        }
        
        Texture2D texture = RenderPiecePreview(pieceType, pieceColor);
        textureCache[key] = texture;
        return texture;
    }
    
    /// <summary>
    /// Clear texture cache
    /// </summary>
    public void ClearCache()
    {
        foreach (var texture in textureCache.Values)
        {
            if (texture != null)
            {
                DestroyImmediate(texture);
            }
        }
        textureCache.Clear();
    }
    
    private void OnDestroy()
    {
        ClearCache();
        
        if (renderTexture != null)
        {
            renderTexture.Release();
            DestroyImmediate(renderTexture);
        }
    }
}