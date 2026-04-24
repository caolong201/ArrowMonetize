using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections;

public class UIInGame : MonoBehaviour
{
    [Header("UI References")]
    public Button hintButton;
    public Button shuffleButton;
    public Button reloadButton;
    public Button backButton;
    public Button btnSetting;
    public TextMeshProUGUI noMovableArrowText;

    [Header("Settings Popup")]
    public GameObject settingPopup;
    public Button settingCloseButton;
    public Button buttonBgm;
    public Image bgmOnImage;
    public Image bgmOffImage;
    public Button buttonSfx;
    public Image sfxOnImage;
    public Image sfxOffImage;
    
    [Header("Lives UI")]
    public GameObject livesPanel;
    public Image[] lifeImages = new Image[3]; // 3 images for 3 lives

    [Header("Level Timer UI")]
    public Slider timerSlider;
    public TextMeshProUGUI timerText;
    public Button btnSliderAds;
    public float levelDurationSeconds = 60f;
    public float adsButtonRevealRemainingSeconds = 15f;
    public GameObject timePopup;
    public Button timePopupCloseButton;
    public Button adsTimeButton;
    
    [Header("Tutorial UI")]
    public GameObject tutorialPanel; // Panel containing tutorial elements
    public TextMeshProUGUI tutorialText; // Text "TAP TO MOVE"
    public Image handImage; // Hand guide image
    public GameObject infoTutorial;
    
    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float messageDisplayDuration = 2f;
    public float lifeFadeOutDuration = 0.5f;
    public float handTapAnimationDuration = 1f; // Duration of one tap animation cycle
    
    private BoardManager boardManager;
    private UIManager uiManager;
    private LivesManager livesManager;
    private LevelManager levelManager;
    private CanvasGroup canvasGroup;
    private bool isTutorialActive = false;
    private bool hasTimeUpTriggeredLose = false;
    private Coroutine handAnimationCoroutine;
    private Coroutine levelTimerCoroutine;
    private Coroutine hideMessageCoroutine;
    private Tween adsButtonShakeTween;
    private bool isTimerPaused = false;
    private float remainingLevelTime = 0f;
#if UNITY_WEBGL || UNITY_EDITOR
    private bool waitingTimerAdReward = false;
#endif
    
    // Tutorial step system (configured per level)
    private int currentTutorialStep = 0;
    private int[] tutorialArrowIndices = Array.Empty<int>();
    private ArrowCell[] tutorialArrows = Array.Empty<ArrowCell>();
    
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
        boardManager = FindFirstObjectByType<BoardManager>();
        uiManager = FindFirstObjectByType<UIManager>();
        livesManager = FindFirstObjectByType<LivesManager>();
        levelManager = FindFirstObjectByType<LevelManager>();
        
        if (hintButton != null)
        {
            hintButton.onClick.AddListener(OnHintClicked);
        }
        
        if (shuffleButton != null)
        {
            shuffleButton.onClick.AddListener(OnShuffleClicked);
        }
        
        if (reloadButton != null)
        {
            reloadButton.onClick.AddListener(OnReloadClicked);
        }
        
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
        
        if (btnSetting != null)
        {
            btnSetting.onClick.AddListener(OnSettingClicked);
        }

        if (settingCloseButton != null)
        {
            settingCloseButton.onClick.AddListener(OnSettingCloseClicked);
        }

        if (buttonBgm != null)
        {
            buttonBgm.onClick.AddListener(OnToggleBgmClicked);
        }

        if (buttonSfx != null)
        {
            buttonSfx.onClick.AddListener(OnToggleSfxClicked);
        }

        if (btnSliderAds != null)
        {
            btnSliderAds.gameObject.SetActive(false);
            btnSliderAds.onClick.AddListener(OnBtnSliderAdsClicked);
        }

        if (timePopup != null)
        {
            timePopup.SetActive(false);
        }
        
        if (settingPopup != null)
        {
            settingPopup.SetActive(false);
        }
        RefreshSettingPopupVisual();

        if (timePopupCloseButton != null)
        {
            timePopupCloseButton.onClick.AddListener(OnTimePopupCloseClicked);
        }

        if (adsTimeButton != null)
        {
            adsTimeButton.onClick.AddListener(OnAdsTimeClicked);
        }
        
