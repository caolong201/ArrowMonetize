using System;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerGM : MonoBehaviour {

    public Text infoText;

    void Awake()
    {
        GameMonetize.OnResumeGame += ResumeGame;
        GameMonetize.OnPauseGame += PauseGame;
    }

    public void ResumeGame()
    {
        // RESUME MY GAME
        infoText.text = "RESUME GAME";
        AudioListener.volume = 1f;
        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        // PAUSE MY GAME
        infoText.text = "GAME PAUSED";
        Time.timeScale = 0.01f;
        AudioListener.volume = 0f;
    }


    public void ShowAd()
    {
        GameMonetize.Instance.ShowAd();
    }
}
