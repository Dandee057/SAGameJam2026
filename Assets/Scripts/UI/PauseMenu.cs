using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")] public GameObject pausePanel;
    public Button resumeButton;
    public Button mainMenuButton;

    [Header("Settings")] public string mainMenuSceneName = "Main Menu";
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode altPauseKey = KeyCode.P;

    [Tooltip("Also pause all game audio while paused.")]
    public bool pauseAudio = false;

    public static bool IsPaused { get; private set; }

    private PlayerController player;
    private bool playerWasLocked;

    private void Awake()
    {
        if (pausePanel)
        {
            pausePanel.SetActive(false);
        }

        if (resumeButton)
        {
            resumeButton.onClick.AddListener(Resume);
        }

        if (mainMenuButton)
        {
            mainMenuButton.onClick.AddListener(ExitToMainMenu);
        }

        SetPaused(false, true);
    }

    private void OnDestroy()
    {
        if (IsPaused)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsPaused = false;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(pauseKey) && !Input.GetKeyDown(altPauseKey))
        {
            return;
        }

        if (IsPaused)
        {
            Resume();
            return;
        }

        if (MinigameManager.IsRunning)
        {
            return;
        }


        GameFlow flow = FindAnyObjectByType<GameFlow>();
        if (flow != null && flow.Phase != GamePhase.Sabotage)
        {
            return;
        }

        Pause();
    }


    public void Pause()
    {
        SetPaused(true);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void ExitToMainMenu()
    {
        SetPaused(false);
        SceneManager.LoadScene(mainMenuSceneName);
    }


    private void SetPaused(bool paused, bool force = false)
    {
        if (!force && paused == IsPaused)
        {
            return;
        }

        IsPaused = paused;

        Time.timeScale = paused ? 0f : 1f;
        if (pauseAudio)
        {
            AudioListener.pause = paused;
        }

        if (pausePanel)
        {
            pausePanel.SetActive(paused);
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
        }

        if (player != null)
        {
            if (paused)
            {
                playerWasLocked = player.inputLocked;
                player.inputLocked = true;
            }
            else if (!force)
            {
                player.inputLocked = playerWasLocked;
            }
        }

        if (paused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (resumeButton)
            {
                resumeButton.Select();
            }
        }
    }
}