        // Hide message text initially
        if (noMovableArrowText != null)
        {
            noMovableArrowText.gameObject.SetActive(false);
        }
        
        // Hide tutorial initially
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        
        // Subscribe to lives changed event
        if (livesManager != null)
        {
            livesManager.OnLivesChanged += OnLivesChanged;
            // Initialize lives display
            UpdateLivesDisplay(livesManager.GetCurrentLives());
        }
    }
    
    void OnDestroy()
    {
        // Stop hand animation coroutine
        if (handAnimationCoroutine != null)
        {
            StopCoroutine(handAnimationCoroutine);
        }
        
        // Unsubscribe from events
        if (livesManager != null)
        {
            livesManager.OnLivesChanged -= OnLivesChanged;
        }
        
        // Clean up tweens when object is destroyed
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }
        
        if (noMovableArrowText != null)
        {
            noMovableArrowText.DOKill();
        }
        
        if (tutorialText != null)
        {
            tutorialText.DOKill();
            tutorialText.transform.DOKill();
        }
        
        if (handImage != null)
        {
            handImage.DOKill();
            handImage.transform.DOKill();
        }

        if (levelTimerCoroutine != null)
        {
            StopCoroutine(levelTimerCoroutine);
        }

        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
        }

        if (adsButtonShakeTween != null && adsButtonShakeTween.IsActive())
        {
            adsButtonShakeTween.Kill();
        }

        if (btnSliderAds != null)
        {
            btnSliderAds.onClick.RemoveListener(OnBtnSliderAdsClicked);
            btnSliderAds.transform.DOKill();
        }
        
        if (btnSetting != null)
        {
            btnSetting.onClick.RemoveListener(OnSettingClicked);
        }

        if (settingCloseButton != null)
        {
            settingCloseButton.onClick.RemoveListener(OnSettingCloseClicked);
        }

        if (buttonBgm != null)
        {
            buttonBgm.onClick.RemoveListener(OnToggleBgmClicked);
        }

        if (buttonSfx != null)
        {
            buttonSfx.onClick.RemoveListener(OnToggleSfxClicked);
        }

        if (timePopupCloseButton != null)
        {
            timePopupCloseButton.onClick.RemoveListener(OnTimePopupCloseClicked);
        }

        if (adsTimeButton != null)
        {
            adsTimeButton.onClick.RemoveListener(OnAdsTimeClicked);
        }

#if UNITY_WEBGL || UNITY_EDITOR
        if (waitingTimerAdReward)
        {
            GameMonetize.OnResumeGame -= OnAdsTimeResume;
            waitingTimerAdReward = false;
        }
