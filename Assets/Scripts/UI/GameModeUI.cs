using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for selecting game modes (Human vs Human, Human vs AI, AI vs AI)
/// and AI difficulty settings
/// </summary>
public class GameModeUI : MonoBehaviour
{
    [Header("UI References")]
    public Button humanVsHumanButton;
    public Button humanVsAIButton;
    public Button aiVsAIButton;
    public Button startGameButton;
    public TextMeshProUGUI gameModeText;
    public TextMeshProUGUI difficultyText;
    
    [Header("AI Difficulty")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;
    
    [Header("Player Selection")]
    public Button playAsWhiteButton;
    public Button playAsBlackButton;
    public TextMeshProUGUI playerColorText;
    
    private PlayerType whitePlayerType = PlayerType.Human;
    private PlayerType blackPlayerType = PlayerType.Human;
    private AIDifficulty selectedDifficulty = AIDifficulty.Medium;
    private PieceColor humanPlayerColor = PieceColor.White; // For Human vs AI games
    
    public static GameModeUI Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameModeUI: Instance created");
        }
        else
        {
            Debug.LogWarning("GameModeUI: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        SetupButtons();
        UpdateUI();
    }
    
    /// <summary>
    /// Setup button click events
    /// </summary>
    private void SetupButtons()
    {
        if (humanVsHumanButton != null)
            humanVsHumanButton.onClick.AddListener(() => SetGameMode(PlayerType.Human, PlayerType.Human));
        
        if (humanVsAIButton != null)
            humanVsAIButton.onClick.AddListener(() => SetHumanVsAIMode());
        
        if (aiVsAIButton != null)
            aiVsAIButton.onClick.AddListener(() => SetGameMode(PlayerType.Computer, PlayerType.Computer));
        
        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);
        
        // Difficulty buttons
        if (easyButton != null)
            easyButton.onClick.AddListener(() => SetDifficulty(AIDifficulty.Easy));
        
        if (mediumButton != null)
            mediumButton.onClick.AddListener(() => SetDifficulty(AIDifficulty.Medium));
        
        if (hardButton != null)
            hardButton.onClick.AddListener(() => SetDifficulty(AIDifficulty.Hard));
        
        // Player color selection for Human vs AI
        if (playAsWhiteButton != null)
            playAsWhiteButton.onClick.AddListener(() => SetHumanPlayerColor(PieceColor.White));
        
        if (playAsBlackButton != null)
            playAsBlackButton.onClick.AddListener(() => SetHumanPlayerColor(PieceColor.Black));
    }
    
    /// <summary>
    /// Set the game mode (player types for white and black)
    /// </summary>
    public void SetGameMode(PlayerType whiteType, PlayerType blackType)
    {
        whitePlayerType = whiteType;
        blackPlayerType = blackType;
        
        Debug.Log($"GameModeUI: Game mode set to {whiteType} vs {blackType}");
        UpdateUI();
    }
    
    /// <summary>
    /// Set Human vs AI mode based on selected human player color
    /// </summary>
    private void SetHumanVsAIMode()
    {
        if (humanPlayerColor == PieceColor.White)
        {
            SetGameMode(PlayerType.Human, PlayerType.Computer);
        }
        else
        {
            SetGameMode(PlayerType.Computer, PlayerType.Human);
        }
    }
    
    /// <summary>
    /// Set which color the human player wants to play as (for Human vs AI games)
    /// </summary>
    public void SetHumanPlayerColor(PieceColor color)
    {
        humanPlayerColor = color;
        
        // Update game mode if we're currently in Human vs AI mode
        if (IsHumanVsAI())
        {
            SetHumanVsAIMode();
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// Set AI difficulty level
    /// </summary>
    public void SetDifficulty(AIDifficulty difficulty)
    {
        selectedDifficulty = difficulty;
        
        // Apply difficulty to AI player if it exists
        if (AIPlayer.Instance != null)
        {
            AIPlayer.Instance.SetDifficulty(difficulty);
        }
        
        Debug.Log($"GameModeUI: AI difficulty set to {difficulty}");
        UpdateUI();
    }
    
    /// <summary>
    /// Start the game with selected settings
    /// </summary>
    public void StartGame()
    {
        Debug.Log($"GameModeUI: Starting game - {whitePlayerType} vs {blackPlayerType}, Difficulty: {selectedDifficulty}");
        
        // Apply AI difficulty
        if (AIPlayer.Instance != null)
        {
            AIPlayer.Instance.SetDifficulty(selectedDifficulty);
        }
        
        // Set game mode in TurnManager
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.SetGameMode(whitePlayerType, blackPlayerType);
        }
        
        // Ensure we're in playing state to start the game
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameState.Playing);
        }
        
