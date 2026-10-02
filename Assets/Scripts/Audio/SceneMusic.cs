using UnityEngine;


public class SceneMusic : MonoBehaviour
{
    [SerializeField] private string trackName;
    [SerializeField] private float fadeTime = 1f;

    private void Start()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(trackName))
        {
            AudioManager.Instance.PlayMusic(trackName, fadeTime);
        }
    }
}