#endif
        
        // Clean up life images tweens
        if (lifeImages != null)
        {
            foreach (Image lifeImage in lifeImages)
            {
                if (lifeImage != null)
                {
                    lifeImage.DOKill();
                }
            }
        }
    }
    
    void OnLivesChanged(int currentLives)
    {
        UpdateLivesDisplay(currentLives);
    }
    
    void UpdateLivesDisplay(int currentLives)
    {
        if (lifeImages == null) return;
        
        // Fade out images from right to left (last to first)
        // If currentLives = 2, fade out image index 2 (third image)
        // If currentLives = 1, fade out image index 1 (second image)
        // If currentLives = 0, fade out image index 0 (first image)
        
        for (int i = 0; i < lifeImages.Length; i++)
        {
            if (lifeImages[i] == null) continue;
            
            // Image should be visible if i < currentLives
            bool shouldBeVisible = i < currentLives;
            
            // Kill any existing tweens
            lifeImages[i].DOKill();
            
            if (shouldBeVisible)
            {
                // Show and fade in
                lifeImages[i].gameObject.SetActive(true);
                Color color = lifeImages[i].color;
                color.a = 1f;
                lifeImages[i].color = color;
            }
            else
            {
                // Fade out
                lifeImages[i].DOFade(0f, lifeFadeOutDuration)
                    .SetEase(DG.Tweening.Ease.InQuad)
                    .OnComplete(() =>
                    {
                        if (lifeImages[i] != null)
                        {
                            lifeImages[i].gameObject.SetActive(false);
                        }
                    });
            }
        }
    }
    
    void OnHintClicked()
    {
        PlayClickSound();
        if (boardManager != null)
        {
            boardManager.ShowHint();
        }
    }
    
    void OnShuffleClicked()
    {
        PlayClickSound();
        if (boardManager != null)
        {
            boardManager.ShuffleBoard();
        }
    }
    
    void OnReloadClicked()
    {
        PlayClickSound();
        if (boardManager != null)
        {
            boardManager.ReloadBoard();
            
            // If tutorial is active, refresh arrow references
            if (isTutorialActive && levelManager != null)
            {
                int lv = levelManager.GetCurrentLevel();
                if (lv == 1 || lv == 4)
                {
                    StartCoroutine(RefreshTutorialAfterReload());
                }
            }
        }
    }
    
    IEnumerator RefreshTutorialAfterReload()
    {
        if (tutorialArrowIndices == null || tutorialArrowIndices.Length == 0)
        {
            yield break;
        }
        
        // Wait for board to be recreated
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        // Refresh arrow references
        if (boardManager != null)
        {
            for (int i = 0; i < tutorialArrowIndices.Length; i++)
            {
                tutorialArrows[i] = boardManager.GetArrowByIndex(tutorialArrowIndices[i]);
            }
        }
        
        // Restart current step with new arrow references
        StartTutorialStep(currentTutorialStep);
    }
    
    void OnBackClicked()
    {
        PlayClickSound();
        // Clear board before going back to home
        if (boardManager != null)
        {
            boardManager.ClearBoardPublic();
        }
        
        if (uiManager != null)
        {
            uiManager.ShowHome();
        }
    }
    
    void UpdateButtonVisibility(bool hideButtons)
    {
        // Hide/Show back button
        if (backButton != null)
        {
            backButton.gameObject.SetActive(!hideButtons);
        }
        
        // Hide/Show hint button
        if (hintButton != null)
        {
            hintButton.gameObject.SetActive(!hideButtons);
        }
    }

    bool ConfigureTutorialForLevel(int level)
    {
        // Level 1: 2x2 (indices 0,1,3)
        if (level == 1)
        {
            tutorialArrowIndices = new[] { 0, 1, 3 };
            tutorialArrows = new ArrowCell[tutorialArrowIndices.Length];
            return true;
        }
        
        // Level 4: 3x3 indices (index = y * 3 + x) with 8 steps:
        // 1(1,0) -> 4(1,1) -> 3(0,1) -> 0(0,0) -> 6(0,2) -> 7(1,2) -> 8(2,2) -> 5(2,1)
        if (level == 4)
        {
            tutorialArrowIndices = new[] { 1, 4, 0, 3, 6, 7, 8, 5 };
            tutorialArrows = new ArrowCell[tutorialArrowIndices.Length];
            return true;
        }
        
        // Unsupported level
        tutorialArrowIndices = Array.Empty<int>();
        tutorialArrows = Array.Empty<ArrowCell>();
        return false;
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
        
        // Reset lives display when showing
        if (livesManager != null)
        {
            UpdateLivesDisplay(livesManager.GetCurrentLives());
        }
        
        // Ensure levelManager reference is set
        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        
        // Configure tutorial for supported levels (1 or 4)
        int currentLevel = levelManager != null ? levelManager.GetCurrentLevel() : -1;
        bool shouldShowTutorial = ConfigureTutorialForLevel(currentLevel);
        
        // Hide/Show buttons and lives panel based on level (hide for level < 4)
        bool shouldHideButtons = currentLevel < 4;
        UpdateButtonVisibility(shouldHideButtons);
        
        // Hide/Show lives panel based on level
        if (livesPanel != null)
        {
            livesPanel.SetActive(!shouldHideButtons);
        }
        
        // Show infoTutorial from level 1 to 3, hide from level 4 onwards
        if (infoTutorial != null)
        {
            bool shouldShowInfoTutorial = currentLevel >= 1 && currentLevel <= 4;
            infoTutorial.SetActive(shouldShowInfoTutorial);
        }
        
        if (shouldShowTutorial)
        {
            ShowTutorial();
        }
        else
        {
            HideTutorial();
        }
        
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

        StartLevelTimer();
    }

    void StartLevelTimer()
    {
        hasTimeUpTriggeredLose = false;
        isTimerPaused = false;
        remainingLevelTime = Mathf.Max(1f, levelDurationSeconds);

        if (levelTimerCoroutine != null)
        {
            StopCoroutine(levelTimerCoroutine);
        }

        if (adsButtonShakeTween != null && adsButtonShakeTween.IsActive())
        {
            adsButtonShakeTween.Kill();
        }

        HideAdsSliderButton();

        if (timePopup != null)
        {
            timePopup.SetActive(false);
        }

        UpdateTimerUI(0f, Mathf.CeilToInt(remainingLevelTime));
        levelTimerCoroutine = StartCoroutine(LevelTimerCountdown());
    }

    IEnumerator LevelTimerCountdown()
    {
        float duration = Mathf.Max(1f, levelDurationSeconds);
        bool adsButtonShown = false;
        float revealAt = Mathf.Clamp(adsButtonRevealRemainingSeconds, 0f, duration);

        while (remainingLevelTime > 0f)
        {
            if (!isTimerPaused)
            {
                remainingLevelTime = Mathf.Max(0f, remainingLevelTime - Time.deltaTime);
            }

            float normalizedValue = (duration - remainingLevelTime) / duration;
            UpdateTimerUI(normalizedValue, Mathf.CeilToInt(remainingLevelTime));

            if (!adsButtonShown && remainingLevelTime <= revealAt)
            {
                adsButtonShown = true;
                ShowAdsSliderButton();
            }

            yield return null;
        }

        UpdateTimerUI(1f, 0);
        levelTimerCoroutine = null;

        if (!hasTimeUpTriggeredLose && gameObject.activeInHierarchy && uiManager != null)
        {
            hasTimeUpTriggeredLose = true;
            if (boardManager != null)
            {
                boardManager.TriggerLoseSequence();
            }
            else
            {
                uiManager.ShowLose();
            }
        }
    }

    void UpdateTimerUI(float sliderValue, int remainingSeconds)
    {
        if (timerSlider != null)
        {
            timerSlider.minValue = 0f;
            timerSlider.maxValue = 1f;
            timerSlider.value = Mathf.Clamp01(sliderValue);
        }

        if (timerText != null)
        {
            int minutes = remainingSeconds / 60;
            int seconds = remainingSeconds % 60;
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    void ShowAdsSliderButton()
    {
        if (btnSliderAds == null)
        {
            return;
        }

        btnSliderAds.gameObject.SetActive(true);

        btnSliderAds.transform.DOKill();
        btnSliderAds.transform.localScale = Vector3.one;
        btnSliderAds.transform.localRotation = Quaternion.identity;

        adsButtonShakeTween = btnSliderAds.transform
            .DOShakeRotation(0.7f, new Vector3(0f, 0f, 12f), 12, 90f, false)
            .SetLoops(-1, LoopType.Restart);
    }

    void HideAdsSliderButton()
    {
        if (adsButtonShakeTween != null && adsButtonShakeTween.IsActive())
        {
            adsButtonShakeTween.Kill();
        }

        if (btnSliderAds != null)
        {
            btnSliderAds.transform.DOKill();
            btnSliderAds.transform.localScale = Vector3.one;
            btnSliderAds.transform.localRotation = Quaternion.identity;
            btnSliderAds.gameObject.SetActive(false);
        }
    }

    void OnBtnSliderAdsClicked()
    {
        PlayClickSound();
        isTimerPaused = true;
        if (timePopup != null)
        {
            timePopup.SetActive(true);
        }
    }

    void OnTimePopupCloseClicked()
    {
        PlayClickSound();
        if (timePopup != null)
        {
            timePopup.SetActive(false);
        }

        isTimerPaused = false;
    }

    void OnAdsTimeClicked()
    {
        PlayClickSound();
#if UNITY_WEBGL || UNITY_EDITOR
        if (waitingTimerAdReward)
            return;

        if (GameMonetize.Instance == null)
        {
            Debug.LogWarning("GameMonetize.Instance is missing; cannot show ad.");
            return;
        }

        waitingTimerAdReward = true;
        GameMonetize.OnResumeGame += OnAdsTimeResume;
        GameMonetize.Instance.ShowAd();
#else
        Debug.LogWarning("Reward ads are only integrated for WebGL / Editor in this project.");
#endif
    }

    void OnSettingClicked()
    {
        PlayClickSound();
        SetSettingPopupVisible(true);
    }

    void OnSettingCloseClicked()
    {
        PlayClickSound();
        SetSettingPopupVisible(false);
    }

    void OnToggleBgmClicked()
    {
        PlayClickSound();
        if (AudioManager.Instance == null) return;
        
        AudioManager.Instance.SetBgmEnabled(!AudioManager.Instance.IsBgmEnabled);
        RefreshSettingPopupVisual();
    }

    void OnToggleSfxClicked()
    {
        PlayClickSound();
        if (AudioManager.Instance == null) return;
        
        AudioManager.Instance.SetSfxEnabled(!AudioManager.Instance.IsSfxEnabled);
        RefreshSettingPopupVisual();
    }

    void SetSettingPopupVisible(bool isVisible)
    {
        if (settingPopup != null)
        {
            settingPopup.SetActive(isVisible);
        }

        if (isVisible)
        {
            RefreshSettingPopupVisual();
        }
    }

    void RefreshSettingPopupVisual()
    {
        bool bgmEnabled = AudioManager.Instance == null || AudioManager.Instance.IsBgmEnabled;
        bool sfxEnabled = AudioManager.Instance == null || AudioManager.Instance.IsSfxEnabled;

        if (bgmOnImage != null) bgmOnImage.gameObject.SetActive(bgmEnabled);
        if (bgmOffImage != null) bgmOffImage.gameObject.SetActive(!bgmEnabled);

        if (sfxOnImage != null) sfxOnImage.gameObject.SetActive(sfxEnabled);
        if (sfxOffImage != null) sfxOffImage.gameObject.SetActive(!sfxEnabled);
    }

    void PlayClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayClick();
        }
    }

