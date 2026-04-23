using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class UIHome : MonoBehaviour
{
    [Header("UI References")]
    public Button playButton;
    public TextMeshProUGUI levelText;
    
    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float levelUpAnimationDuration = 0.8f;
    
    private LevelManager levelManager;
    private UIManager uiManager;
    private CanvasGroup canvasGroup;
    private int previousLevel = -1;
    
    void Awake()
    {
        // Get or add CanvasGroup component
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    void Start()
    {
        levelManager = FindFirstObjectByType<LevelManager>();
        uiManager = FindFirstObjectByType<UIManager>();
        
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }
        
        // Initialize previous level
        if (levelManager != null)
        {
            previousLevel = levelManager.GetCurrentLevel();
        }
        
        UpdateLevelDisplay();
    }
    
    void OnPlayClicked()
    {
        // Load level and show InGame UI
        if (levelManager != null)
        {
            int currentLevel = levelManager.GetCurrentLevel();
            int levelToLoad = currentLevel;
            
            // If level > 10, load random level from 7 to 10 (but don't update currentLevel)
            if (currentLevel > 10)
            {
                levelToLoad = Random.Range(7, 11); // Random from 7 to 10 (inclusive)
                levelManager.LoadLevel(levelToLoad, false); // Don't update currentLevel
            }
            else
            {
                levelManager.LoadLevel(levelToLoad, true); // Update currentLevel normally
            }
        }
        
        // Wait a frame to ensure level is loaded before showing UI
        StartCoroutine(DelayedShowInGame());
    }
    
    IEnumerator DelayedShowInGame()
    {
        yield return null; // Wait one frame
        if (uiManager != null)
        {
            uiManager.ShowInGame();
        }
    }
    
    public void UpdateLevelDisplay()
    {
        if (levelText != null && levelManager != null)
        {
            int currentLevel = levelManager.GetCurrentLevel();
            
            // Check if level increased
            bool isLevelUp = previousLevel != -1 && currentLevel > previousLevel;
            
            // Update text
            levelText.text = $"Level {currentLevel}";
            
            // Animate if level up
            if (isLevelUp)
            {
                AnimateLevelUp();
            }
            
            // Update previous level
            previousLevel = currentLevel;
        }
    }
    
    void AnimateLevelUp()
    {
        if (levelText == null) return;
        
        // Kill any existing tweens
        levelText.transform.DOKill();
        levelText.DOKill();
        
        // Store original values
        Vector3 originalScale = Vector3.one;
        Vector3 originalRotation = Vector3.zero;
        Color originalColor = levelText.color;
        
        // Create level up animation sequence
        Sequence levelUpSequence = DOTween.Sequence();
        
        // 1. Scale up dramatically with bounce
        levelUpSequence.Append(levelText.transform.DOScale(Vector3.one * 1.5f, levelUpAnimationDuration * 0.2f)
            .SetEase(DG.Tweening.Ease.OutBack));
        
        // 2. Rotate with bounce (celebration effect)
        levelUpSequence.Join(levelText.transform.DORotate(new Vector3(0, 0, 15f), levelUpAnimationDuration * 0.15f)
            .SetEase(DG.Tweening.Ease.OutQuad));
        
        // 3. Color flash to gold/yellow
        levelUpSequence.Join(levelText.DOColor(new Color(1f, 0.84f, 0f, 1f), levelUpAnimationDuration * 0.15f)
            .SetEase(DG.Tweening.Ease.OutQuad));
        
        // 4. Rotate back
        levelUpSequence.Append(levelText.transform.DORotate(new Vector3(0, 0, -15f), levelUpAnimationDuration * 0.15f)
            .SetEase(DG.Tweening.Ease.InOutQuad));
        
        // 5. Scale bounce down
        levelUpSequence.Append(levelText.transform.DOScale(Vector3.one * 0.9f, levelUpAnimationDuration * 0.15f)
            .SetEase(DG.Tweening.Ease.InBack));
        
        // 6. Rotate to center
        levelUpSequence.Join(levelText.transform.DORotate(Vector3.zero, levelUpAnimationDuration * 0.15f)
            .SetEase(DG.Tweening.Ease.InOutQuad));
        
        // 7. Color back to original
        levelUpSequence.Join(levelText.DOColor(originalColor, levelUpAnimationDuration * 0.15f)
            .SetEase(DG.Tweening.Ease.InQuad));
        
        // 8. Final scale bounce up
        levelUpSequence.Append(levelText.transform.DOScale(Vector3.one * 1.1f, levelUpAnimationDuration * 0.1f)
            .SetEase(DG.Tweening.Ease.OutBack));
        
        // 9. Scale back to normal
        levelUpSequence.Append(levelText.transform.DOScale(originalScale, levelUpAnimationDuration * 0.1f)
            .SetEase(DG.Tweening.Ease.InQuad));
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
        UpdateLevelDisplay();
        
        if (canvasGroup != null)
        {
            // Kill any existing tweens
            canvasGroup.DOKill();
            
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            canvasGroup.DOFade(1f, fadeDuration)
                .SetEase(DG.Tweening.Ease.OutQuad)
                .OnComplete(() =>
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                });
        }
    }
    
    public void Hide()
    {
        if (canvasGroup != null)
        {
            // Kill any existing tweens
            canvasGroup.DOKill();
            
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            canvasGroup.DOFade(0f, fadeDuration)
                .SetEase(DG.Tweening.Ease.InQuad)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        // Clean up tweens when object is destroyed
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }
        
        if (levelText != null)
        {
            levelText.transform.DOKill();
            levelText.DOKill();
        }
    }
}