        // Hide the game mode UI (optional)
        HideUI();
    }
    
    /// <summary>
    /// Update UI text and button states
    /// </summary>
    private void UpdateUI()
    {
        // Update game mode text
        if (gameModeText != null)
        {
            string modeDescription = GetGameModeDescription();
            gameModeText.text = $"Game Mode: {modeDescription}";
        }
        
        // Update difficulty text
        if (difficultyText != null)
        {
            difficultyText.text = $"AI Difficulty: {selectedDifficulty}";
        }
        
        // Update player color text
        if (playerColorText != null)
        {
            if (IsHumanVsAI())
            {
                playerColorText.text = $"You play as: {humanPlayerColor}";
                playerColorText.gameObject.SetActive(true);
            }
            else
            {
                playerColorText.gameObject.SetActive(false);
            }
        }
        
        // Show/hide difficulty controls based on whether AI is involved
        bool hasAI = whitePlayerType == PlayerType.Computer || blackPlayerType == PlayerType.Computer;
        
        if (easyButton != null) easyButton.gameObject.SetActive(hasAI);
        if (mediumButton != null) mediumButton.gameObject.SetActive(hasAI);
        if (hardButton != null) hardButton.gameObject.SetActive(hasAI);
        if (difficultyText != null) difficultyText.gameObject.SetActive(hasAI);
        
        // Show/hide player color selection for Human vs AI
        bool isHumanVsAI = IsHumanVsAI();
        if (playAsWhiteButton != null) playAsWhiteButton.gameObject.SetActive(isHumanVsAI);
        if (playAsBlackButton != null) playAsBlackButton.gameObject.SetActive(isHumanVsAI);
    }
    
    /// <summary>
    /// Get a user-friendly description of the current game mode
    /// </summary>
    private string GetGameModeDescription()
    {
        if (whitePlayerType == PlayerType.Human && blackPlayerType == PlayerType.Human)
        {
            return "Human vs Human";
        }
        else if (whitePlayerType == PlayerType.Computer && blackPlayerType == PlayerType.Computer)
        {
            return "AI vs AI";
        }
        else
        {
            string humanColor = whitePlayerType == PlayerType.Human ? "White" : "Black";
            return $"Human ({humanColor}) vs AI";
        }
    }
    
    /// <summary>
    /// Check if current mode is Human vs AI
    /// </summary>
    private bool IsHumanVsAI()
    {
        return (whitePlayerType == PlayerType.Human && blackPlayerType == PlayerType.Computer) ||
               (whitePlayerType == PlayerType.Computer && blackPlayerType == PlayerType.Human);
    }
    
    /// <summary>
    /// Show the game mode selection UI
    /// </summary>
    public void ShowUI()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// Hide the game mode selection UI
    /// </summary>
    public void HideUI()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Quick setup method for testing - Human vs AI on Medium difficulty
    /// </summary>
    public void QuickSetupHumanVsAI()
    {
        SetHumanPlayerColor(PieceColor.White);
        SetGameMode(PlayerType.Human, PlayerType.Computer);
        SetDifficulty(AIDifficulty.Medium);
        
        Debug.Log("GameModeUI: Quick setup complete - Human (White) vs AI (Medium)");
    }
    
    /// <summary>
    /// Quick setup method for testing - AI vs AI on Easy difficulty
    /// </summary>
    public void QuickSetupAIVsAI()
    {
        SetGameMode(PlayerType.Computer, PlayerType.Computer);
        SetDifficulty(AIDifficulty.Easy);
        
        Debug.Log("GameModeUI: Quick setup complete - AI vs AI (Easy)");
    }
}