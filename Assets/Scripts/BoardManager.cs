using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int boardWidth = 7;
    public int boardHeight = 10;
    public float cellSize = 1f;
    public float cellSpacing = 0.1f;
    
    [Header("Prefabs")]
    public GameObject arrowCellPrefab;
    
    [Header("Board Container")]
    public Transform boardContainer;
    
    [Header("Animation Settings")]
    public float moveSpeed = 0.15f; // Time to move each cell (seconds)
    
    [Header("Board Init Animation")]
    public float spawnAnimationDuration = 0.5f; // Animation duration for spawning each arrow
    public float spawnStaggerDelay = 0.02f; // Delay between arrows (creates wave effect)
    public bool animateFromCenter = true; // Animation from center outward or top to bottom
    
    [Header("Shuffle Animation")]
    public float shuffleAnimationDuration = 0.3f; // Shuffle animation duration (faster than spawn)
    public float shuffleStaggerDelay = 0.01f; // Delay between arrows when shuffling (faster)
    
    [Header("Lose Animation")]
    public float loseFallDuration = 1.5f; // Fall animation duration
    public float loseStaggerDelay = 0.03f; // Delay between arrows when falling
    public float loseFallDistance = 10f; // Fall distance
    
    [Header("Collision Animation")]
    public float collisionShakeStrength = 0.2f; // Shake strength on collision
    public float collisionShakeDuration = 0.5f; // Collision animation duration
    public int collisionVibrato = 15; // Number of vibrations
    public float collisionScaleBounce = 1.3f; // Scale on collision
    public float collisionImpactDistance = 0.3f; // Movement distance on collision
    
    private ArrowCell[,] board;
    private ArrowCell specialArrow;
    [SerializeField] private LevelManager levelManager;
    private LivesManager livesManager;
    private bool isMoving = false;
    private Coroutine currentMoveCoroutine;
    
    [Header("Hint Settings")]
    public Color hintColor = new Color(1f, 0.8f, 0f, 1f); // Yellow color for hint
    public float hintDuration = 3f; // Hint display duration (seconds)
    private ArrowCell highlightedArrow = null;
    private Coroutine hintCoroutine;
    
    // Store current difficulty settings for reload
    private DifficultySettings currentDifficultySettings;
    
    // Track arrows that have already lost a life (to prevent multiple life loss for same arrow)
    private HashSet<ArrowCell> arrowsThatLostLife = new HashSet<ArrowCell>();
    
    void Awake()
    {
        livesManager = FindFirstObjectByType<LivesManager>();
        if (livesManager == null)
        {
            GameObject livesObj = new GameObject("LivesManager");
            livesManager = livesObj.AddComponent<LivesManager>();
        }
        
        // Subscribe to lives depleted event
        if (livesManager != null)
        {
            livesManager.OnLivesDepleted += OnAllLivesDepleted;
        }
    }
    
    void OnDestroy()
    {
        if (livesManager != null)
        {
            livesManager.OnLivesDepleted -= OnAllLivesDepleted;
        }
    }
    
    void OnAllLivesDepleted()
    {
        Debug.Log("All lives depleted! Animating arrows falling...");
        
        // Animate all arrows falling down
        AnimateArrowsFalling();
        
        // Show lose screen after a delay
        StartCoroutine(DelayedShowLose());
    }
    
    public void TriggerLoseSequence()
    {
        OnAllLivesDepleted();
    }
    
    void AnimateArrowsFalling()
    {
        if (board == null) return;
        
        List<ArrowCell> allArrows = new List<ArrowCell>();
        
        // Collect all arrows on the board
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (board[y, x] != null)
                {
                    ArrowCell arrow = board[y, x];
                    allArrows.Add(arrow);
                }
            }
        }
        
        if (allArrows.Count == 0) return;
        
        // Shuffle arrows for random fall order
        for (int i = 0; i < allArrows.Count; i++)
        {
            ArrowCell temp = allArrows[i];
            int randomIndex = Random.Range(i, allArrows.Count);
            allArrows[i] = allArrows[randomIndex];
            allArrows[randomIndex] = temp;
        }
        
        // Animate each arrow falling with staggered timing
        for (int i = 0; i < allArrows.Count; i++)
        {
            ArrowCell arrow = allArrows[i];
            float delay = i * loseStaggerDelay;
            
            AnimateArrowFalling(arrow, delay);
        }
    }
    
    void AnimateArrowFalling(ArrowCell arrow, float delay)
    {
        if (arrow == null || arrow.gameObject == null) return;
        
        // Kill any existing tweens
        arrow.transform.DOKill();
        
        Vector3 originalPos = arrow.transform.localPosition;
        Vector3 originalRotation = arrow.transform.localEulerAngles;
        Vector3 originalScale = arrow.transform.localScale;
        
        // Random rotation direction and amount for tumbling effect
        float randomRotation = Random.Range(180f, 720f);
        bool rotateLeft = Random.value > 0.5f;
        if (rotateLeft) randomRotation = -randomRotation;
        
        // Target position: fall straight down on Y axis only
        Vector3 fallTargetPos = originalPos + new Vector3(0f, -loseFallDistance, 0f);
        
        // Create falling sequence
        Sequence fallSequence = DOTween.Sequence();
        
        // 1. Slight scale up (anticipation)
        fallSequence.Append(arrow.transform.DOScale(originalScale * 1.1f, loseFallDuration * 0.1f)
            .SetEase(Ease.OutQuad)
            .SetDelay(delay));
        
        // 2. Start falling down with gravity-like easing
        fallSequence.Append(arrow.transform.DOLocalMove(fallTargetPos, loseFallDuration * 0.9f)
            .SetEase(Ease.InQuad) // Accelerate like gravity
            .SetDelay(delay));
        
        // 3. Rotate while falling (tumbling effect)
        fallSequence.Join(arrow.transform.DORotate(originalRotation + new Vector3(0, 0, randomRotation), loseFallDuration * 0.9f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetDelay(delay));
        
        // 4. Scale down while falling
        fallSequence.Join(arrow.transform.DOScale(originalScale * 0.3f, loseFallDuration * 0.9f)
            .SetEase(Ease.InQuad)
            .SetDelay(delay));
        
        // 5. Fade out while falling
        UnityEngine.UI.Image backgroundImage = arrow.GetComponent<UnityEngine.UI.Image>();
        if (backgroundImage != null)
        {
            fallSequence.Join(backgroundImage.DOFade(0f, loseFallDuration * 0.7f)
                .SetDelay(delay + loseFallDuration * 0.2f));
        }
        
        if (arrow.arrowImage != null)
        {
            fallSequence.Join(arrow.arrowImage.DOFade(0f, loseFallDuration * 0.7f)
                .SetDelay(delay + loseFallDuration * 0.2f));
        }
        
        // Destroy arrow after falling
        fallSequence.OnComplete(() =>
        {
            if (arrow != null && arrow.gameObject != null)
            {
                Destroy(arrow.gameObject);
            }
        });
    }
    
    IEnumerator DelayedShowLose()
    {
        // Wait for fall animation to complete
        float waitTime = (boardHeight * boardWidth * loseStaggerDelay) + loseFallDuration;
        yield return new WaitForSeconds(Mathf.Min(waitTime, 2.5f)); // Cap at 2.5 seconds
        
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowLose();
        }
    }
    
    public void InitializeBoard(int width, int height, bool resetLives = true)
    {
        DifficultySettings settings = new DifficultySettings
        {
            boardWidth = width,
            boardHeight = height,
            guaranteedPaths = 0,
            obstacleDensity = 0.3f,
            arrowsPointingToCenter = 0.2f
        };
        InitializeBoard(settings, resetLives);
    }
    
    public void InitializeBoard(DifficultySettings settings, bool resetLives = true)
    {
        // Store settings for reload
        currentDifficultySettings = settings;
        
        boardWidth = settings.boardWidth;
        boardHeight = settings.boardHeight;
        
        // Reset lives when starting new level
        if (resetLives && livesManager != null)
        {
            livesManager.ResetLives();
        }
        
        // Clear tracked arrows that lost life
        arrowsThatLostLife.Clear();
        
        // Generate board with validation - retry if no arrows can move
        const int maxRetries = 50;
        int retryCount = 0;
        bool boardValid = false;
        List<ArrowCell> createdArrows = null;
        int centerX = 0;
        int centerY = 0;
        
        while (!boardValid && retryCount < maxRetries)
        {
            // Clear existing board
            ClearBoard();
            
            // Create new board
            board = new ArrowCell[boardHeight, boardWidth];
            
            // Calculate center position for special arrow
            centerX = boardWidth / 2;
            centerY = boardHeight / 2;
            
            // Always generate at least 1 guaranteed path
            int pathCount = Mathf.Max(1, settings.guaranteedPaths);
            Dictionary<Vector2Int, ArrowDirection> pathDirections = GenerateComplexPathsWithDirections(centerX, centerY, pathCount, settings.pathComplexity);
            
            // Create board cells
            createdArrows = new List<ArrowCell>();
            
            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    bool isOnPath = pathDirections.ContainsKey(pos);
                    ArrowDirection pathDirection = isOnPath ? pathDirections[pos] : ArrowDirection.Up; // Default, not used if not on path
                    ArrowCell arrow = CreateArrowCell(x, y, centerX, centerY, settings, isOnPath, pathDirection);
                    if (arrow != null)
                    {
                        createdArrows.Add(arrow);
                    }
                }
            }
            
            // Validate that at least one arrow can move
            boardValid = HasAnyMovableArrow();
            
            if (!boardValid)
            {
                retryCount++;
                Debug.LogWarning($"Generated board with no movable arrows (retry {retryCount}/{maxRetries}). Regenerating...");
            }
        }
        
        if (!boardValid)
        {
            Debug.LogError($"Failed to generate valid board after {maxRetries} attempts. Board may have no movable arrows.");
        }
        
        // Animate all arrows with stagger effect
        if (createdArrows != null && createdArrows.Count > 0)
        {
            AnimateBoardSpawn(createdArrows, centerX, centerY);
        }
    }
    
    public void InitializeLevel1Board(bool resetLives = true)
    {
        // Level 1 board: 2x2 grid with only 3 arrows in L-shape
        boardWidth = 2;
        boardHeight = 2;
        
        // Reset lives when starting level
        if (resetLives && livesManager != null)
        {
            livesManager.ResetLives();
        }
        
        // Clear tracked arrows that lost life
        arrowsThatLostLife.Clear();
        
        // Clear existing board
        ClearBoard();
        
        // Create new board
        board = new ArrowCell[boardHeight, boardWidth];
        
        // Hardcode arrow positions based on image:
        // Top-left (0,0): Up arrow (gray)
        // Top-right (1,0): Left arrow (gray)
        // Bottom-right (1,1): Up arrow (yellow/special)
        
        int specialX = 1; // Bottom-right
        int specialY = 1;
        
        // Create board cells - only 3 arrows
        List<ArrowCell> createdArrows = new List<ArrowCell>();
        
        // Top-left: Up arrow
        ArrowCell arrow1 = CreateArrowCellForTutorial(0, 0, ArrowDirection.Up, false);
        if (arrow1 != null)
        {
            createdArrows.Add(arrow1);
        }
        
        // Top-right: Left arrow
        ArrowCell arrow2 = CreateArrowCellForTutorial(1, 0, ArrowDirection.Left, false);
        if (arrow2 != null)
        {
            createdArrows.Add(arrow2);
        }
        
        // Bottom-right: Up arrow (special)
        ArrowCell arrow3 = CreateArrowCellForTutorial(1, 1, ArrowDirection.Up, true);
        if (arrow3 != null)
        {
            createdArrows.Add(arrow3);
        }
        
        // Animate all arrows with stagger effect
        AnimateBoardSpawn(createdArrows, specialX, specialY);
    }
    
    public void InitializeLevel2Board(bool resetLives = true)
    {
        // Level 2 board: 2x2 grid with only 3 arrows
        boardWidth = 2;
        boardHeight = 2;
        
        // Reset lives when starting level
        if (resetLives && livesManager != null)
        {
            livesManager.ResetLives();
        }
        
        // Clear tracked arrows that lost life
        arrowsThatLostLife.Clear();
        
        // Clear existing board
        ClearBoard();
        
        // Create new board
        board = new ArrowCell[boardHeight, boardWidth];
        
        // Hardcode arrow positions:
        // Top-left (x=0, y=0): Empty
        // Top-right (x=1, y=0): Right arrow (gray)
        // Bottom-left (x=0, y=1): Right arrow (yellow/special)
        // Bottom-right (x=1, y=1): Up arrow (gray)
        
        int specialX = 0; // Bottom-left (x=0)
        int specialY = 1; // Bottom-left (y=1)
        
        // Create board cells - only 3 arrows
        List<ArrowCell> createdArrows = new List<ArrowCell>();
        
        // Top-right (x=1, y=0): Right arrow (gray)
        ArrowCell arrow1 = CreateArrowCellForTutorial(1, 0, ArrowDirection.Right, false);
        if (arrow1 != null)
        {
            createdArrows.Add(arrow1);
        }
        
        // Bottom-left (x=0, y=1): Right arrow (special)
        ArrowCell arrow2 = CreateArrowCellForTutorial(0, 1, ArrowDirection.Right, true);
        if (arrow2 != null)
        {
            createdArrows.Add(arrow2);
        }
        
        // Bottom-right (x=1, y=1): Up arrow (gray)
        ArrowCell arrow3 = CreateArrowCellForTutorial(1, 1, ArrowDirection.Up, false);
        if (arrow3 != null)
        {
            createdArrows.Add(arrow3);
        }
        
        // Animate all arrows with stagger effect
        AnimateBoardSpawn(createdArrows, specialX, specialY);
    }

    public void InitializeLevel3Board(bool resetLives = true)
    {
        // Level 3 board: 2x2 grid with 3 arrows
        boardWidth = 2;
        boardHeight = 2;
        
        // Reset lives when starting level
        if (resetLives && livesManager != null)
        {
            livesManager.ResetLives();
        }
        
        // Clear tracked arrows that lost life
        arrowsThatLostLife.Clear();
        
        // Clear existing board
        ClearBoard();
        
        // Create new board
        board = new ArrowCell[boardHeight, boardWidth];
        
        // Hardcode arrow positions based on image:
        // Top-left (x=0, y=0): Down arrow (yellow/special)
        // Top-right (x=1, y=0): Empty
        // Bottom-left (x=0, y=1): Right arrow (gray)
        // Bottom-right (x=1, y=1): Up arrow (gray)
        
        int specialX = 0; // Top-left
        int specialY = 0; // Top-left
        
        // Create board cells - only 3 arrows
        List<ArrowCell> createdArrows = new List<ArrowCell>();
        
        // Top-left: Down arrow (special)
        ArrowCell arrow1 = CreateArrowCellForTutorial(0, 0, ArrowDirection.Down, true);
        if (arrow1 != null)
        {
            createdArrows.Add(arrow1);
        }
        
        // Bottom-left: Right arrow (gray)
        ArrowCell arrow2 = CreateArrowCellForTutorial(0, 1, ArrowDirection.Right, false);
        if (arrow2 != null)
        {
            createdArrows.Add(arrow2);
        }
        
        // Bottom-right: Up arrow (gray)
        ArrowCell arrow3 = CreateArrowCellForTutorial(1, 1, ArrowDirection.Up, false);
        if (arrow3 != null)
        {
            createdArrows.Add(arrow3);
        }
        
        // Animate all arrows with stagger effect
        AnimateBoardSpawn(createdArrows, specialX, specialY);
    }
    
    public void InitializeLevel4Board(bool resetLives = true)
    {
        // Level 4 board: 3x3 grid as shown in reference image
        boardWidth = 3;
        boardHeight = 3;
        
        // Reset lives when starting level
        if (resetLives && livesManager != null)
        {
            livesManager.ResetLives();
        }
        
        // Clear tracked arrows that lost life
        arrowsThatLostLife.Clear();
        
        // Clear existing board
        ClearBoard();
        
        // Create new board
        board = new ArrowCell[boardHeight, boardWidth];
        
        // Hardcode arrow positions (x,y):
        // Row 0: (0,0) Right | (1,0) Up    | (2,0) Right
        // Row 1: (0,1) Right | (1,1) Up    | (2,1) Down (special, yellow)
        // Row 2: (0,2) Up    | (1,2) Left  | (2,2) Left
        int specialX = 2;
        int specialY = 1;
        
        List<ArrowCell> createdArrows = new List<ArrowCell>();
        
        // Row 0
        ArrowCell a00 = CreateArrowCellForTutorial(0, 0, ArrowDirection.Right, false);
        if (a00 != null) createdArrows.Add(a00);
        ArrowCell a10 = CreateArrowCellForTutorial(1, 0, ArrowDirection.Up, false);
        if (a10 != null) createdArrows.Add(a10);
        ArrowCell a20 = CreateArrowCellForTutorial(2, 0, ArrowDirection.Right, false);
        if (a20 != null) createdArrows.Add(a20);
        
        // Row 1
        ArrowCell a01 = CreateArrowCellForTutorial(0, 1, ArrowDirection.Right, false);
        if (a01 != null) createdArrows.Add(a01);
        ArrowCell a11 = CreateArrowCellForTutorial(1, 1, ArrowDirection.Up, false);
        if (a11 != null) createdArrows.Add(a11);
        ArrowCell a21 = CreateArrowCellForTutorial(2, 1, ArrowDirection.Down, true); // special
        if (a21 != null) createdArrows.Add(a21);
        
        // Row 2
        ArrowCell a02 = CreateArrowCellForTutorial(0, 2, ArrowDirection.Up, false);
        if (a02 != null) createdArrows.Add(a02);
        ArrowCell a12 = CreateArrowCellForTutorial(1, 2, ArrowDirection.Left, false);
        if (a12 != null) createdArrows.Add(a12);
        ArrowCell a22 = CreateArrowCellForTutorial(2, 2, ArrowDirection.Left, false);
        if (a22 != null) createdArrows.Add(a22);
        
        // Animate all arrows with stagger effect
        AnimateBoardSpawn(createdArrows, specialX, specialY);
    }
    
    public void InitializeTutorialBoard()
    {
        // Hardcode tutorial board: 3x3 grid
        boardWidth = 3;
        boardHeight = 3;
        
        // Reset lives when starting tutorial
        if (livesManager != null)
        {
            livesManager.ResetLives();
        }
        
        // Clear tracked arrows that lost life
        arrowsThatLostLife.Clear();
        
        // Clear existing board
        ClearBoard();
        
        // Create new board
        board = new ArrowCell[boardHeight, boardWidth];
        
        // Hardcode arrow directions based on image:
        // Row 0 (top): Up, Left, Up
        // Row 1 (middle): Down, Up (special), Right
        // Row 2 (bottom): Down, Left, Down
        ArrowDirection[,] tutorialDirections = new ArrowDirection[3, 3]
        {
            { ArrowDirection.Up, ArrowDirection.Left, ArrowDirection.Up },
            { ArrowDirection.Down, ArrowDirection.Up, ArrowDirection.Right },
            { ArrowDirection.Down, ArrowDirection.Left, ArrowDirection.Down }
        };
        
        int centerX = 1; // Center of 3x3 grid
        int centerY = 1;
        
        // Create board cells
        List<ArrowCell> createdArrows = new List<ArrowCell>();
        
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                bool isSpecial = (x == centerX && y == centerY);
                ArrowDirection dir = tutorialDirections[y, x];
                
                ArrowCell arrow = CreateArrowCellForTutorial(x, y, dir, isSpecial);
                if (arrow != null)
                {
                    createdArrows.Add(arrow);
                }
            }
        }
        
        // Animate all arrows with stagger effect
        AnimateBoardSpawn(createdArrows, centerX, centerY);
    }
    
    ArrowCell CreateArrowCellForTutorial(int x, int y, ArrowDirection dir, bool isSpecial)
    {
        GameObject cellObj;
        
        if (arrowCellPrefab != null)
        {
            cellObj = Instantiate(arrowCellPrefab, boardContainer);
        }
        else
        {
            // Create default cell if no prefab
            cellObj = new GameObject($"ArrowCell_{x}_{y}");
            cellObj.transform.SetParent(boardContainer);
            
            // Add components
            Image img = cellObj.AddComponent<Image>();
            img.color = Color.white;
            
            Button btn = cellObj.AddComponent<Button>();
            
            // Add ArrowVisualHelper for automatic arrow shape generation
            ArrowVisualHelper visualHelper = cellObj.AddComponent<ArrowVisualHelper>();
            
            ArrowCell arrowCell = cellObj.AddComponent<ArrowCell>();
            arrowCell.arrowImage = img;
        }
        
        ArrowCell arrow = cellObj.GetComponent<ArrowCell>();
        
        if (isSpecial)
        {
            specialArrow = arrow;
        }
        
        arrow.Initialize(dir, new Vector2Int(x, y), this, isSpecial);
        
        // Position the cell
        float totalCellSize = cellSize + cellSpacing;
        float startX = -(boardWidth - 1) * totalCellSize / 2f;
        float startY = (boardHeight - 1) * totalCellSize / 2f;
        
        Vector3 position = new Vector3(
            startX + x * totalCellSize,
            startY - y * totalCellSize,
            0f
        );
        
        cellObj.transform.localPosition = position;
        cellObj.transform.localScale = Vector3.zero; // Start scaled down for animation
        
        // IMPORTANT: Assign arrow to board array so GetArrowByIndex() can find it
        board[y, x] = arrow;
        
        return arrow;
    }
    
    public void ReloadBoard()
    {
        // Reset lives when reloading
        if (livesManager != null)
        {
            livesManager.ResetLives();
        }
        
        // Clear tracked arrows that lost life
        arrowsThatLostLife.Clear();
        
        if (currentDifficultySettings != null)
        {
            Debug.Log("Reloading board...");
            InitializeBoard(currentDifficultySettings);
        }
        else
        {
            // Fallback: reload with current board size
            InitializeBoard(boardWidth, boardHeight);
        }
    }
    
    public void ShuffleBoard()
    {
        if (board == null) return;
        
        Debug.Log("Shuffling board...");
        
        // Shuffle with validation - retry if no arrows can move
        const int maxShuffleRetries = 30;
        int retryCount = 0;
        bool shuffleValid = false;
        List<ArrowCell> arrowsToShuffle = null;
        
        while (!shuffleValid && retryCount < maxShuffleRetries)
        {
            // Collect all arrows to shuffle
            arrowsToShuffle = new List<ArrowCell>();
            
            // Shuffle directions of all arrows except special arrow
            // Use multiple passes to ensure no opposite directions
            for (int pass = 0; pass < 2; pass++)
            {
                for (int y = 0; y < boardHeight; y++)
                {
                    for (int x = 0; x < boardWidth; x++)
                    {
                        if (board[y, x] != null && !board[y, x].isSpecialArrow)
                        {
                            // Get random new direction avoiding opposite of adjacent arrows
                            ArrowDirection newDirection = GetRandomDirectionAvoidingOpposite(x, y);
                            board[y, x].direction = newDirection;
                            board[y, x].UpdateVisual();
                            
                            // Collect arrow for animation (only in first pass)
                            if (pass == 0 && !arrowsToShuffle.Contains(board[y, x]))
                            {
                                arrowsToShuffle.Add(board[y, x]);
                            }
                        }
                    }
                }
            }
            
            // Validate that at least one arrow can move
            shuffleValid = HasAnyMovableArrow();
            
            if (!shuffleValid)
            {
                retryCount++;
                Debug.LogWarning($"Shuffled board with no movable arrows (retry {retryCount}/{maxShuffleRetries}). Reshuffling...");
                
                // Restore original directions before retry (optional, but helps with variety)
                // Actually, let's just continue shuffling for more variety
            }
        }
        
        if (!shuffleValid)
        {
            Debug.LogWarning($"Shuffle resulted in no movable arrows after {maxShuffleRetries} attempts. Board may have blocking issues.");
        }
        
        // Animate shuffle with faster animation
        if (arrowsToShuffle != null && arrowsToShuffle.Count > 0)
        {
            AnimateBoardShuffle(arrowsToShuffle);
        }
        
        // Clear any existing hints
        ClearHint();
    }
    
    void AnimateBoardShuffle(List<ArrowCell> arrows)
    {
        if (arrows == null || arrows.Count == 0) return;
        
        // Shuffle the list for random animation order
        for (int i = 0; i < arrows.Count; i++)
        {
            ArrowCell temp = arrows[i];
            int randomIndex = Random.Range(i, arrows.Count);
            arrows[i] = arrows[randomIndex];
            arrows[randomIndex] = temp;
        }
        
        // Animate each arrow with stagger delay (faster than spawn)
        for (int i = 0; i < arrows.Count; i++)
        {
            ArrowCell arrow = arrows[i];
            float delay = i * shuffleStaggerDelay;
            
            AnimateArrowCellShuffle(arrow, delay);
        }
    }
    
    void AnimateArrowCellShuffle(ArrowCell arrow, float delay)
    {
        if (arrow == null || arrow.gameObject == null) return;
        
        // Kill any existing tweens
        arrow.transform.DOKill();
        
        Vector3 originalScale = arrow.transform.localScale;
        Vector3 originalRotation = arrow.transform.localEulerAngles;
        
        // Create shuffle animation sequence (faster and simpler than spawn)
        Sequence shuffleSequence = DOTween.Sequence();
        
        // 1. Quick scale down
        shuffleSequence.Append(arrow.transform.DOScale(originalScale * 0.8f, shuffleAnimationDuration * 0.2f)
            .SetEase(Ease.InQuad)
            .SetDelay(delay));
        
        // 2. Rotate quickly (visual feedback for direction change)
        shuffleSequence.Join(arrow.transform.DORotate(originalRotation + new Vector3(0, 0, 180f), shuffleAnimationDuration * 0.2f, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad)
            .SetDelay(delay));
        
        // 3. Scale bounce back
        shuffleSequence.Append(arrow.transform.DOScale(originalScale * 1.1f, shuffleAnimationDuration * 0.2f)
            .SetEase(Ease.OutBack));
        
        // 4. Rotate back
        shuffleSequence.Join(arrow.transform.DORotate(originalRotation, shuffleAnimationDuration * 0.2f)
            .SetEase(Ease.OutQuad));
        
        // 5. Scale back to normal
        shuffleSequence.Append(arrow.transform.DOScale(originalScale, shuffleAnimationDuration * 0.2f)
            .SetEase(Ease.InQuad));
    }
    
    Dictionary<Vector2Int, ArrowDirection> GenerateComplexPathsWithDirections(int centerX, int centerY, int pathCount, float complexity)
    {
        Dictionary<Vector2Int, ArrowDirection> pathDirections = new Dictionary<Vector2Int, ArrowDirection>();
        
        // Generate multiple complex paths to different edges
        for (int i = 0; i < pathCount; i++)
        {
            int edgeChoice = Random.Range(0, 4);
            int exitX = 0, exitY = 0;
            
            switch (edgeChoice)
            {
                case 0: // Top edge
                    exitX = Random.Range(0, boardWidth);
                    exitY = 0;
                    break;
                case 1: // Bottom edge
                    exitX = Random.Range(0, boardWidth);
                    exitY = boardHeight - 1;
                    break;
                case 2: // Left edge
                    exitX = 0;
                    exitY = Random.Range(0, boardHeight);
                    break;
                case 3: // Right edge
                    exitX = boardWidth - 1;
                    exitY = Random.Range(0, boardHeight);
                    break;
            }
            
            // Create complex winding path from center to exit with directions
            Dictionary<Vector2Int, ArrowDirection> path = GenerateWindingPathWithDirections(
                new Vector2Int(centerX, centerY), 
                new Vector2Int(exitX, exitY), 
                complexity
            );
            
            // Merge paths (if multiple paths overlap, keep the first one's direction)
            foreach (var kvp in path)
            {
                if (!pathDirections.ContainsKey(kvp.Key))
                {
                    pathDirections[kvp.Key] = kvp.Value;
                }
            }
        }
        
        return pathDirections;
    }
    
    Dictionary<Vector2Int, ArrowDirection> GenerateWindingPathWithDirections(Vector2Int start, Vector2Int end, float complexity)
    {
        Dictionary<Vector2Int, ArrowDirection> pathDirections = new Dictionary<Vector2Int, ArrowDirection>();
        List<Vector2Int> path = new List<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        
        Vector2Int current = start;
        path.Add(current);
        visited.Add(current);
        
        int maxSteps = (boardWidth + boardHeight) * 2; // Prevent infinite loops
        int stepCount = 0;
        
        while (current != end && stepCount < maxSteps)
        {
            stepCount++;
            
            // Calculate direction to target
            int dx = end.x - current.x;
            int dy = end.y - current.y;
            
            // Determine preferred direction (towards target)
            Vector2Int preferredDirection = Vector2Int.zero;
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                preferredDirection = dx > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                preferredDirection = dy > 0 ? Vector2Int.down : Vector2Int.up;
            }
            
            // List of possible next positions
            List<Vector2Int> candidates = new List<Vector2Int>();
            
            // Add preferred direction
            Vector2Int preferredNext = current + preferredDirection;
            if (IsValidPosition(preferredNext) && !visited.Contains(preferredNext))
            {
                candidates.Add(preferredNext);
            }
            
            // Add perpendicular directions based on complexity
            Vector2Int perp1 = Vector2Int.zero;
            Vector2Int perp2 = Vector2Int.zero;
            
            if (preferredDirection == Vector2Int.right || preferredDirection == Vector2Int.left)
            {
                perp1 = Vector2Int.up;
                perp2 = Vector2Int.down;
            }
            else
            {
                perp1 = Vector2Int.left;
                perp2 = Vector2Int.right;
            }
            
            Vector2Int perp1Next = current + perp1;
            Vector2Int perp2Next = current + perp2;
            
            if (IsValidPosition(perp1Next) && !visited.Contains(perp1Next))
            {
                candidates.Add(perp1Next);
            }
            if (IsValidPosition(perp2Next) && !visited.Contains(perp2Next))
            {
                candidates.Add(perp2Next);
            }
            
            // Add opposite direction with low probability (for complexity)
            if (Random.Range(0f, 1f) < complexity * 0.3f)
            {
                Vector2Int oppositeNext = current - preferredDirection;
                if (IsValidPosition(oppositeNext) && !visited.Contains(oppositeNext))
                {
                    candidates.Add(oppositeNext);
                }
            }
            
            // If no candidates, try to backtrack or find any valid move
            if (candidates.Count == 0)
            {
                // Try all 4 directions
                Vector2Int[] allDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (Vector2Int dir in allDirections)
                {
                    Vector2Int next = current + dir;
                    if (IsValidPosition(next) && !visited.Contains(next))
                    {
                        candidates.Add(next);
                    }
                }
            }
            
            // Choose next position based on complexity
            Vector2Int nextPos;
            if (candidates.Count == 0)
            {
                // Fallback: move towards target even if visited
                nextPos = current + preferredDirection;
                if (!IsValidPosition(nextPos))
                {
                    // Last resort: any valid direction
                    foreach (Vector2Int dir in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    {
                        Vector2Int test = current + dir;
                        if (IsValidPosition(test))
                        {
                            nextPos = test;
                            break;
                        }
                    }
                }
            }
            else
            {
                // Choose based on complexity: higher complexity = more random
                if (Random.Range(0f, 1f) < complexity && candidates.Count > 1)
                {
                    // Random choice for complexity
                    nextPos = candidates[Random.Range(0, candidates.Count)];
                }
                else
                {
                    // Prefer direction towards target
                    nextPos = candidates[0];
                }
            }
            
            // Calculate direction from current to nextPos
            Vector2Int moveDirection = nextPos - current;
            ArrowDirection arrowDir = Vector2IntToArrowDirection(moveDirection);
            
            // Set direction for current position
            pathDirections[current] = arrowDir;
            
            current = nextPos;
            path.Add(current);
            visited.Add(current);
        }
        
        // Ensure we reach the end
        if (current != end)
        {
            // Complete the path directly if needed
            Vector2Int prev = path.Count > 1 ? path[path.Count - 2] : current;
            while (current != end)
            {
                int dx = end.x - current.x;
                int dy = end.y - current.y;
                
                Vector2Int nextStep = current;
                if (Mathf.Abs(dx) > Mathf.Abs(dy))
                {
                    nextStep.x += dx > 0 ? 1 : -1;
                }
                else
                {
                    nextStep.y += dy > 0 ? 1 : -1;
                }
                
                Vector2Int dirToNext = nextStep - current;
                ArrowDirection dir = Vector2IntToArrowDirection(dirToNext);
                pathDirections[current] = dir;
                
                current = nextStep;
                if (!path.Contains(current))
                {
                    path.Add(current);
                }
            }
        }
        
        // Set direction for the last cell (pointing out)
        if (path.Count > 1)
        {
            Vector2Int lastCell = path[path.Count - 1];
            if (!pathDirections.ContainsKey(lastCell))
            {
                Vector2Int secondLast = path[path.Count - 2];
                Vector2Int exitDirection = lastCell - secondLast;
                pathDirections[lastCell] = Vector2IntToArrowDirection(exitDirection);
            }
        }
        
        return pathDirections;
    }
    
    ArrowDirection Vector2IntToArrowDirection(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return ArrowDirection.Down; // Up in grid means y decreases
        if (dir == Vector2Int.down) return ArrowDirection.Up; // Down in grid means y increases
        if (dir == Vector2Int.left) return ArrowDirection.Left;
        if (dir == Vector2Int.right) return ArrowDirection.Right;
        return ArrowDirection.Up; // Default
    }
    
    ArrowCell CreateArrowCell(int x, int y, int centerX, int centerY, DifficultySettings settings, bool isOnPath, ArrowDirection pathDirection = ArrowDirection.Up)
    {
        GameObject cellObj;
        
        if (arrowCellPrefab != null)
        {
            cellObj = Instantiate(arrowCellPrefab, boardContainer);
        }
        else
        {
            // Create default cell if no prefab
            cellObj = new GameObject($"ArrowCell_{x}_{y}");
            cellObj.transform.SetParent(boardContainer);
            
            // Add components
            Image img = cellObj.AddComponent<Image>();
            img.color = Color.white;
            
            Button btn = cellObj.AddComponent<Button>();
            
            // Add ArrowVisualHelper for automatic arrow shape generation
            ArrowVisualHelper visualHelper = cellObj.AddComponent<ArrowVisualHelper>();
            
            ArrowCell arrowCell = cellObj.AddComponent<ArrowCell>();
            arrowCell.arrowImage = img;
        }
        
        ArrowCell arrow = cellObj.GetComponent<ArrowCell>();
        
        // Determine if this is the special arrow (center)
        bool isSpecial = (x == centerX && y == centerY);
        
        // Determine arrow direction based on difficulty settings
        ArrowDirection dir;
        if (isSpecial)
        {
            // Special arrow: if on path, point towards exit, otherwise random
            if (isOnPath)
            {
                // Point towards nearest edge
                int distToTop = y;
                int distToBottom = boardHeight - 1 - y;
                int distToLeft = x;
                int distToRight = boardWidth - 1 - x;
                
                int minDist = Mathf.Min(distToTop, distToBottom, distToLeft, distToRight);
                if (minDist == distToTop) dir = ArrowDirection.Up;
                else if (minDist == distToBottom) dir = ArrowDirection.Down;
                else if (minDist == distToLeft) dir = ArrowDirection.Left;
                else dir = ArrowDirection.Right;
            }
            else
            {
                dir = GetRandomDirection();
            }
            specialArrow = arrow;
        }
        else if (isOnPath)
        {
            // On guaranteed path: use the path direction
            dir = pathDirection;
        }
        else
        {
            // Not on path: use difficulty-based random
            float rand = Random.Range(0f, 1f);
            
            if (rand < settings.arrowsPointingToCenter)
            {
                // Point towards center (makes it harder)
                if (x < centerX) dir = ArrowDirection.Right;
                else if (x > centerX) dir = ArrowDirection.Left;
                else if (y < centerY) dir = ArrowDirection.Down;
                else dir = ArrowDirection.Up;
            }
            else if (rand < settings.arrowsPointingToCenter + settings.obstacleDensity)
            {
                // Random obstacle direction (avoid opposite of adjacent arrows)
                dir = GetRandomDirectionAvoidingOpposite(x, y);
            }
            else
            {
                // Normal random (avoid opposite of adjacent arrows)
                dir = GetRandomDirectionAvoidingOpposite(x, y);
            }
        }
        
        arrow.Initialize(dir, new Vector2Int(x, y), this, isSpecial);
        
        // Position the cell
        float totalCellSize = cellSize + cellSpacing;
        float startX = -(boardWidth - 1) * totalCellSize / 2f;
        float startY = (boardHeight - 1) * totalCellSize / 2f;
        
        Vector3 position = new Vector3(
            startX + x * totalCellSize,
            startY - y * totalCellSize,
            0f
        );
        
        cellObj.transform.localPosition = position;
        cellObj.transform.localScale = Vector3.one * cellSize;
        
        board[y, x] = arrow;
        
        return arrow;
    }
    
    void AnimateBoardSpawn(List<ArrowCell> arrows, int centerX, int centerY)
    {
        if (arrows == null || arrows.Count == 0) return;
        
        // Sort arrows by distance from center (if animateFromCenter) or by position
        if (animateFromCenter)
        {
            arrows.Sort((a, b) =>
            {
                Vector2Int posA = a.GetGridPosition();
                Vector2Int posB = b.GetGridPosition();
                
                int distA = Mathf.Abs(posA.x - centerX) + Mathf.Abs(posA.y - centerY);
                int distB = Mathf.Abs(posB.x - centerX) + Mathf.Abs(posB.y - centerY);
                
                if (distA != distB)
                    return distA.CompareTo(distB);
                
                // If same distance, sort by y then x
                if (posA.y != posB.y)
                    return posA.y.CompareTo(posB.y);
                return posA.x.CompareTo(posB.x);
            });
        }
        else
        {
            // Sort by y then x (top to bottom, left to right)
            arrows.Sort((a, b) =>
            {
                Vector2Int posA = a.GetGridPosition();
                Vector2Int posB = b.GetGridPosition();
                
                if (posA.y != posB.y)
                    return posA.y.CompareTo(posB.y);
                return posA.x.CompareTo(posB.x);
            });
        }
        
        // Animate each arrow with stagger delay
        for (int i = 0; i < arrows.Count; i++)
        {
            ArrowCell arrow = arrows[i];
            float delay = i * spawnStaggerDelay;
            
            AnimateArrowCellSpawn(arrow, delay, arrow.isSpecialArrow);
        }
    }
    
    void AnimateArrowCellSpawn(ArrowCell arrow, float delay, bool isSpecial)
    {
        if (arrow == null || arrow.gameObject == null) return;
        
        // Kill any existing tweens
        arrow.transform.DOKill();
        
        // Get UI components for fade
        UnityEngine.UI.Image backgroundImage = arrow.GetComponent<UnityEngine.UI.Image>();
        UnityEngine.UI.Image arrowImage = arrow.arrowImage;
        
        // Set initial state (invisible and scaled down)
        arrow.transform.localScale = Vector3.zero;
        
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = 0f;
            backgroundImage.color = bgColor;
        }
        
        if (arrowImage != null)
        {
            Color arrowColor = arrowImage.color;
            arrowColor.a = 0f;
            arrowImage.color = arrowColor;
        }
        
        // Create spawn animation sequence
        Sequence spawnSequence = DOTween.Sequence();
        
        // 1. Scale bounce up (more dramatic for special arrow)
        float scaleTarget = isSpecial ? 1.2f : 1.1f;
        spawnSequence.Append(arrow.transform.DOScale(Vector3.one * scaleTarget, spawnAnimationDuration * 0.3f)
            .SetEase(Ease.OutBack)
            .SetDelay(delay));
        
        // 2. Fade in
        if (backgroundImage != null)
        {
            spawnSequence.Join(backgroundImage.DOFade(1f, spawnAnimationDuration * 0.3f)
                .SetDelay(delay));
        }
        
        if (arrowImage != null)
        {
            spawnSequence.Join(arrowImage.DOFade(1f, spawnAnimationDuration * 0.3f)
                .SetDelay(delay));
        }
        
        // 3. Rotate slightly for visual interest
        float rotationAmount = isSpecial ? 15f : 5f;
        spawnSequence.Join(arrow.transform.DORotate(new Vector3(0, 0, Random.Range(-rotationAmount, rotationAmount)), spawnAnimationDuration * 0.2f)
            .SetEase(Ease.OutQuad)
            .SetDelay(delay));
        
        // 4. Scale back to normal
        spawnSequence.Append(arrow.transform.DOScale(Vector3.one, spawnAnimationDuration * 0.2f)
            .SetEase(Ease.InQuad));
        
        // 5. Rotate back to 0
        spawnSequence.Join(arrow.transform.DORotate(Vector3.zero, spawnAnimationDuration * 0.2f)
            .SetEase(Ease.InQuad));
        
        // Special arrow gets extra bounce
        if (isSpecial)
        {
            spawnSequence.Append(arrow.transform.DOScale(Vector3.one * 1.05f, spawnAnimationDuration * 0.1f)
                .SetEase(Ease.OutQuad));
            spawnSequence.Append(arrow.transform.DOScale(Vector3.one, spawnAnimationDuration * 0.1f)
                .SetEase(Ease.InQuad));
        }
    }
    
    ArrowDirection GetRandomDirection()
    {
        return (ArrowDirection)Random.Range(0, 4);
    }
    
    ArrowDirection GetOppositeDirection(ArrowDirection dir)
    {
        switch (dir)
        {
            case ArrowDirection.Up:
                return ArrowDirection.Down;
            case ArrowDirection.Down:
                return ArrowDirection.Up;
            case ArrowDirection.Left:
                return ArrowDirection.Right;
            case ArrowDirection.Right:
                return ArrowDirection.Left;
            default:
                return dir;
        }
    }
    
    List<ArrowDirection> GetAdjacentArrowDirections(int x, int y)
    {
        List<ArrowDirection> adjacentDirections = new List<ArrowDirection>();
        
        // Check top neighbor
        if (y > 0 && board[y - 1, x] != null)
        {
            adjacentDirections.Add(board[y - 1, x].direction);
        }
        
        // Check bottom neighbor
        if (y < boardHeight - 1 && board[y + 1, x] != null)
        {
            adjacentDirections.Add(board[y + 1, x].direction);
        }
        
        // Check left neighbor
        if (x > 0 && board[y, x - 1] != null)
        {
            adjacentDirections.Add(board[y, x - 1].direction);
        }
        
        // Check right neighbor
        if (x < boardWidth - 1 && board[y, x + 1] != null)
        {
            adjacentDirections.Add(board[y, x + 1].direction);
        }
        
        return adjacentDirections;
    }
    
    ArrowDirection GetRandomDirectionAvoidingOpposite(int x, int y)
    {
        List<ArrowDirection> adjacentDirections = GetAdjacentArrowDirections(x, y);
        List<ArrowDirection> forbiddenDirections = new List<ArrowDirection>();
        
        // Collect all opposite directions of adjacent arrows
        foreach (ArrowDirection adjDir in adjacentDirections)
        {
            ArrowDirection opposite = GetOppositeDirection(adjDir);
            if (!forbiddenDirections.Contains(opposite))
            {
                forbiddenDirections.Add(opposite);
            }
        }
        
        // Get all possible directions
        List<ArrowDirection> possibleDirections = new List<ArrowDirection>
        {
            ArrowDirection.Up,
            ArrowDirection.Down,
            ArrowDirection.Left,
            ArrowDirection.Right
        };
        
        // Remove forbidden directions
        foreach (ArrowDirection forbidden in forbiddenDirections)
        {
            possibleDirections.Remove(forbidden);
        }
        
        // If no possible directions left, return random (shouldn't happen but safety check)
        if (possibleDirections.Count == 0)
        {
            return GetRandomDirection();
        }
        
        // Return random from possible directions
        return possibleDirections[Random.Range(0, possibleDirections.Count)];
    }
    
    public void OnArrowClicked(Vector2Int gridPos)
    {
        if (isMoving) return;
        
        ArrowCell clickedArrow = board[gridPos.y, gridPos.x];
        if (clickedArrow == null) return;
        
        // Notify tutorial system about arrow click (for tutorial step tracking)
        UIInGame uiInGame = FindFirstObjectByType<UIInGame>();
        if (uiInGame != null)
        {
            uiInGame.OnTutorialArrowClicked(clickedArrow);
        }
        
        // Stop any existing movement
        if (currentMoveCoroutine != null)
        {
            StopCoroutine(currentMoveCoroutine);
        }
        
        // Start continuous movement
        currentMoveCoroutine = StartCoroutine(MoveArrowContinuously(clickedArrow));
    }
    
    IEnumerator MoveArrowContinuously(ArrowCell arrow)
    {
        isMoving = true;
        
        Vector2Int direction = arrow.GetDirectionVector();
        float totalCellSize = cellSize + cellSpacing;
        float startX = -(boardWidth - 1) * totalCellSize / 2f;
        float startY = (boardHeight - 1) * totalCellSize / 2f;
        
        // Kill any existing tweens on this arrow
        arrow.transform.DOKill();
        
        // Calculate the entire path first
        List<Vector3> pathPositions = new List<Vector3>();
        List<Vector2Int> pathGridPositions = new List<Vector2Int>();
        Vector2Int currentPos = arrow.GetGridPosition();
        
        // Build the complete path
        while (true)
        {
            Vector2Int nextPos = currentPos + direction;
            
            // Check if next position is valid
            if (IsValidPosition(nextPos))
            {
                // Check if there's an arrow blocking the path
                if (board[nextPos.y, nextPos.x] != null)
                {
                    // Blocked - stop path building
                    break;
                }
                
                // Add position to path
                Vector3 targetPos = new Vector3(
                    startX + nextPos.x * totalCellSize,
                    startY - nextPos.y * totalCellSize,
                    0f
                );
                pathPositions.Add(targetPos);
                pathGridPositions.Add(nextPos);
                
                // Update board array immediately (before animation)
                if (IsValidPosition(currentPos))
                {
                    board[currentPos.y, currentPos.x] = null;
                }
                board[nextPos.y, nextPos.x] = arrow;
                
                // Update grid position
                arrow.Initialize(arrow.direction, nextPos, this, arrow.isSpecialArrow);
                
                currentPos = nextPos;
            }
            else
            {
                // Arrow will exit the board
                Vector3 exitPos = new Vector3(
                    startX + nextPos.x * totalCellSize,
                    startY - nextPos.y * totalCellSize,
                    0f
                );
                pathPositions.Add(exitPos);
                pathGridPositions.Add(nextPos);
                
                // Remove from board
                if (IsValidPosition(currentPos))
                {
                    board[currentPos.y, currentPos.x] = null;
                }
                
                break;
            }
        }
        
        // If no path, play collision animation
        if (pathPositions.Count == 0)
        {
            Debug.Log("Arrow blocked by another arrow!");
            
            // Lose a life when arrow is blocked (only once per arrow, and only if level >= 4)
            if (livesManager != null && !arrowsThatLostLife.Contains(arrow))
            {
                arrowsThatLostLife.Add(arrow);
                
                // Only lose life if level >= 4
                if (levelManager != null && levelManager.GetCurrentLevel() >= 4)
                {
                    livesManager.LoseLife();
                }
            }
            
            // Calculate blocked position
            Vector2Int blockedPos = currentPos + direction;
            
            // Fade red color for blocking arrow (adjacent arrow in front)
            if (IsValidPosition(blockedPos) && board[blockedPos.y, blockedPos.x] != null)
            {
                ArrowCell blockingArrow = board[blockedPos.y, blockedPos.x];
                blockingArrow.FadeColor(blockingArrow.errorColor, 0.2f);
            }
            
            // Play collision animation
            yield return StartCoroutine(PlayCollisionAnimation(arrow, currentPos, blockedPos, direction));
            
            isMoving = false;
            currentMoveCoroutine = null;
            
            // Check if there are any movable arrows left after collision
            CheckForMovableArrows();
            
            yield break;
        }
        
        // Has path - fade green color for clicked arrow
        arrow.FadeColor(arrow.successColor, 0.2f);
        
        // Arrow can move successfully, remove from lost life tracking
        arrowsThatLostLife.Remove(arrow);
        
        // Create continuous linear movement without gaps
        Sequence moveSequence = DOTween.Sequence();
        
        // Chain all movements together seamlessly
        for (int i = 0; i < pathPositions.Count; i++)
        {
            moveSequence.Append(arrow.transform.DOLocalMove(pathPositions[i], moveSpeed).SetEase(Ease.Linear));
        }
        
        // Wait for all movements to complete
        yield return moveSequence.WaitForCompletion();
        
        // Check if arrow exited the board
        Vector2Int finalPos = pathGridPositions[pathGridPositions.Count - 1];
        if (!IsValidPosition(finalPos))
        {
            // Arrow exited - continue moving off screen before disappearing
            Vector3 currentExitPos = pathPositions[pathPositions.Count - 1];
            
        // Calculate direction vector for exit movement
        // Note: In grid, Vector2Int.up means y increases which moves down in world space
        //       In grid, Vector2Int.down means y decreases which moves up in world space
            Vector3 exitDirection = Vector3.zero;
            bool isVertical = false;
            if (direction == Vector2Int.up)
            {
                // Grid y increases means world space moves down
                exitDirection = Vector3.down;
                isVertical = true;
            }
            else if (direction == Vector2Int.down)
            {
                // Grid y decreases means world space moves up
                exitDirection = Vector3.up;
                isVertical = true;
            }
            else if (direction == Vector2Int.left)
            {
                exitDirection = Vector3.left;
            }
            else if (direction == Vector2Int.right)
            {
                exitDirection = Vector3.right;
            }
            
            // Move further off screen (vertical arrows move further)
            float offScreenDistance = isVertical ? totalCellSize * 4f : totalCellSize * 2f;
            Vector3 offScreenPos = currentExitPos + exitDirection * offScreenDistance;
            
            // Calculate duration based on distance to maintain consistent speed
            // Speed = distance / time, so time = distance / speed
            // Use moveSpeed per cell, so duration = (distance / cellSize) * moveSpeed
            float exitDuration = (offScreenDistance / totalCellSize) * moveSpeed;
            
            // Move arrow off screen
            Sequence exitSequence = DOTween.Sequence();
            exitSequence.Append(arrow.transform.DOLocalMove(offScreenPos, exitDuration).SetEase(Ease.Linear));
            
            // Fade out while moving
            UnityEngine.UI.Image backgroundImage = arrow.GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Image arrowImage = arrow.arrowImage;
            
            if (backgroundImage != null)
            {
                exitSequence.Join(backgroundImage.DOFade(0f, exitDuration));
            }
            
            if (arrowImage != null)
            {
                exitSequence.Join(arrowImage.DOFade(0f, exitDuration));
            }
            
            // Scale down while moving
            exitSequence.Join(arrow.transform.DOScale(Vector3.zero, exitDuration).SetEase(Ease.InBack));
            
            // Wait for exit animation
            yield return exitSequence.WaitForCompletion();
            
            // Destroy arrow after moving off screen
            if (arrow.isSpecialArrow)
            {
                // Special arrow exited - WIN!
                OnSpecialArrowExited();
            }
            
            if (arrow != null && arrow.gameObject != null)
            {
                Destroy(arrow.gameObject);
            }
        }
        
        isMoving = false;
        currentMoveCoroutine = null;
        
        // Check if there are any movable arrows left after movement
        CheckForMovableArrows();
    }
    
    void CheckForMovableArrows()
    {
        // Don't check if already won (special arrow has exited)
        if (specialArrow == null)
        {
            return; // Level already completed, don't show message
        }
        
        // Check if there are any arrows that can still move
        bool hasMovableArrow = HasAnyMovableArrow();
        
        if (!hasMovableArrow)
        {
            Debug.Log("No movable arrows found after movement!");
            
            // Show message in UI and auto shuffle after delay
            UIInGame uiInGame = FindFirstObjectByType<UIInGame>();
            if (uiInGame != null)
            {
                uiInGame.ShowNoMovableArrowMessage();
            }
        }
    }
    
    bool HasAnyMovableArrow()
    {
        if (board == null) return false;
        
        // Search through all arrows
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (board[y, x] == null) continue;
                
                ArrowCell arrow = board[y, x];
                
                // Skip the special arrow itself
                //if (arrow.isSpecialArrow) continue;
                
                // Check if this arrow can move
                if (CanArrowMove(arrow))
                {
                    return true; // Found at least one movable arrow
                }
            }
        }
        
        return false; // No movable arrows found
    }
    
    IEnumerator PlayCollisionAnimation(ArrowCell arrow, Vector2Int currentPosition, Vector2Int blockedPosition, Vector2Int moveDirection)
    {
        // Kill any existing tweens
        arrow.transform.DOKill();
        
        Vector3 originalPosition = arrow.transform.localPosition;
        Vector3 originalScale = Vector3.one; // Use default scale
        
        // Calculate blocked position in world space
        float totalCellSize = cellSize + cellSpacing;
        float startX = -(boardWidth - 1) * totalCellSize / 2f;
        float startY = (boardHeight - 1) * totalCellSize / 2f;
        Vector3 blockedWorldPos = new Vector3(
            startX + blockedPosition.x * totalCellSize,
            startY - blockedPosition.y * totalCellSize,
            0f
        );
        
        // Calculate direction to blocked position
        Vector3 directionToBlocked = (blockedWorldPos - originalPosition).normalized;
        if (directionToBlocked.magnitude < 0.1f)
        {
            // Fallback direction if too close
            directionToBlocked = Vector3.right;
        }
        
        // Find all arrows in front of clicked arrow
        List<ArrowCell> affectedArrows = FindAffectedHorizontalArrows(currentPosition, blockedPosition, moveDirection);
        
        // Create collision sequence
        Sequence collisionSequence = DOTween.Sequence();
        
        // 1. Move forward towards blocked position (impact) - more visible
        Vector3 impactPosition = originalPosition + directionToBlocked * collisionImpactDistance;
        collisionSequence.Append(arrow.transform.DOLocalMove(impactPosition, collisionShakeDuration * 0.15f).SetEase(Ease.OutCubic));
        
        // 2. Scale bounce effect - more dramatic
        collisionSequence.Join(arrow.transform.DOScale(originalScale * collisionScaleBounce, collisionShakeDuration * 0.15f).SetEase(Ease.OutBack));
        
        // 3. Bounce back from impact
        collisionSequence.Append(arrow.transform.DOLocalMove(originalPosition, collisionShakeDuration * 0.2f).SetEase(Ease.InCubic));
        
        // 4. Shake animation (vibrate back and forth) - more visible
        collisionSequence.Append(arrow.transform.DOShakePosition(
            collisionShakeDuration * 0.5f, 
            collisionShakeStrength, 
            collisionVibrato, 
            90f, 
            false, 
            true
        ));
        
        // 5. Scale back to normal
        collisionSequence.Join(arrow.transform.DOScale(originalScale, collisionShakeDuration * 0.5f).SetEase(Ease.InQuad));
        
        // 6. Ensure final position
        collisionSequence.AppendCallback(() => {
            arrow.transform.localPosition = originalPosition;
            arrow.transform.localScale = originalScale;
        });
        
        // Also animate the blocking arrow if it exists
        if (IsValidPosition(blockedPosition) && board[blockedPosition.y, blockedPosition.x] != null)
        {
            ArrowCell blockingArrow = board[blockedPosition.y, blockedPosition.x];
            blockingArrow.transform.DOKill();
            
            Vector3 blockingOriginalPos = blockingArrow.transform.localPosition;
            Vector3 blockingOriginalScale = Vector3.one;
            
            // Calculate direction from blocking arrow to hitting arrow
            Vector3 directionFromBlocked = (originalPosition - blockedWorldPos).normalized;
            if (directionFromBlocked.magnitude < 0.1f)
            {
                directionFromBlocked = Vector3.left;
            }
            
            // Create blocking arrow animation sequence
            Sequence blockingSequence = DOTween.Sequence();
            
            // Move back slightly (recoil)
            Vector3 recoilPos = blockingOriginalPos + directionFromBlocked * (collisionImpactDistance * 0.5f);
            blockingSequence.Append(blockingArrow.transform.DOLocalMove(recoilPos, collisionShakeDuration * 0.15f).SetEase(Ease.OutCubic));
            
            // Scale bounce
            blockingSequence.Join(blockingArrow.transform.DOScale(blockingOriginalScale * 1.15f, collisionShakeDuration * 0.15f).SetEase(Ease.OutBack));
            
            // Return to position
            blockingSequence.Append(blockingArrow.transform.DOLocalMove(blockingOriginalPos, collisionShakeDuration * 0.2f).SetEase(Ease.InCubic));
            
            // Shake
            blockingSequence.Append(blockingArrow.transform.DOShakePosition(
                collisionShakeDuration * 0.5f, 
                collisionShakeStrength * 0.7f, 
                collisionVibrato, 
                90f, 
                false, 
                true
            ));
            
            // Scale back
            blockingSequence.Join(blockingArrow.transform.DOScale(blockingOriginalScale, collisionShakeDuration * 0.5f).SetEase(Ease.InQuad));
            
            // Ensure final position
            blockingSequence.AppendCallback(() => {
                blockingArrow.transform.localPosition = blockingOriginalPos;
                blockingArrow.transform.localScale = blockingOriginalScale;
            });
        }
        
        // Animate all affected arrows in front
        foreach (ArrowCell affectedArrow in affectedArrows)
        {
            if (affectedArrow == null || affectedArrow == arrow) continue;
            
            affectedArrow.transform.DOKill();
            Vector3 affectedOriginalPos = affectedArrow.transform.localPosition;
            Vector3 affectedOriginalScale = Vector3.one;
            
            // Calculate forward direction based on move direction
            Vector3 forwardDirection = Vector3.zero;
            if (moveDirection == Vector2Int.right)
            {
                forwardDirection = Vector3.right;
            }
            else if (moveDirection == Vector2Int.left)
            {
                forwardDirection = Vector3.left;
            }
            else if (moveDirection == Vector2Int.up)
            {
                forwardDirection = Vector3.up;
            }
            else if (moveDirection == Vector2Int.down)
            {
                forwardDirection = Vector3.down;
            }
            
            // Create animation sequence: move forward then back
            Sequence affectedSequence = DOTween.Sequence();
            
            // Move forward slightly
            Vector3 forwardPos = affectedOriginalPos + forwardDirection * (collisionImpactDistance * 0.3f);
            affectedSequence.Append(affectedArrow.transform.DOLocalMove(forwardPos, collisionShakeDuration * 0.2f).SetEase(Ease.OutCubic));
            
            // Return to original position
            affectedSequence.Append(affectedArrow.transform.DOLocalMove(affectedOriginalPos, collisionShakeDuration * 0.3f).SetEase(Ease.InCubic));
            
            // Ensure final position
            affectedSequence.AppendCallback(() => {
                affectedArrow.transform.localPosition = affectedOriginalPos;
                affectedArrow.transform.localScale = affectedOriginalScale;
            });
        }
        
        // Wait for collision animation to complete
        yield return collisionSequence.WaitForCompletion();
    }
    
    List<ArrowCell> FindAffectedHorizontalArrows(Vector2Int currentPos, Vector2Int collisionPos, Vector2Int moveDirection)
    {
        List<ArrowCell> affectedArrows = new List<ArrowCell>();
        
        // Find all arrows in front of clicked arrow
        // Based on the clicked arrow's position and direction
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (board[y, x] == null) continue;
                
                ArrowCell arrow = board[y, x];
                
                Vector2Int arrowPos = arrow.GetGridPosition();
                
                // Skip the arrow that is moving
                if (arrowPos == currentPos) continue;
                
                // Check if arrow is in front of clicked arrow based on move direction
                // "In front" means all arrows in the same row/column as clicked arrow, in the direction of movement
                bool isInFront = false;
                
                if (moveDirection == Vector2Int.right)
                {
                    // Click Right arrow: all arrows in same row, to the right of clicked arrow
                    isInFront = (arrowPos.y == currentPos.y && arrowPos.x > currentPos.x);
                }
                else if (moveDirection == Vector2Int.left)
                {
                    // Click Left arrow: all arrows in same row, to the left of clicked arrow
                    isInFront = (arrowPos.y == currentPos.y && arrowPos.x < currentPos.x);
                }
                else if (moveDirection == Vector2Int.up)
                {
                    // Click Up arrow: all arrows in same column, above clicked arrow
                    isInFront = (arrowPos.x == currentPos.x && arrowPos.y > currentPos.y);
                }
                else if (moveDirection == Vector2Int.down)
                {
                    // Click Down arrow: all arrows in same column, below clicked arrow
                    isInFront = (arrowPos.x == currentPos.x && arrowPos.y < currentPos.y);
                }
                
                if (isInFront)
                {
                    affectedArrows.Add(arrow);
                }
            }
        }
        
        return affectedArrows;
    }
    
    bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < boardWidth && pos.y >= 0 && pos.y < boardHeight;
    }
    
    void OnSpecialArrowExited()
    {
        Debug.Log("WIN! Special arrow exited the board!");
        
        // Set special arrow to null to prevent further checks
        specialArrow = null;
        
        // Animate remaining arrows with celebration effect
        AnimateRemainingArrows();
        
        // Delay level completion to let animation play
        StartCoroutine(DelayedLevelCompletion());
    }
    
    void AnimateRemainingArrows()
    {
        if (board == null) return;
        
        List<ArrowCell> remainingArrows = new List<ArrowCell>();
        
        // Collect all remaining arrows
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (board[y, x] != null)
                {
                    ArrowCell arrow = board[y, x];
                    // Skip special arrow (already exited)
                    if (!arrow.isSpecialArrow)
                    {
                        remainingArrows.Add(arrow);
                    }
                }
            }
        }
        
        if (remainingArrows.Count == 0) return;
        
        // Animate each arrow with staggered timing for wave effect
        float staggerDelay = 0.05f;
        float animationDuration = 0.8f;
        
        for (int i = 0; i < remainingArrows.Count; i++)
        {
            ArrowCell arrow = remainingArrows[i];
            float delay = i * staggerDelay;
            
            // Kill any existing tweens
            arrow.transform.DOKill();
            
            Vector3 originalScale = arrow.transform.localScale;
            Vector3 originalRotation = arrow.transform.localEulerAngles;
            
            // Create celebration sequence
            Sequence celebrationSequence = DOTween.Sequence();
            
            // 1. Scale bounce up
            celebrationSequence.Append(arrow.transform.DOScale(originalScale * 1.3f, animationDuration * 0.2f)
                .SetEase(Ease.OutBack)
                .SetDelay(delay));
            
            // 2. Rotate with bounce
            celebrationSequence.Join(arrow.transform.DORotate(originalRotation + new Vector3(0, 0, 360f), animationDuration * 0.4f, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic)
                .SetDelay(delay));
            
            // 3. Scale bounce down
            celebrationSequence.Append(arrow.transform.DOScale(originalScale * 0.8f, animationDuration * 0.2f)
                .SetEase(Ease.InBack));
            
            // 4. Final scale bounce
            celebrationSequence.Append(arrow.transform.DOScale(originalScale * 1.1f, animationDuration * 0.2f)
                .SetEase(Ease.OutBack));
            
            // 5. Scale back to normal
            celebrationSequence.Append(arrow.transform.DOScale(originalScale, animationDuration * 0.2f)
                .SetEase(Ease.InQuad));
            
            // 6. Fade out and scale down
            celebrationSequence.Append(arrow.transform.DOScale(Vector3.zero, animationDuration * 0.3f)
                .SetEase(Ease.InBack));
            
            // Fade out UI Images (backgroundImage and arrowImage)
            UnityEngine.UI.Image backgroundImage = arrow.GetComponent<UnityEngine.UI.Image>();
            if (backgroundImage != null)
            {
                celebrationSequence.Join(backgroundImage.DOFade(0f, animationDuration * 0.3f)
                    .SetDelay(delay + animationDuration * 0.5f));
            }
            
            // Fade out arrow image if exists
            if (arrow.arrowImage != null)
            {
                celebrationSequence.Join(arrow.arrowImage.DOFade(0f, animationDuration * 0.3f)
                    .SetDelay(delay + animationDuration * 0.5f));
            }
            
            // Destroy arrow after animation
            celebrationSequence.OnComplete(() =>
            {
                if (arrow != null && arrow.gameObject != null)
                {
                    Destroy(arrow.gameObject);
                }
            });
        }
    }
    
    IEnumerator DelayedLevelCompletion()
    {
       if (levelManager != null && levelManager.GetCurrentLevel() < 4)
        {
             yield return new WaitForSeconds(0.2f);
        }
        else
        {
            float waitTime = (boardHeight * boardWidth * 0.05f) + 1f; // Max stagger + animation time
            yield return new WaitForSeconds(Mathf.Min(waitTime, 2f)); // Cap at 2 seconds
            
        }

         if (levelManager != null)
            {
                levelManager.OnLevelCompleted();
            }
       
    }
    
    void ClearBoard()
    {
        if (board != null)
        {
            for (int y = 0; y < board.GetLength(0); y++)
            {
                for (int x = 0; x < board.GetLength(1); x++)
                {
                    if (board[y, x] != null)
                    {
                        Destroy(board[y, x].gameObject);
                    }
                }
            }
        }
        
        // Clear all children
        if (boardContainer != null)
        {
            foreach (Transform child in boardContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        board = null;
        specialArrow = null;
    }
    
    public void ClearBoardPublic()
    {
        ClearBoard();
    }
    
    public ArrowCell GetSpecialArrow()
    {
        return specialArrow;
    }
    
    // Get arrow by index (for tutorial)
    // Index calculation: index = y * boardWidth + x
    public ArrowCell GetArrowByIndex(int index)
    {
        if (board == null) return null;
        
        int x = index % boardWidth;
        int y = index / boardWidth;
        
        if (y >= 0 && y < boardHeight && x >= 0 && x < boardWidth)
        {
            return board[y, x];
        }
        
        return null;
    }
    
    // Get arrow world position (for tutorial hand positioning)
    public Vector3 GetArrowWorldPosition(ArrowCell arrow)
    {
        if (arrow == null) return Vector3.zero;
        return arrow.transform.position;
    }
    
    public void ShowHint()
    {
        if (specialArrow == null || board == null) return;
        
        // Clear previous hint
        ClearHint();
        
        // Find the nearest arrow that can move
        ArrowCell hintArrow = FindNearestMovableArrow();
        
        if (hintArrow != null)
        {
            highlightedArrow = hintArrow;
            
            // Start fade in/out animation for 2 seconds
            if (hintCoroutine != null)
            {
                StopCoroutine(hintCoroutine);
            }
            hintCoroutine = StartCoroutine(HintFadeAnimation());
        }
        else
        {
            Debug.Log("No movable arrow found!");
            
            // Show message in UI and auto shuffle after delay
            UIInGame uiInGame = FindFirstObjectByType<UIInGame>();
            if (uiInGame != null)
            {
                uiInGame.ShowNoMovableArrowMessage();
            }
        }
    }
    
    IEnumerator HintFadeAnimation()
    {
        if (highlightedArrow == null) yield break;
        
        Image backgroundImage = highlightedArrow.GetComponent<Image>();
        if (backgroundImage == null) yield break;
        
        Color originalColor = backgroundImage.color;
        float fadeDuration = 0.5f; // 0.5 second for each fade (4 times = 2 seconds total)
        
        // Kill any existing tweens
        backgroundImage.DOKill();
        
        // Animation: in -> out -> in -> out (4 times in 2 seconds)
        for (int i = 0; i < 4; i++)
        {
            // Fade in to hint color
            backgroundImage.DOColor(hintColor, fadeDuration)
                .SetEase(Ease.OutQuad);
            
            yield return new WaitForSeconds(fadeDuration);
            
            // Fade out to original color
            backgroundImage.DOColor(originalColor, fadeDuration)
                .SetEase(Ease.InQuad);
            
            yield return new WaitForSeconds(fadeDuration);
        }
        
        // Clear hint after animation completes
        ClearHint(originalColor);
    }
    
    ArrowCell FindNearestMovableArrow()
    {
        if (specialArrow == null) return null;
        
        Vector2Int specialPos = specialArrow.GetGridPosition();
        ArrowCell nearestArrow = null;
        float nearestDistance = float.MaxValue;
        
        // Search through all arrows
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (board[y, x] == null) continue;
                
                ArrowCell arrow = board[y, x];
                
                // Skip the special arrow itself
                //if (arrow.isSpecialArrow) continue;
                
                // Check if this arrow can move
                if (CanArrowMove(arrow))
                {
                    // Calculate distance to special arrow
                    Vector2Int arrowPos = arrow.GetGridPosition();
                    float distance = Vector2Int.Distance(arrowPos, specialPos);
                    
                    // Find the nearest one
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestArrow = arrow;
                    }
                }
            }
        }
        
        return nearestArrow;
    }
    
    bool CanArrowMove(ArrowCell arrow)
    {
        if (arrow == null) return false;
        
        Vector2Int currentPos = arrow.GetGridPosition();
        Vector2Int direction = arrow.GetDirectionVector();
        Vector2Int nextPos = currentPos + direction;
        
        // Check if arrow can move (not blocked and valid position)
        if (IsValidPosition(nextPos))
        {
            // Check if there's an arrow blocking the path
            if (board[nextPos.y, nextPos.x] == null)
            {
                return true; // Can move
            }
        }
        else
        {
            // Can exit the board
            return true;
        }
        
        return false; // Blocked
    }
    
    
    public void ClearHint(Color originalColor)
    {
        if (highlightedArrow != null)
        {
            highlightedArrow.SetHighlight(false, originalColor);
            highlightedArrow = null;
        }
        
        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
            hintCoroutine = null;
        }
    }

    public void ClearHint()
    {
        if (highlightedArrow != null)
        {
           ClearHint(highlightedArrow.OriginalColor);
        }
    }
}

