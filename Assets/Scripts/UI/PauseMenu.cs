using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Simple pause menu: Esc (or P) toggles it. Resume / Exit to Main Menu.
/// Freezes the game with Time.timeScale = 0 (the clock, waiters, player and minigame timers all stop).
///
/// Setup: put this on an ALWAYS-ACTIVE object (e.g. the HUD Canvas), make a 'pausePanel' child that starts
/// hidden, and drag in the panel + the two buttons. The buttons are hooked up automatically.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button mainMenuButton;

    [Header("Settings")]
    public string mainMenuSceneName = "Main Menu";
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode altPauseKey = KeyCode.P;
    [Tooltip("Also pause all game audio while paused.")]
    public bool pauseAudio = false;

    public static bool IsPaused { get; private set; }

    private PlayerController player;
    private bool playerWasLocked;

    private void Awake()
    {
        if (pausePanel) pausePanel.SetActive(false);
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (mainMenuButton) mainMenuButton.onClick.AddListener(ExitToMainMenu);
        SetPaused(false, force: true);
    }

    private void OnDestroy()
    {
        // never leave the next scene frozen
        if (IsPaused) { Time.timeScale = 1f; AudioListener.pause = false; IsPaused = false; }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(pauseKey) && !Input.GetKeyDown(altPauseKey)) return;

        if (IsPaused) { Resume(); return; }

        // Don't steal Esc from an open minigame (Esc cancels it there).
        if (MinigameManager.IsRunning) return;

        // Don't pause during the end-of-round cutscene / result screen.
        GameFlow flow = FindAnyObjectByType<GameFlow>();
        if (flow != null && flow.Phase != GamePhase.Sabotage) return;

        Pause();
    }

    // ---------- button functions (also usable from OnClick) ----------

    public void Pause() => SetPaused(true);
    public void Resume() => SetPaused(false);

    public void ExitToMainMenu()
    {
        SetPaused(false);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ---------- internals ----------

    private void SetPaused(bool paused, bool force = false)
    {
        if (!force && paused == IsPaused) return;
        IsPaused = paused;

        Time.timeScale = paused ? 0f : 1f;
        if (pauseAudio) AudioListener.pause = paused;
        if (pausePanel) pausePanel.SetActive(paused);

        // stop the player reacting to keys (e.g. interact) while paused
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            if (paused) { playerWasLocked = player.inputLocked; player.inputLocked = true; }
            else if (!force) player.inputLocked = playerWasLocked;
        }

        if (paused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (resumeButton) resumeButton.Select();   // keyboard/gamepad can press Resume straight away
        }
    }
}
