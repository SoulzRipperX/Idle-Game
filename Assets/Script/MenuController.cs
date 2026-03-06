using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class MenuController : MonoBehaviour
{
    public GameObject optionsMenu;
    public Slider volumeSlider;
    public Toggle bgmToggle;


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

        SceneManager.LoadScene(1);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void OpenOptions()
    {
        if (optionsMenu != null)
        {
            optionsMenu.SetActive(true);
        }
    }

    public void CloseOptions()
    {
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

    public void OnBgmToggleChanged(bool isOn)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBgmEnabled(isOn);
        }
        else
        {
            PlayerPrefs.SetInt("BGM_Enabled", isOn ? 1 : 0);
        }
    }
}
