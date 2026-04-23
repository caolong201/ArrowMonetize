using UnityEngine;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public UIHome uiHome;
    public UIInGame uiInGame;
    public UIWin uiWin;
    public UILose uiLose;
    
    [SerializeField] private LevelManager levelManager;
    
    void Start()
    {
        // Check if level is 1, if so go directly to InGame
        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<LevelManager>();
        }
        
        if (levelManager != null && levelManager.GetCurrentLevel() == 1)
        {
            // Level 1: Load level and show InGame directly
            levelManager.LoadLevel(1, true);
            StartCoroutine(DelayedShowInGame());
        }
        else
        {
            // Show home screen by default for other levels
            ShowHome();
        }
    }
    
    IEnumerator DelayedShowInGame()
    {
        yield return null; // Wait one frame to ensure level is loaded
        ShowInGame();
    }
    
    public void ShowHome()
    {
        HideAllPanels();
        
        if (uiHome != null)
        {
            uiHome.Show();
        }
    }
    
    public void ShowInGame()
    {
        HideAllPanels();
        
        if (uiInGame != null)
        {
            uiInGame.Show();
        }
    }
    
    public void ShowWin()
    {
        HideAllPanels();
        
        if (uiWin != null)
        {
            uiWin.Show();
        }
    }
    
    public void ShowLose()
    {
        HideAllPanels();
        
        if (uiLose != null)
        {
            uiLose.Show();
        }
    }
    
    void HideAllPanels()
    {
        if (uiHome != null)
        {
            uiHome.Hide();
        }
        
        if (uiInGame != null)
        {
            uiInGame.Hide();
        }
        
        if (uiWin != null)
        {
            uiWin.Hide();
        }
        
        if (uiLose != null)
        {
            uiLose.Hide();
        }
    }
    
    public void UpdateLevelDisplay()
    {
        if (uiHome != null)
        {
            uiHome.UpdateLevelDisplay();
        }
    }
    
    public void ShowWinScreen()
    {
        ShowWin();
    }
    
    public void HideWinScreen()
    {
        if (uiWin != null)
        {
            uiWin.Hide();
        }
    }
    
}

