using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "FirstDate";
    [SerializeField] private string creditsSceneName = "Credits";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Buttons")]
    [SerializeField] private GameObject quitButton;

    private void Start()
    {
   
        ShowMainPanel();

        Time.timeScale = 1f;


#if UNITY_WEBGL && !UNITY_EDITOR
        if (quitButton != null) quitButton.SetActive(false);
#endif
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ShowMainPanel();
        }
    }

    [Tooltip("How long the menu music takes to fade out when Play is pressed. 0 = cut instantly.")]
    public float musicFadeOnPlay = 0.5f;

    public void Play()
    {
        // The AudioManager survives scene loads, so stop the menu music or it carries into the game.
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic(musicFadeOnPlay);
        SceneManager.LoadScene(gameSceneName);
    }

    public void HowToPlay()
    {
        ShowOnly(howToPlayPanel);
    }

    public void Options()
    {
        ShowOnly(optionsPanel);
    }

    public void Credits()
    {
        SceneManager.LoadScene(creditsSceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowMainPanel()
    {
        ShowOnly(mainPanel);
    }


    private void ShowOnly(GameObject panelToShow)
    {
        if (mainPanel != null) mainPanel.SetActive(panelToShow == mainPanel);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(panelToShow == howToPlayPanel);
        if (optionsPanel != null) optionsPanel.SetActive(panelToShow == optionsPanel);
    }
}
