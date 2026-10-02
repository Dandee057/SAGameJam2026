using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;


public class CreditsMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            BackToMainMenu();
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
