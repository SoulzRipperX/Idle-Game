using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX")]
    [SerializeField] private AudioClip plotClickSfx;
    [SerializeField] private AudioClip sellSfx;
    [SerializeField] private AudioClip upgradeSfx;

    private const string MasterVolumeKey = "Volume";
    private const string BgmEnabledKey = "BGM_Enabled";

    public float MasterVolume { get; private set; } = 1f;
    public bool BgmEnabled { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
        ApplySettings();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        ApplySettings();
    }

    public void SetBgmEnabled(bool enabled)
    {
        BgmEnabled = enabled;
        PlayerPrefs.SetInt(BgmEnabledKey, BgmEnabled ? 1 : 0);
        ApplySettings();
    }

    public void PlayPlotClick(AudioClip overrideClip = null, float volumeScale = 1f)
    {
        AudioClip clip = overrideClip != null ? overrideClip : plotClickSfx;
        PlaySfx(clip, volumeScale);
    }

    public void PlaySell(AudioClip overrideClip = null, float volumeScale = 1f)
    {
        AudioClip clip = overrideClip != null ? overrideClip : sellSfx;
        PlaySfx(clip, volumeScale);
    }

    public void PlayUpgrade(AudioClip overrideClip = null, float volumeScale = 1f)
    {
        AudioClip clip = overrideClip != null ? overrideClip : upgradeSfx;
        PlaySfx(clip, volumeScale);
    }

    private void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        BgmEnabled = PlayerPrefs.GetInt(BgmEnabledKey, 1) == 1;
    }

    private void ApplySettings()
    {
        AudioListener.volume = MasterVolume;

        if (bgmSource != null)
        {
            bgmSource.mute = !BgmEnabled;
            if (BgmEnabled && !bgmSource.isPlaying)
                bgmSource.Play();
        }
    }

    private void PlaySfx(AudioClip clip, float volumeScale)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }
}
