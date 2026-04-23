using UnityEngine;
using System;

public class LivesManager : MonoBehaviour
{
    [Header("Lives Settings")]
    public int maxLives = 3;
    
    private int currentLives;
    
    public event Action<int> OnLivesChanged;
    public event Action OnLivesDepleted;
    
    void Awake()
    {
        ResetLives();
    }
    
    public void ResetLives()
    {
        currentLives = maxLives;
        OnLivesChanged?.Invoke(currentLives);
    }
    
    public void LoseLife()
    {
        if (currentLives > 0)
        {
            currentLives--;
            Debug.Log($"Lost a life! Remaining: {currentLives}");
            OnLivesChanged?.Invoke(currentLives);
            
            if (currentLives <= 0)
            {
                Debug.Log("All lives depleted!");
                OnLivesDepleted?.Invoke();
            }
        }
    }
    
    public int GetCurrentLives()
    {
        return currentLives;
    }
    
    public int GetMaxLives()
    {
        return maxLives;
    }
}

