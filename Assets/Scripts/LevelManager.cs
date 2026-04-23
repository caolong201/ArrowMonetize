using UnityEngine;

[System.Serializable]
public class LevelData
{
    public int levelNumber;
    public int boardWidth;
    public int boardHeight;
    public DifficultySettings difficultySettings;
    
    public LevelData()
    {
        difficultySettings = new DifficultySettings();
        difficultySettings.boardWidth = boardWidth;
        difficultySettings.boardHeight = boardHeight;
    }
}

public class LevelManager : MonoBehaviour
{
    private const string CURRENT_LEVEL_KEY = "CurrentLevel";
    
    [Header("Level Settings")]
    public int currentLevel = 1;
    public LevelData[] levelData;
    
    [Header("Default Board Size")]
    public int defaultWidth = 7;
    public int defaultHeight = 10;
    
    private BoardManager boardManager;
    private UIManager uiManager;
    
    void Awake()
    {
        boardManager = FindFirstObjectByType<BoardManager>();
        uiManager = FindFirstObjectByType<UIManager>();
        
        // Initialize default level data if not set
        if (levelData == null || levelData.Length == 0)
        {
            CreateDefaultLevelData();
        }
        
        // Load saved level
        LoadSavedLevel();
    }
    
    void Start()
    {
        // Don't auto-load level on start, wait for Play button
        // Level will be loaded when user clicks Play button
    }
    
    void LoadSavedLevel()
    {
        if (PlayerPrefs.HasKey(CURRENT_LEVEL_KEY))
        {
            currentLevel = PlayerPrefs.GetInt(CURRENT_LEVEL_KEY);
            Debug.Log($"Loaded saved level: {currentLevel}");
        }
        else
        {
            // First time playing, start at level 1
            currentLevel = 1;
            SaveCurrentLevel();
        }
    }
    
