using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class UILose : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI loseText;
    [Tooltip("Shown after lose message animation finishes (e.g. Retry / Home panel).")]
    public GameObject losePopup;
    
    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float autoHideDelay = 3f;
    
    [Header("Lose Messages")]
    public List<string> loseMessages = new List<string>
    {
        "Don't give up! Try again!",
        "Almost there! Keep going!",
        "You're getting better!",
        "Next time you'll win!",
        "Practice makes perfect!",
        "Don't lose hope!",
        "You can do it!",
        "Keep trying!",
        "You're improving!",
        "One more try!"
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
    
    string GetRandomLoseMessage()
    {
        if (loseMessages == null || loseMessages.Count == 0)
        {
            return "Try again!";
        }
        
        return loseMessages[Random.Range(0, loseMessages.Count)];
    }
    
    void AnimateLoseText(System.Action onComplete = null)
    {
        if (loseText == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        // Kill any existing tweens
        loseText.transform.DOKill();
        
        // Reset transform
        loseText.transform.localScale = Vector3.one;
        loseText.transform.localRotation = Quaternion.identity;
        
        // Create animation sequence
        Sequence textSequence = DOTween.Sequence();
        
        // 1. Scale up with bounce
        textSequence.Append(loseText.transform.DOScale(Vector3.one * 1.2f, 0.3f)
            .SetEase(Ease.OutBack));
        
        // 2. Rotate slightly for emphasis
        textSequence.Join(loseText.transform.DORotate(new Vector3(0, 0, -5f), 0.2f)
            .SetEase(Ease.OutQuad));
        
        // 3. Rotate back
        textSequence.Append(loseText.transform.DORotate(new Vector3(0, 0, 5f), 0.2f)
            .SetEase(Ease.InOutQuad));
        
        // 4. Rotate to center
        textSequence.Append(loseText.transform.DORotate(Vector3.zero, 0.2f)
            .SetEase(Ease.InOutQuad));
        
        // 5. Scale bounce down
        textSequence.Append(loseText.transform.DOScale(Vector3.one * 0.95f, 0.15f)
            .SetEase(Ease.InQuad));
        
        // 6. Scale back to normal
        textSequence.Append(loseText.transform.DOScale(Vector3.one, 0.15f)
            .SetEase(Ease.OutQuad));
        
        if (onComplete != null)
            textSequence.OnComplete(() => onComplete.Invoke());
    }
    
    void ShowLosePopup()
    {
        if (losePopup != null)
            losePopup.SetActive(true);
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
        
        // Stop any existing auto-hide coroutine
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        
        if (losePopup != null)
            losePopup.SetActive(false);
        
        // Set random lose message
        if (loseText != null)
        {
            loseText.text = GetRandomLoseMessage();
        }
        
        void AfterMessageIntro()
        {
            AnimateLoseText(ShowLosePopup);
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
                    
                    // Animate lose text after fade in, then show lose popup
                    AfterMessageIntro();
                });
        }
        else
        {
            AfterMessageIntro();
        }
        
        // Auto-return home only when there is no lose popup (popup should own exit flow)
        if (losePopup == null)
            autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
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
        if (losePopup != null)
            losePopup.SetActive(false);
        
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
        
        if (loseText != null)
        {
            loseText.transform.DOKill();
        }
    }
}

