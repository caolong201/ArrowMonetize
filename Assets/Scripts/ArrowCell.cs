using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ArrowCell : MonoBehaviour
{
    [Header("Arrow Properties")]
    public ArrowDirection direction;
    public bool isSpecialArrow = false; // Special yellow arrow
    
    [Header("Visual Components")]
    private Image backgroundImage;
    public Image arrowImage;
    public Sprite upArrowSprite;
    public Sprite downArrowSprite;
    public Sprite leftArrowSprite;
    public Sprite rightArrowSprite;
    
    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color specialColor = Color.yellow;
    public Color successColor = Color.green; // Green color when path is available
    public Color errorColor = Color.red; // Red color when no path available
    private Color originalColor;
    public Color OriginalColor { get { return originalColor; } }
    
    private Button button;
    private BoardManager boardManager;
    private Vector2Int gridPosition;
    
    void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }
        
        button.onClick.AddListener(OnArrowClicked);

        backgroundImage = GetComponent<Image>();
    }
    
    public void Initialize(ArrowDirection dir, Vector2Int pos, BoardManager manager, bool special = false)
    {
        direction = dir;
        gridPosition = pos;
        boardManager = manager;
        isSpecialArrow = special;
        
        UpdateVisual();
    }
    
    public void UpdateVisual()
    {
        // Set sprite based on direction
        switch (direction)
        {
            case ArrowDirection.Up:
                if (upArrowSprite != null) arrowImage.sprite = upArrowSprite;
                break;
            case ArrowDirection.Down:
                if (downArrowSprite != null) arrowImage.sprite = downArrowSprite;
                break;
            case ArrowDirection.Left:
                if (leftArrowSprite != null) arrowImage.sprite = leftArrowSprite;
                break;
            case ArrowDirection.Right:
                if (rightArrowSprite != null) arrowImage.sprite = rightArrowSprite;
                break;
        }
        
        // Set color
        Color targetColor = isSpecialArrow ? specialColor : normalColor;
        backgroundImage.color = targetColor;
        originalColor = targetColor;
    }
    
    public void SetHighlight(bool highlight, Color highlightColor)
    {
        if (backgroundImage != null)
        {
            if (highlight)
            {
                backgroundImage.color = highlightColor;
            }
            else
            {
                backgroundImage.color = originalColor;
            }
        }
    }
    
    void OnArrowClicked()
    {
        if (boardManager != null)
        {
            boardManager.OnArrowClicked(gridPosition);
        }
    }
    
    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }
    
    public Vector2Int GetDirectionVector()
    {
        switch (direction)
        {
            case ArrowDirection.Up:
                return Vector2Int.down; // Up in grid means y decreases (moves upward)
            case ArrowDirection.Down:
                return Vector2Int.up; // Down in grid means y increases (moves downward)
            case ArrowDirection.Left:
                return Vector2Int.left;
            case ArrowDirection.Right:
                return Vector2Int.right;
            default:
                return Vector2Int.zero;
        }
    }
    
    public void FadeColor(Color targetColor, float duration, System.Action onComplete = null)
    {
        if (backgroundImage == null) return;
        
        // Kill any existing color tweens
        backgroundImage.DOKill();
        
        // Store original color
        Color startColor = backgroundImage.color;
        
        // Create fade sequence: fade in to target color, then fade out to original color
        Sequence colorSequence = DOTween.Sequence();
        
        // Fade in to target color
        colorSequence.Append(backgroundImage.DOColor(targetColor, duration).SetEase(Ease.OutQuad));
        
        // Fade out to original color
        colorSequence.Append(backgroundImage.DOColor(originalColor, duration).SetEase(Ease.InQuad));
        
        // Callback when complete
        if (onComplete != null)
        {
            colorSequence.OnComplete(() => onComplete());
        }
    }
}

