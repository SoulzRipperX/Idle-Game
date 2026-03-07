using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Main_Game";
    public GameObject optionsMenu;
    public Slider volumeSlider;
    public Toggle bgmToggle;
    [SerializeField] private AudioClip uiClickSfx;


    void Start()
    {
        if (AudioManager.Instance != null)
        {
            if (volumeSlider != null)
                volumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);

            if (bgmToggle != null)
                bgmToggle.SetIsOnWithoutNotify(AudioManager.Instance.BgmEnabled);
        }
        else
        {
            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Volume", 1f));
                AudioListener.volume = volumeSlider.value;
            }

            if (bgmToggle != null)
                bgmToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("BGM_Enabled", 1) == 1);
        }

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            ToggleFullscreen();
        }
    }

    void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;

        if (Screen.fullScreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
    }

    public void PlayGame()
    {
        PlayUiClick();
        if (!string.IsNullOrWhiteSpace(gameSceneName))
            SceneManager.LoadScene(gameSceneName);
        else
            SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        PlayUiClick();
        Application.Quit();
    }

    public void OpenOptions()
    {
        PlayUiClick();
        if (optionsMenu != null)
        {
            optionsMenu.SetActive(true);
        }
    }

    public void CloseOptions()
    {
        PlayUiClick();
        if (optionsMenu != null)
        {
            optionsMenu.SetActive(false);
        }
    }

    public void AdjustVolume()
    {
        if (volumeSlider != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(volumeSlider.value);
            }
            else
            {
                AudioListener.volume = volumeSlider.value;
                PlayerPrefs.SetFloat("Volume", volumeSlider.value);
            }
        }
    }

    public GameObject creditPanel;

    public void OpenCredits()
    {
        PlayUiClick();
        if (creditPanel != null)
            creditPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        PlayUiClick();
        if (creditPanel != null)
            creditPanel.SetActive(false);
    }

    public void OnBgmToggleChanged(bool isOn)
    {
        PlayUiClick(0.7f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBgmEnabled(isOn);
        }
        else
        {
            PlayerPrefs.SetInt("BGM_Enabled", isOn ? 1 : 0);
        }
    }

    private void PlayUiClick(float volumeScale = 1f)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUiClick(uiClickSfx, volumeScale);
    }
}
