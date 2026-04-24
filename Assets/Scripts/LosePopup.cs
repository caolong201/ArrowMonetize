using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Out-of-hearts popup: Close and Replay restart the current level with full lives; BtnADS watches an ad for +1 life.
/// Assign buttons in the Inspector (Button Close, BtnReplay, BtnADS).
/// </summary>
public class LosePopup : MonoBehaviour
{
    [SerializeField] Button buttonClose;
    [SerializeField] Button btnReplay;
    [SerializeField] Button btnAds;

    UIManager uiManager;
    LevelManager levelManager;
    LivesManager livesManager;

#if UNITY_WEBGL || UNITY_EDITOR
    bool waitingAdReward;
#endif

    void Awake()
    {
        uiManager = FindFirstObjectByType<UIManager>();
        levelManager = FindFirstObjectByType<LevelManager>();
        livesManager = FindFirstObjectByType<LivesManager>();
    }

    void Start()
    {
        if (buttonClose != null)
            buttonClose.onClick.AddListener(OnCloseClicked);
        if (btnReplay != null)
            btnReplay.onClick.AddListener(OnReplayClicked);
        if (btnAds != null)
            btnAds.onClick.AddListener(OnAdsClicked);
    }

    void OnDestroy()
    {
#if UNITY_WEBGL || UNITY_EDITOR
        if (waitingAdReward)
        {
            GameMonetize.OnResumeGame -= OnAdResumeGrantContinue;
            waitingAdReward = false;
        }
#endif
        if (buttonClose != null)
            buttonClose.onClick.RemoveListener(OnCloseClicked);
        if (btnReplay != null)
            btnReplay.onClick.RemoveListener(OnReplayClicked);
        if (btnAds != null)
            btnAds.onClick.RemoveListener(OnAdsClicked);
    }

    void OnCloseClicked()
    {
        RestartCurrentLevelWithFullLives();
    }

    void OnReplayClicked()
    {
        RestartCurrentLevelWithFullLives();
    }

    void RestartCurrentLevelWithFullLives()
    {
        if (levelManager == null || uiManager == null)
            return;

        int level = levelManager.GetCurrentLevel();
        uiManager.ShowInGame();
        levelManager.LoadLevel(level, false, true);
    }

    void OnAdsClicked()
    {
        if (levelManager == null || uiManager == null || livesManager == null)
            return;

#if UNITY_WEBGL || UNITY_EDITOR
        if (waitingAdReward)
            return;

        if (GameMonetize.Instance == null)
        {
            Debug.LogWarning("GameMonetize.Instance is missing; cannot show ad.");
            return;
        }

        waitingAdReward = true;
        GameMonetize.OnResumeGame += OnAdResumeGrantContinue;
        GameMonetize.Instance.ShowAd();
#else
        Debug.LogWarning("Reward ads are only integrated for WebGL / Editor in this project.");
#endif
    }

#if UNITY_WEBGL || UNITY_EDITOR
    void OnAdResumeGrantContinue()
    {
        GameMonetize.OnResumeGame -= OnAdResumeGrantContinue;
        waitingAdReward = false;

        if (livesManager != null)
            livesManager.AddLife(1);

        if (levelManager == null || uiManager == null)
            return;

        int level = levelManager.GetCurrentLevel();
        uiManager.ShowInGame();
        levelManager.LoadLevel(level, false, false);
    }
#endif
}
