using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "ArrowsJam/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Board Settings")]
    public int defaultBoardWidth = 7;
    public int defaultBoardHeight = 10;
    public float cellSize = 1f;
    public float cellSpacing = 0.1f;
    
    [Header("Arrow Settings")]
    public Color normalArrowColor = Color.white;
    public Color specialArrowColor = Color.yellow;
    
    [Header("Level Progression")]
    public int widthIncreasePerLevel = 1;
    public int heightIncreasePerLevel = 0;
    public int maxLevels = 10;
}

