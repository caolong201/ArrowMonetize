using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class UIWin : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI winText;
    
    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float autoHideDelay = 3f;
    
    [Header("Congratulations Messages")]
    public List<string> congratulationsMessages = new List<string>
    {
        "Amazing!",
        "Fantastic!",
        "Brilliant!",
        "Excellent!",
        "Outstanding!",
        "Perfect!",
        "Incredible!",
        "Wonderful!",
        "Superb!",
        "Magnificent!",
        "Spectacular!",
        "Awesome!",
        "Phenomenal!",
        "Extraordinary!",
        "Marvelous!"
    };
    
    private LevelManager levelManager;
    private UIManager uiManager;
    private CanvasGroup canvasGroup;
    private Coroutine autoHideCoroutine;
    
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
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
        
        // Stop any existing auto-hide coroutine
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }
        
        // Random select a congratulations message
        string congratulationsMessage = GetRandomCongratulationsMessage();
        
        if (winText != null)
        {
            if (levelManager != null)
            {
                winText.text = $"{congratulationsMessage}\nLevel {levelManager.GetCurrentLevel()-1} Completed!";
            }
            else
            {
                winText.text = congratulationsMessage;
            }
            
            // Animate text
            AnimateWinText();
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
        
        // Start auto-hide coroutine
        autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
    }
    
    string GetRandomCongratulationsMessage()
    {
        if (congratulationsMessages == null || congratulationsMessages.Count == 0)
        {
            return "Congratulations!";
        }
        
        int randomIndex = Random.Range(0, congratulationsMessages.Count);
        return congratulationsMessages[randomIndex];
    }
    
    void AnimateWinText()
    {
        if (winText == null) return;
        
        // Kill any existing tweens
        winText.transform.DOKill();
        winText.DOKill();
        
        // Reset scale and rotation
        winText.transform.localScale = Vector3.zero;
        winText.transform.localRotation = Quaternion.identity;
        
        // Create animation sequence
        Sequence textSequence = DOTween.Sequence();
        
        // 1. Scale bounce in
        textSequence.Append(winText.transform.DOScale(Vector3.one * 1.2f, 0.4f)
            .SetEase(DG.Tweening.Ease.OutBack));
        
        // 2. Rotate bounce
        textSequence.Join(winText.transform.DORotate(new Vector3(0, 0, 10f), 0.2f)
            .SetEase(DG.Tweening.Ease.OutQuad));
        
        // 3. Scale back to normal
        textSequence.Append(winText.transform.DOScale(Vector3.one, 0.3f)
            .SetEase(DG.Tweening.Ease.InQuad));
        
        // 4. Rotate back
        textSequence.Join(winText.transform.DORotate(Vector3.zero, 0.3f)
            .SetEase(DG.Tweening.Ease.InQuad));
        
        // 5. Continuous subtle bounce
        textSequence.Append(winText.transform.DOScale(Vector3.one * 1.05f, 0.5f)
            .SetEase(DG.Tweening.Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo));
    }
    
    IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSeconds(autoHideDelay);
        
        Hide();
        
        // Show Home after hiding
        if (uiManager != null)
        {
            uiManager.ShowHome();
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
        // Stop coroutine
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }
        
        // Clean up tweens when object is destroyed
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
        }
        
        if (winText != null)
        {
            winText.transform.DOKill();
            winText.DOKill();
        }
    }
}