#if UNITY_WEBGL || UNITY_EDITOR
    void OnAdsTimeResume()
    {
        GameMonetize.OnResumeGame -= OnAdsTimeResume;
        waitingTimerAdReward = false;

        remainingLevelTime = Mathf.Max(1f, levelDurationSeconds);
        UpdateTimerUI(0f, Mathf.CeilToInt(remainingLevelTime));

        HideAdsSliderButton();

        if (timePopup != null)
        {
            timePopup.SetActive(false);
        }
        
        if (settingPopup != null)
        {
            settingPopup.SetActive(false);
        }

        isTimerPaused = false;
    }
#endif
    
    void ShowTutorial()
    {
        if (tutorialArrowIndices == null || tutorialArrowIndices.Length == 0)
        {
            isTutorialActive = false;
            return;
        }
        
        isTutorialActive = true;
        currentTutorialStep = 0;
        
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("tutorialPanel is null! Please assign it in the Inspector.");
        }
        
        // Wait for board to be ready, then get arrow references
        StartCoroutine(WaitForBoardAndStartTutorial());
    }
    
    IEnumerator WaitForBoardAndStartTutorial()
    {
        if (tutorialArrowIndices == null || tutorialArrowIndices.Length == 0)
        {
            yield break;
        }
        
        // Ensure tutorial panel is active from the start
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
        
        // Ensure boardManager reference
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
        }
        
        // Wait a few frames for board to be initialized
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        // Ensure panel is still active after wait
        if (tutorialPanel != null && !tutorialPanel.activeSelf)
        {
            Debug.LogWarning("tutorialPanel was deactivated! Reactivating...");
            tutorialPanel.SetActive(true);
        }
        
        // Try to get references to tutorial arrows
        int attempts = 0;
        const int maxAttempts = 10;
        
        while (attempts < maxAttempts)
        {
            // Keep panel active during retry loop
            if (tutorialPanel != null && !tutorialPanel.activeSelf)
            {
                tutorialPanel.SetActive(true);
            }
            
            bool allArrowsFound = true;
            
            if (boardManager != null)
            {
                for (int i = 0; i < tutorialArrowIndices.Length; i++)
                {
                    ArrowCell arrow = boardManager.GetArrowByIndex(tutorialArrowIndices[i]);
                    tutorialArrows[i] = arrow;
                    
                    if (arrow == null)
                    {
                        allArrowsFound = false;
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("WaitForBoardAndStartTutorial - boardManager is null!");
                allArrowsFound = false;
            }
            
            if (allArrowsFound)
            {
                // All arrows found, start tutorial
                if (tutorialPanel != null)
                {
                    tutorialPanel.SetActive(true);
                }
                StartTutorialStep(0);
                yield break;
            }
            
            attempts++;
            yield return new WaitForSeconds(0.1f);
        }
        
        // If we couldn't find arrows after max attempts, log warning but don't complete tutorial
        Debug.LogWarning("Could not find all tutorial arrows, but keeping tutorial active. Board may not be ready yet.");
        
        // Ensure panel is active before starting
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
        
        // Try to start anyway with what we have
        StartTutorialStep(0);
    }
    
    void StartTutorialStep(int stepIndex)
    {
        // Ensure tutorial panel is active
        if (tutorialPanel != null && !tutorialPanel.activeSelf)
        {
            tutorialPanel.SetActive(true);
        }
        
        if (tutorialArrowIndices == null || tutorialArrowIndices.Length == 0)
        {
            CompleteTutorial();
            return;
        }
        
        if (stepIndex < 0 || stepIndex >= tutorialArrowIndices.Length)
        {
            // Tutorial completed
            CompleteTutorial();
            return;
        }
        
        currentTutorialStep = stepIndex;
        ArrowCell targetArrow = tutorialArrows[stepIndex];
        
        if (targetArrow == null)
        {
            // Try to get arrow again (in case board was reloaded or not ready yet)
            if (boardManager != null)
            {
                targetArrow = boardManager.GetArrowByIndex(tutorialArrowIndices[stepIndex]);
                tutorialArrows[stepIndex] = targetArrow;
            }
            
            if (targetArrow == null)
            {
                // Arrow still not found, wait a bit and try again
                Debug.LogWarning($"Tutorial arrow at index {tutorialArrowIndices[stepIndex]} not found! Retrying...");
                // Ensure panel stays active during retry
                if (tutorialPanel != null)
                {
                    tutorialPanel.SetActive(true);
                }
                StartCoroutine(RetryTutorialStep(stepIndex));
                return;
            }
        }
        
        // Update tutorial text
        if (tutorialText != null)
        {
            tutorialText.gameObject.SetActive(true);
            tutorialText.text = "TAP TO MOVE";
            tutorialText.DOKill();
            
            // Fade in
            Color textColor = tutorialText.color;
            textColor.a = 0f;
            tutorialText.color = textColor;
            
            tutorialText.DOFade(1f, 0.3f)
                .SetEase(DG.Tweening.Ease.OutQuad);
            
            // Pulse animation
            tutorialText.transform.DOKill();
            tutorialText.transform.localScale = Vector3.one;
            tutorialText.transform.DOScale(Vector3.one * 1.1f, 0.8f)
                .SetEase(DG.Tweening.Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        
        // Position hand at target arrow
        PositionHandAtArrow(targetArrow);
        
        // Small delay to ensure position is set before starting animation
        StartCoroutine(DelayedStartHandAnimation());
    }
    
    IEnumerator DelayedStartHandAnimation()
    {
        yield return new WaitForEndOfFrame();
        // Start hand tap animation
        StartHandTapAnimation();
    }
    
    IEnumerator RetryTutorialStep(int stepIndex)
    {
        // Ensure panel stays active during retry
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
        
        // Wait a bit for board to be ready
        yield return new WaitForSeconds(0.2f);
        
        // Ensure panel is still active after wait
        if (tutorialPanel != null && !tutorialPanel.activeSelf)
        {
            Debug.LogWarning("tutorialPanel was deactivated during retry wait! Reactivating...");
            tutorialPanel.SetActive(true);
        }
        
        // Try again
        ArrowCell targetArrow = null;
        if (boardManager != null)
        {
            targetArrow = boardManager.GetArrowByIndex(tutorialArrowIndices[stepIndex]);
            tutorialArrows[stepIndex] = targetArrow;
        }
        
        if (targetArrow == null)
        {
            // Still not found, but don't complete tutorial - just log warning
            Debug.LogWarning($"Tutorial arrow at index {tutorialArrowIndices[stepIndex]} still not found after retry. Tutorial will wait.");
            // Ensure panel stays active
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }
            // Don't complete tutorial, just return - maybe board will be ready later
            yield break;
        }
        
        // Arrow found, continue with tutorial step
        currentTutorialStep = stepIndex;
        
        // Ensure panel is active before continuing
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
        
        // Update tutorial text
        if (tutorialText != null)
        {
            tutorialText.gameObject.SetActive(true);
            tutorialText.text = "TAP TO MOVE";
            tutorialText.DOKill();
            
            // Fade in
            Color textColor = tutorialText.color;
            textColor.a = 0f;
            tutorialText.color = textColor;
            
            tutorialText.DOFade(1f, 0.3f)
                .SetEase(DG.Tweening.Ease.OutQuad);
            
            // Pulse animation
            tutorialText.transform.DOKill();
            tutorialText.transform.localScale = Vector3.one;
            tutorialText.transform.DOScale(Vector3.one * 1.1f, 0.8f)
                .SetEase(DG.Tweening.Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        
        // Position hand at target arrow
        PositionHandAtArrow(targetArrow);
        
        // Start hand tap animation
        StartCoroutine(DelayedStartHandAnimation());
    }
    
    void PositionHandAtArrow(ArrowCell arrow)
    {
        if (handImage == null)
        {
            Debug.LogWarning("PositionHandAtArrow - handImage is null!");
            return;
        }
        
        if (arrow == null)
        {
            Debug.LogWarning("PositionHandAtArrow - arrow is null!");
            return;
        }
        
        // Get arrow's RectTransform (arrows are UI elements)
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        if (arrowRect == null)
        {
            Debug.LogWarning("PositionHandAtArrow - arrowRect is null!");
            return;
        }
        
        // Get hand's RectTransform
        RectTransform handRect = handImage.rectTransform;
        
        // Get canvases
        Canvas handCanvas = handImage.GetComponentInParent<Canvas>();
        Canvas arrowCanvas = arrowRect.GetComponentInParent<Canvas>();
        
        if (handCanvas != null && arrowCanvas != null)
        {
            // If both are in the same canvas, directly copy anchored position
            if (handCanvas == arrowCanvas)
            {
                // Get hand's parent
                RectTransform handParent = handRect.parent as RectTransform;
                RectTransform arrowParent = arrowRect.parent as RectTransform;
                
                if (handParent == arrowParent)
                {
                    // Same parent - direct copy
                    handRect.anchoredPosition = arrowRect.anchoredPosition;
                }
                else
                {
                    // Different parents - convert position
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        handParent,
                        RectTransformUtility.WorldToScreenPoint(arrowCanvas.worldCamera ?? Camera.main, arrowRect.position),
                        handCanvas.worldCamera ?? Camera.main,
                        out localPoint);
                    
                    handRect.anchoredPosition = localPoint;
                }
            }
            else
            {
                // Different canvases - convert through screen space
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                    arrowCanvas.worldCamera ?? Camera.main, 
                    arrowRect.position);
                
                RectTransform handParent = handRect.parent as RectTransform;
                if (handParent != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        handParent,
                        screenPos,
                        handCanvas.worldCamera ?? Camera.main,
                        out Vector2 localPoint);
                    
                    handRect.anchoredPosition = localPoint;
                }
            }
        }
        else
        {
            // Fallback: try to match world positions
            Debug.LogWarning("PositionHandAtArrow - Canvas not found, using world position fallback");
            handImage.transform.position = arrow.transform.position;
        }
    }
    
    public void OnTutorialArrowClicked(ArrowCell clickedArrow)
    {
        if (!isTutorialActive) return;
        
        // Check if clicked arrow matches current step target
        ArrowCell targetArrow = tutorialArrows[currentTutorialStep];
        if (targetArrow == null || clickedArrow != targetArrow)
        {
            // Wrong arrow clicked, ignore
            return;
        }
        
        // Correct arrow clicked! Move to next step
        NextTutorialStep();
    }
    
    void NextTutorialStep()
    {
        // Stop current hand animation
        if (handAnimationCoroutine != null)
        {
            StopCoroutine(handAnimationCoroutine);
            handAnimationCoroutine = null;
        }
        
        // Move to next step
        int nextStep = currentTutorialStep + 1;
        StartTutorialStep(nextStep);
    }
    
    void CompleteTutorial()
    {
        isTutorialActive = false;
        
        // Stop hand animation
        if (handAnimationCoroutine != null)
        {
            StopCoroutine(handAnimationCoroutine);
            handAnimationCoroutine = null;
        }
        
        // Hide tutorial UI
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        
        if (tutorialText != null)
        {
            tutorialText.DOKill();
            tutorialText.transform.DOKill();
            tutorialText.gameObject.SetActive(false);
        }
        
        if (handImage != null)
        {
            handImage.DOKill();
            handImage.transform.DOKill();
            handImage.gameObject.SetActive(false);
        }
    }
    
    void HideTutorial()
    {
        CompleteTutorial();
    }
    
    void StartHandTapAnimation()
    {
        if (handImage == null)
        {
            Debug.LogWarning("StartHandTapAnimation - handImage is null!");
            return;
        }
        
        handImage.gameObject.SetActive(true);
        
        // Stop existing animation
        if (handAnimationCoroutine != null)
        {
            StopCoroutine(handAnimationCoroutine);
        }
        
        handAnimationCoroutine = StartCoroutine(HandTapAnimationLoop());
    }
    
    IEnumerator HandTapAnimationLoop()
    {
        if (handImage == null) yield break;
        
        Vector3 originalPosition = handImage.rectTransform.anchoredPosition;
        Vector3 originalScale = Vector3.one;
        
        while (isTutorialActive && currentTutorialStep < tutorialArrowIndices.Length)
        {
            // Update position in case arrow moved
            ArrowCell targetArrow = tutorialArrows[currentTutorialStep];
            if (targetArrow != null)
            {
                PositionHandAtArrow(targetArrow);
                originalPosition = handImage.rectTransform.anchoredPosition;
            }
            
            // Reset scale
            handImage.transform.localScale = originalScale;
            
            // Kill any existing tweens
            handImage.transform.DOKill();
            handImage.rectTransform.DOKill();
            
            
            // 2. Scale down slightly (press effect)
            handImage.transform.DOScale(originalScale * 0.85f, handTapAnimationDuration * 0.3f)
                .SetEase(DG.Tweening.Ease.OutQuad);
            
            yield return new WaitForSeconds(handTapAnimationDuration * 0.3f);
            
            // 3. Move back up (release)
            handImage.rectTransform.DOAnchorPosY(originalPosition.y, handTapAnimationDuration * 0.2f)
                .SetEase(DG.Tweening.Ease.InQuad);
            
            // 4. Scale back to normal
            handImage.transform.DOScale(originalScale, handTapAnimationDuration * 0.2f)
                .SetEase(DG.Tweening.Ease.InQuad);
            
            yield return new WaitForSeconds(handTapAnimationDuration * 0.2f);
            
            // 5. Wait before next tap
            yield return new WaitForSeconds(handTapAnimationDuration * 0.5f);
        }
    }
    
    public void Hide()
    {
        hasTimeUpTriggeredLose = false;

        if (levelTimerCoroutine != null)
        {
            StopCoroutine(levelTimerCoroutine);
            levelTimerCoroutine = null;
        }

        if (adsButtonShakeTween != null && adsButtonShakeTween.IsActive())
        {
            adsButtonShakeTween.Kill();
        }

        if (btnSliderAds != null)
        {
            btnSliderAds.transform.DOKill();
            btnSliderAds.gameObject.SetActive(false);
        }

        if (timePopup != null)
        {
            timePopup.SetActive(false);
        }

        isTimerPaused = false;

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
    
    public void ShowNoMovableArrowMessage()
    {
        if (noMovableArrowText == null) return;
        
        // Stop existing message coroutine only (do not stop timer/tutorial coroutines)
        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
            hideMessageCoroutine = null;
        }
        
        // Kill any existing tweens on text
        noMovableArrowText.DOKill();
        
        // Show text with animation
        noMovableArrowText.gameObject.SetActive(true);
        Color textColor = noMovableArrowText.color;
        textColor.a = 0f;
        noMovableArrowText.color = textColor;
        
        // Fade in
        noMovableArrowText.DOFade(1f, 0.3f)
            .SetEase(DG.Tweening.Ease.OutQuad);
        
        // Start coroutine to hide message and shuffle after delay
        hideMessageCoroutine = StartCoroutine(HideMessageAndShuffle());
    }
    
    IEnumerator HideMessageAndShuffle()
    {
        yield return new WaitForSeconds(messageDisplayDuration);
        
        // Fade out
        if (noMovableArrowText != null)
        {
            noMovableArrowText.DOFade(0f, 0.3f)
                .SetEase(DG.Tweening.Ease.InQuad)
                .OnComplete(() =>
                {
                    if (noMovableArrowText != null)
                    {
                        noMovableArrowText.gameObject.SetActive(false);
                    }
                });
        }
        
        // Auto shuffle board
        if (boardManager != null)
        {
            boardManager.ShuffleBoard();
        }

        hideMessageCoroutine = null;
    }
    
}

