using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool Paused = false;
    public GameObject PauseMenuCanvas;
    public string sceneToLoad;
    [SerializeField] private bool allowPauseWithoutCanvas = false;
    [SerializeField] private AudioClip uiClickSfx;

    void Start()
    {
        Time.timeScale = 1f;
        Paused = false;

        if (PauseMenuCanvas != null)
            PauseMenuCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape) && (PauseMenuCanvas != null || allowPauseWithoutCanvas))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        PlayUiClick(0.8f);

        if (Paused)
            Play();
        else
            Stop();
    }

    public void Play()
    {
        if (PauseMenuCanvas != null)
            PauseMenuCanvas.SetActive(false);

        Time.timeScale = 1f;
        Paused = false;
    }

    public void Stop()
    {
        if (PauseMenuCanvas != null)
            PauseMenuCanvas.SetActive(true);

        Time.timeScale = 0f;
        Paused = true;
    }

    public void MainMenuButton()
    {
        PlayUiClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneToLoad);
    }

    private void PlayUiClick(float volumeScale = 1f)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUiClick(uiClickSfx, volumeScale);
    }
}
