using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultScreen : MonoBehaviour
{
    [Serializable]
    public class FlipbookArt
    {
        [Tooltip("The Image that shows the artwork.")]
        public Image image;

        [Tooltip("The sprites to flip between, in order. Two is typical; one = a still.")]
        public Sprite[] frames;

        [Tooltip("Seconds each sprite stays on screen.")]
        public float frameTime = 0.5f;

        public void Apply(float elapsed)
        {
            if (image == null || frames == null || frames.Length == 0)
            {
                return;
            }

            int index = (int)(elapsed / Mathf.Max(0.05f, frameTime)) % frames.Length;
            image.sprite = frames[index];
        }
    }

    [Header("Win = they broke up")] public GameObject winPanel;
    public FlipbookArt winArt;
    public Button mainMenuButton;
    public string mainMenuSceneName = "MainMenu";
    public UnityEvent onMainMenu;

    [Header("Lose = still together")] public GameObject losePanel;
    public FlipbookArt loseArt;
    public Button nextLevelButton;

    [Tooltip("Scene to load for the next level. Leave empty to only fire the event.")]
    public string nextLevelSceneName;

    public UnityEvent onNextLevel;

    [Header("Sounds")]
    [Tooltip("Played when the WIN panel appears (e.g. Win_Fanfare).")]
    public AudioClip winSound;
    [Tooltip("Played when the LOSE panel appears. Optional.")]
    public AudioClip loseSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    private bool showing;
    private bool shownWin;
    private float shownAt;

    private void Awake()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }

        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(MainMenu);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(Retry);
        }

        // Start hidden, even if the panel was left switched on in the scene.
        // (Show() switches it back on; Awake only runs once, so it won't hide it again.)
        if (!showing)
        {
            gameObject.SetActive(false);
        }
    }

    public void Show(bool won)
    {
        showing = true;               // set first: if this object starts off, SetActive runs Awake right here
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (winPanel == null || losePanel == null)
        {
            Debug.LogError("ResultScreen: 'winPanel' and 'losePanel' must be assigned.", this);
            return;
        }

        winPanel.SetActive(won);
        losePanel.SetActive(!won);

        showing = true;
        shownWin = won;
        shownAt = Time.unscaledTime;
        (won ? winArt : loseArt)?.Apply(0f);

        Sfx.Play(won ? winSound : loseSound, soundVolume, 0f);
    }

    private void Update()
    {
        if (!showing)
        {
            return;
        }

        (shownWin ? winArt : loseArt)?.Apply(Time.unscaledTime - shownAt);
    }

    /// Retry / next level. With no scene name set it restarts this level (skipping the intro).
    public void Retry()
    {
        if (leaving) return;   // ignore double clicks / being hooked up twice
        leaving = true;
        onNextLevel?.Invoke();
        string current = SceneManager.GetActiveScene().name;
        string target = string.IsNullOrEmpty(nextLevelSceneName) ? current : nextLevelSceneName;
        GameFlow.RetryRequested = target == current;
        Time.timeScale = 1f;
        SceneManager.LoadScene(target);
    }

    private bool leaving;

    /// Hook this to a button's OnClick to go back to the main menu.
    public void MainMenu()
    {
        if (leaving) return;
        leaving = true;
        Go(mainMenuSceneName, onMainMenu);
    }

    private static void Go(string sceneName, UnityEvent evt)
    {
        evt?.Invoke();
        if (string.IsNullOrEmpty(sceneName)) return;

        // forgive "MainMenu" vs "Main Menu" style mismatches
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            string spaced = sceneName == "MainMenu" ? "Main Menu" : sceneName.Replace(" ", "");
            if (Application.CanStreamedLevelBeLoaded(spaced)) sceneName = spaced;
            else Debug.LogError($"ResultScreen: scene '{sceneName}' isn't in the build list.");
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}