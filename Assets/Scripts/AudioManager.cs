using UnityEngine;

public class AudioManager : MonoBehaviour
{
    const string BgmPrefKey = "Audio_BGM_Enabled";
    const string SfxPrefKey = "Audio_SFX_Enabled";

    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] AudioClip clickClip;

    public bool IsBgmEnabled { get; private set; } = true;
    public bool IsSfxEnabled { get; private set; } = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        IsBgmEnabled = PlayerPrefs.GetInt(BgmPrefKey, 1) == 1;
        IsSfxEnabled = PlayerPrefs.GetInt(SfxPrefKey, 1) == 1;
        ApplyAudioState();
    }

    void Start()
    {
        if (bgmSource != null && IsBgmEnabled && !bgmSource.isPlaying)
        {
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void SetBgmEnabled(bool isEnabled)
    {
        IsBgmEnabled = isEnabled;
        PlayerPrefs.SetInt(BgmPrefKey, IsBgmEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAudioState();
    }

    public void SetSfxEnabled(bool isEnabled)
    {
        IsSfxEnabled = isEnabled;
        PlayerPrefs.SetInt(SfxPrefKey, IsSfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAudioState();
    }

    public void PlayClick()
    {
        if (!IsSfxEnabled || sfxSource == null || clickClip == null) return;
        sfxSource.PlayOneShot(clickClip);
    }

    void ApplyAudioState()
    {
        if (bgmSource != null)
        {
            if (IsBgmEnabled)
            {
                if (!bgmSource.isPlaying)
                {
                    bgmSource.loop = true;
                    bgmSource.Play();
                }
                bgmSource.mute = false;
            }
            else
            {
                bgmSource.mute = true;
            }
        }

        if (sfxSource != null)
        {
            sfxSource.mute = !IsSfxEnabled;
        }
    }
}
