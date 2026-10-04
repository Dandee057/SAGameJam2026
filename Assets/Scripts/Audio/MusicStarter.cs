using System.Collections;
using UnityEngine;

/// Waits until GameFlow enters the Sabotage phase (after the intro walk-in),
/// then starts the looping background music with an optional fade-in.
/// Put this on the same GameObject as the music AudioSource.
/// IMPORTANT: untick "Play On Awake" on the AudioSource.
[RequireComponent(typeof(AudioSource))]
public class MusicStarter : MonoBehaviour
{
    [Tooltip("Leave empty to use the AudioSource on this object.")]
    public AudioSource source;

    [Tooltip("Extra seconds to wait after the round starts.")]
    public float extraDelay = 0f;

    [Tooltip("Seconds to fade from silent up to the target volume.")]
    public float fadeInTime = 1f;

    [Tooltip("Fade the music out when the timer runs out (cutscenes start).")]
    public bool stopWhenRoundEnds = false;
    public float fadeOutTime = 1f;

    [Tooltip("Lower the music while the pause menu is open.")]
    public bool quieterWhenPaused = true;
    [Range(0f, 1f)] public float pausedVolumeMultiplier = 0.4f;

    [Tooltip("If no GameFlow is found in the scene, start after this many seconds instead.")]
    public float fallbackDelay = 2f;

    GameFlow flow;
    float targetVolume;
    float fade;          // 0..1, driven by fade in/out
    bool started, stopping;

    void Awake()
    {
        if (!source) source = GetComponent<AudioSource>();
        targetVolume = source.volume;
        source.playOnAwake = false;
        source.loop = true;
        source.Stop();
    }

    void Start()
    {
        flow = FindFirstObjectByType<GameFlow>();
        if (!flow) StartCoroutine(BeginAfter(fallbackDelay));
    }

    void Update()
    {
        if (flow && !started && flow.Phase == GamePhase.Sabotage)
            StartCoroutine(BeginAfter(extraDelay));

        if (flow && started && !stopping && stopWhenRoundEnds &&
            (flow.Phase == GamePhase.Cutscene || flow.Phase == GamePhase.Finished))
            StartCoroutine(FadeOut());

        float pauseMul = (quieterWhenPaused && PauseMenu.IsPaused) ? pausedVolumeMultiplier : 1f;
        source.volume = targetVolume * fade * pauseMul;
    }

    IEnumerator BeginAfter(float delay)
    {
        started = true;
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        fade = 0f;
        source.Play();
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            fade = Mathf.Clamp01(t / fadeInTime);
            yield return null;
        }
        fade = 1f;
    }

    IEnumerator FadeOut()
    {
        stopping = true;
        float start = fade, t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            fade = Mathf.Lerp(start, 0f, t / fadeOutTime);
            yield return null;
        }
        fade = 0f;
        source.Stop();
    }
}
