using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages piece sprites for UI elements like the mini tray.
/// Provides sprite lookup for different piece types and colors.
/// </summary>
public class PieceSpriteManager : MonoBehaviour
{
    [Header("Piece Sprites")]
    [SerializeField] private Sprite whitePawnSprite;
    [SerializeField] private Sprite whiteRookSprite;
    [SerializeField] private Sprite whiteKnightSprite;
    [SerializeField] private Sprite whiteBishopSprite;
    [SerializeField] private Sprite whiteQueenSprite;
    [SerializeField] private Sprite whiteKingSprite;
    
    [SerializeField] private Sprite blackPawnSprite;
    [SerializeField] private Sprite blackRookSprite;
    [SerializeField] private Sprite blackKnightSprite;
    [SerializeField] private Sprite blackBishopSprite;
    [SerializeField] private Sprite blackQueenSprite;
    [SerializeField] private Sprite blackKingSprite;
    
    [Header("Default Sprites")]
    [SerializeField] private Sprite defaultPieceSprite; // Fallback sprite
    
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    
    public static PieceSpriteManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSpriteCache();
            Debug.Log("✅ PieceSpriteManager: Instance created and initialized");
        }
        else
        {
            Debug.LogWarning("⚠️ PieceSpriteManager: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Initialize the sprite cache for fast lookup
    /// </summary>
    private void InitializeSpriteCache()
    {
        // White pieces
        AddToCache(PieceColor.White, ChessPieceType.Pawn, whitePawnSprite);
        AddToCache(PieceColor.White, ChessPieceType.Rook, whiteRookSprite);
        AddToCache(PieceColor.White, ChessPieceType.Knight, whiteKnightSprite);
        AddToCache(PieceColor.White, ChessPieceType.Bishop, whiteBishopSprite);
        AddToCache(PieceColor.White, ChessPieceType.Queen, whiteQueenSprite);
        AddToCache(PieceColor.White, ChessPieceType.King, whiteKingSprite);
        
        // Black pieces
        AddToCache(PieceColor.Black, ChessPieceType.Pawn, blackPawnSprite);
        AddToCache(PieceColor.Black, ChessPieceType.Rook, blackRookSprite);
        AddToCache(PieceColor.Black, ChessPieceType.Knight, blackKnightSprite);
        AddToCache(PieceColor.Black, ChessPieceType.Bishop, blackBishopSprite);
        AddToCache(PieceColor.Black, ChessPieceType.Queen, blackQueenSprite);
        AddToCache(PieceColor.Black, ChessPieceType.King, blackKingSprite);
        
        // For other colors (Green, Purple, Yellow, Orange), use default sprite for now
        // This can be expanded later with color-specific sprites
        foreach (PieceColor color in System.Enum.GetValues(typeof(PieceColor)))
        {
            if (color != PieceColor.White && color != PieceColor.Black)
            {
                foreach (ChessPieceType type in System.Enum.GetValues(typeof(ChessPieceType)))
                {
                    if (!HasCachedSprite(color, type))
                    {
                        AddToCache(color, type, defaultPieceSprite);
                    }
                }
            }
        }
        
        Debug.Log($"🎨 PieceSpriteManager: Sprite cache initialized with {spriteCache.Count} entries");
    }
    
    /// <summary>
    /// Add a sprite to the cache
    /// </summary>
    private void AddToCache(PieceColor color, ChessPieceType type, Sprite sprite)
    {
        string key = GetCacheKey(color, type);
        if (sprite != null)
        {
            spriteCache[key] = sprite;
        }
        else
        {
            Debug.LogWarning($"⚠️ PieceSpriteManager: Missing sprite for {color} {type}, using default");
            spriteCache[key] = defaultPieceSprite;
        }
    }
    
    /// <summary>
    /// Check if a sprite is cached for the given piece
    /// </summary>
    private bool HasCachedSprite(PieceColor color, ChessPieceType type)
    {
        string key = GetCacheKey(color, type);
        return spriteCache.ContainsKey(key) && spriteCache[key] != null;
    }
    
    /// <summary>
    /// Get cache key for piece color and type
    /// </summary>
    private string GetCacheKey(PieceColor color, ChessPieceType type)
    {
        return $"{color}_{type}";
    }
    
    /// <summary>
    /// Get sprite for a piece
    /// </summary>
    public Sprite GetPieceSprite(PieceColor color, ChessPieceType type)
    {
        string key = GetCacheKey(color, type);
        
        if (spriteCache.TryGetValue(key, out Sprite sprite) && sprite != null)
        {
            return sprite;
        }
        
        Debug.LogWarning($"⚠️ PieceSpriteManager: No sprite found for {color} {type}, using default");
        return defaultPieceSprite;
    }
    
    /// <summary>
    /// Get sprite for a chess piece component
    /// </summary>
    public Sprite GetPieceSprite(ChessPiece piece)
    {
        if (piece == null)
        {
            Debug.LogWarning("⚠️ PieceSpriteManager: Piece is null, using default sprite");
            return defaultPieceSprite;
        }
        
        return GetPieceSprite(piece.pieceColor, piece.pieceType);
    }
    
    /// <summary>
    /// Create a default circle sprite as fallback
    /// </summary>
    private Sprite CreateDefaultSprite()
    {
        // Create a simple circular sprite as default
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);
                
                if (distance <= radius)
                {
                    // Create a simple piece-like shape
                    float alpha = Mathf.Clamp01(radius - distance + 1f);
                    pixels[y * size + x] = new Color(0.8f, 0.8f, 0.8f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// Initialize with default sprites if none are assigned
    /// </summary>
    private void InitializeDefaultSprites()
    {
        if (defaultPieceSprite == null)
        {
            defaultPieceSprite = CreateDefaultSprite();
            Debug.Log("🎨 PieceSpriteManager: Created default piece sprite");
        }
        
        // If specific sprites are null, assign the default
        if (whitePawnSprite == null) whitePawnSprite = defaultPieceSprite;
        if (whiteRookSprite == null) whiteRookSprite = defaultPieceSprite;
        if (whiteKnightSprite == null) whiteKnightSprite = defaultPieceSprite;
        if (whiteBishopSprite == null) whiteBishopSprite = defaultPieceSprite;
        if (whiteQueenSprite == null) whiteQueenSprite = defaultPieceSprite;
        if (whiteKingSprite == null) whiteKingSprite = defaultPieceSprite;
        
        if (blackPawnSprite == null) blackPawnSprite = defaultPieceSprite;
        if (blackRookSprite == null) blackRookSprite = defaultPieceSprite;
        if (blackKnightSprite == null) blackKnightSprite = defaultPieceSprite;
        if (blackBishopSprite == null) blackBishopSprite = defaultPieceSprite;
        if (blackQueenSprite == null) blackQueenSprite = defaultPieceSprite;
        if (blackKingSprite == null) blackKingSprite = defaultPieceSprite;
    }
    
    private void Start()
    {
        // Initialize default sprites if needed
        InitializeDefaultSprites();
        
        // Re-initialize cache with potentially updated sprites
        InitializeSpriteCache();
    }
}