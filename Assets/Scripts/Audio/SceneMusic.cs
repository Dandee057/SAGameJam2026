using UnityEngine;

/// Plays a music track when this scene starts (through the AudioManager, so it uses the Music mixer group).
/// Drag an audio file into 'Clip', or leave it empty and type a track name from the AudioManager's Music list.
public class SceneMusic : MonoBehaviour
{
    [Tooltip("Drag the music file here. If set, this is used and Track Name is ignored.")]
    [SerializeField] private AudioClip clip;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    [Tooltip("Only used when Clip is empty: the name of a track in the AudioManager's Music list.")]
    [SerializeField] private string trackName;

    [SerializeField] private float fadeTime = 1f;

    private void Start()
    {
        if (AudioManager.Instance == null) return;

        if (clip != null)
            AudioManager.Instance.PlayMusic(clip, volume, fadeTime);
        else if (!string.IsNullOrEmpty(trackName))
            AudioManager.Instance.PlayMusic(trackName, fadeTime);
    }
}
