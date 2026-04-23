using UnityEngine;

public class MainGameManager : MonoBehaviour
{
    [Header("Managers")]
    public BoardManager boardManager;
    public LevelManager levelManager;
    
    void Start()
    {
        // Find or create managers if not assigned
        if (boardManager == null)
        {
            boardManager = FindFirstObjectByType<BoardManager>();
            if (boardManager == null)
            {
                GameObject boardObj = new GameObject("BoardManager");
                boardManager = boardObj.AddComponent<BoardManager>();
            }
        }
        
        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager == null)
            {
                GameObject levelObj = new GameObject("LevelManager");
                levelManager = levelObj.AddComponent<LevelManager>();
            }
        }
        
        Debug.Log("Arrows - Puzzle Escape initialized!");
    }
    
    // Note: Arrow clicks are handled by UI Button components in ArrowCell, not through Update/Input
}