    void SaveCurrentLevel()
    {
        PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, currentLevel);
        PlayerPrefs.Save();
        Debug.Log($"Saved level: {currentLevel}");
    }
    
    void CreateDefaultLevelData()
    {
        // Create some default levels with varying difficulties
        levelData = new LevelData[]
        {
            // Level 1 - Easy
            new LevelData 
            { 
                levelNumber = 1, 
                boardWidth = 7, 
                boardHeight = 10,
                difficultySettings = new DifficultySettings
                {
                    boardWidth = 7,
                    boardHeight = 10,
                    guaranteedPaths = 2,
                    pathComplexity = 0.3f,
                    obstacleDensity = 0.2f,
                    arrowsPointingToCenter = 0.1f
                }
            },
            // Level 2 - Medium
            new LevelData 
            { 
                levelNumber = 2, 
                boardWidth = 8, 
                boardHeight = 10,
                difficultySettings = new DifficultySettings
                {
                    boardWidth = 8,
                    boardHeight = 10,
                    guaranteedPaths = 1,
                    pathComplexity = 0.4f,
                    obstacleDensity = 0.3f,
                    arrowsPointingToCenter = 0.2f
                }
            },
            // Level 3 - Medium-Hard
            new LevelData 
            { 
                levelNumber = 3, 
                boardWidth = 9, 
                boardHeight = 11,
                difficultySettings = new DifficultySettings
                {
                    boardWidth = 9,
                    boardHeight = 11,
                    guaranteedPaths = 1,
                    pathComplexity = 0.5f,
                    obstacleDensity = 0.4f,
                    arrowsPointingToCenter = 0.3f
                }
            },
            // Level 4 - Hard
            new LevelData 
            { 
                levelNumber = 4, 
                boardWidth = 10, 
                boardHeight = 12,
                difficultySettings = new DifficultySettings
                {
                    boardWidth = 10,
                    boardHeight = 12,
                    guaranteedPaths = 1,
                    pathComplexity = 0.6f,
                    obstacleDensity = 0.5f,
                    arrowsPointingToCenter = 0.4f
                }
            },
            // Level 5 - Very Hard
            new LevelData 
            { 
                levelNumber = 5, 
                boardWidth = 11, 
                boardHeight = 13,
                difficultySettings = new DifficultySettings
                {
                    boardWidth = 11,
                    boardHeight = 13,
                    guaranteedPaths = 1,
                    pathComplexity = 0.7f,
                    obstacleDensity = 0.6f,
                    arrowsPointingToCenter = 0.5f,
                    maxClicks = 20
                }
            },
        };
    }
    
    public void LoadLevel(int level, bool updateCurrentLevel = true)
    {
        if (updateCurrentLevel)
        {
            currentLevel = level;
            SaveCurrentLevel();
        }
        
        // Check if this is level 1
        if (level == 1 && boardManager != null)
        {
            boardManager.InitializeLevel1Board();
            Debug.Log("Level 1 loaded: 2x2 board with 3 arrows in L-shape");
            return;
        }
        
        // Check if this is level 2
        if (level == 2 && boardManager != null)
        {
            boardManager.InitializeLevel2Board();
            Debug.Log("Level 2 loaded: 2x2 board with 3 arrows custom layout");
            return;
        }
        
        // Check if this is level 3
        if (level == 3 && boardManager != null)
        {
            boardManager.InitializeLevel3Board();
            Debug.Log("Level 3 loaded: 2x2 board with 3 arrows custom layout");
            return;
        }
        
        // Check if this is level 4
        if (level == 4 && boardManager != null)
        {
            boardManager.InitializeLevel4Board();
            Debug.Log("Level 4 loaded: 2x2 board with 4 arrows custom layout");
            return;
        }
        
        LevelData data = GetLevelData(level);
        
        if (data != null)
        {
            if (boardManager != null)
            {
                // Use difficulty settings if available, otherwise use basic settings
                if (data.difficultySettings != null)
                {
                    boardManager.InitializeBoard(data.difficultySettings);
                }
                else
                {
                    boardManager.InitializeBoard(data.boardWidth, data.boardHeight);
                }
            }
            
            Debug.Log($"Level {level} loaded: {data.boardWidth}x{data.boardHeight}");
        }
        else
        {
            // Use default size
            if (boardManager != null)
            {
                boardManager.InitializeBoard(defaultWidth, defaultHeight);
            }
            
            Debug.Log($"Level {level} loaded with default size: {defaultWidth}x{defaultHeight}");
        }
        
    }
    
    LevelData GetLevelData(int level)
    {
        foreach (LevelData data in levelData)
        {
            if (data.levelNumber == level)
            {
                return data;
            }
        }
        
        // If level not found, create a new one with default size and difficulty
        LevelData newData = new LevelData 
        { 
            levelNumber = level, 
            boardWidth = defaultWidth, 
            boardHeight = defaultHeight 
        };
        
        // Calculate difficulty based on level
        int difficultyLevel = Mathf.Min(level / 5, 4); // Scale difficulty every 5 levels
        newData.difficultySettings = new DifficultySettings
        {
            boardWidth = defaultWidth + difficultyLevel,
            boardHeight = defaultHeight + difficultyLevel,
            guaranteedPaths = Mathf.Max(0, 2 - difficultyLevel),
            obstacleDensity = Mathf.Min(0.7f, 0.2f + difficultyLevel * 0.1f),
            arrowsPointingToCenter = Mathf.Min(0.6f, 0.1f + difficultyLevel * 0.1f)
        };
        
        return newData;
    }
    
    public void OnLevelCompleted()
    {
        Debug.Log($"Level {currentLevel} completed!");
        
        // Auto increase level
        currentLevel++;
        SaveCurrentLevel();
        
        // Show win screen
        if (uiManager != null)
        {
            uiManager.ShowWinScreen();
        }
        
    }
    
    public void LoadNextLevel()
    {
        currentLevel++;
        SaveCurrentLevel();
        LoadLevel(currentLevel);
    }
    
    public int GetCurrentLevel()
    {
        return currentLevel;
    }
}

