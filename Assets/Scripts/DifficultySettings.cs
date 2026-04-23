using UnityEngine;

[System.Serializable]
public class DifficultySettings
{
    [Header("Board Size")]
    public int boardWidth = 7;
    public int boardHeight = 10;
    
    [Header("Path Settings")]
    [Tooltip("Number of guaranteed paths from center to edge (minimum 1)")]
    [Min(1)]
    public int guaranteedPaths = 1;
    
    [Tooltip("Path complexity (0-1, 0 = straight line, 1 = very winding)")]
    [Range(0f, 1f)]
    public float pathComplexity = 0.5f;
    
    [Header("Obstacle Settings")]
    [Tooltip("Ratio of arrows blocking paths (0-1, 0 = no blocking, 1 = heavy blocking)")]
    [Range(0f, 1f)]
    public float obstacleDensity = 0.3f;
    
    [Header("Arrow Distribution")]
    [Tooltip("Ratio of arrows pointing toward center (makes it harder)")]
    [Range(0f, 1f)]
    public float arrowsPointingToCenter = 0.2f;
    
    [Header("Special Rules")]
    [Tooltip("Allow arrows to re-enter the board")]
    public bool allowReentry = false;
    
    [Tooltip("Maximum number of clicks allowed (0 = unlimited)")]
    public int maxClicks = 0;
}

