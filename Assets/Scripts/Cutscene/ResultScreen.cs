using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Shown after the cutscene. Remember: the player is the SABOTEUR.
///   Win  (they broke up)     -> winPanel  with a Main Menu button.
///   Lose (still together)    -> losePanel with a Next Level button.
///
/// Keep THIS object active in the scene and make the two panels its children (inactive) - the buttons get
/// hooked up in Awake.
public class ResultScreen : MonoBehaviour
{
    [Header("Win = they broke up")] public GameObject winPanel;
    public Button mainMenuButton;
    public string mainMenuSceneName = "MainMenu";
    public UnityEvent onMainMenu;

    [Header("Lose = still together")] public GameObject losePanel;
    public Button nextLevelButton;

    [Tooltip("Scene to load for the next level. Leave empty to only fire the event.")]
    public string nextLevelSceneName;

    public UnityEvent onNextLevel;

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
            mainMenuButton.onClick.AddListener(() => Go(mainMenuSceneName, onMainMenu));
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(() => Go(nextLevelSceneName, onNextLevel));
        }
    }

    public void Show(bool won)
    {
        gameObject.SetActive(true); // in case it was left disabled in the scene
        transform.SetAsLastSibling(); // draw on top of the cutscene panel (if they share a canvas)

        if (winPanel == null || losePanel == null)
        {
            Debug.LogError("ResultScreen: 'winPanel' and 'losePanel' must be assigned.", this);
            return;
        }

        winPanel.SetActive(won);
        losePanel.SetActive(!won);
    }

    private static void Go(string sceneName, UnityEvent evt)
    {
        evt?.Invoke();
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}