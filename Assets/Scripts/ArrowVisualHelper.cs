using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script to create arrow visuals using UI primitives
/// Attach this to ArrowCell to automatically generate arrow shape
/// </summary>
[RequireComponent(typeof(Image))]
public class ArrowVisualHelper : MonoBehaviour
{
    private Image arrowImage;
    
    void Awake()
    {
        arrowImage = GetComponent<Image>();
        
        // Create a simple arrow shape using a sprite
        // For now, we'll use a simple colored rectangle that can be rotated
        // In production, you should use actual arrow sprites
        
        if (arrowImage.sprite == null)
        {
            // Create a simple arrow texture programmatically
            arrowImage.sprite = CreateArrowSprite();
        }
    }
    
    Sprite CreateArrowSprite()
    {
        // Create a simple arrow shape
        int width = 64;
        int height = 64;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // Fill with transparent
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        // Draw arrow shape (simple triangle pointing up)
        int centerX = width / 2;
        int centerY = height / 2;
        int arrowSize = 20;
        
        // Draw arrow body (rectangle)
        for (int x = centerX - 4; x <= centerX + 4; x++)
        {
            for (int y = centerY - arrowSize / 2; y <= centerY + arrowSize / 2; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    pixels[y * width + x] = Color.white;
                }
            }
        }
        
        // Draw arrow head (triangle)
        for (int y = centerY + arrowSize / 2; y < centerY + arrowSize; y++)
        {
            int widthAtY = (y - (centerY + arrowSize / 2)) * 2;
            for (int x = centerX - widthAtY / 2; x <= centerX + widthAtY / 2; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    pixels[y * width + x] = Color.white;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
    
    public void SetDirection(ArrowDirection direction)
    {
        if (arrowImage == null) return;
        
        // Rotate the arrow based on direction
        float rotation = 0f;
        switch (direction)
        {
            case ArrowDirection.Up:
                rotation = 0f;
                break;
            case ArrowDirection.Down:
                rotation = 180f;
                break;
            case ArrowDirection.Left:
                rotation = 90f;
                break;
            case ArrowDirection.Right:
                rotation = -90f;
                break;
        }
        
        arrowImage.transform.localRotation = Quaternion.Euler(0, 0, rotation);
    }
}

