using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CutscenePlayer : MonoBehaviour
{
    [Header("UI")] public GameObject panel;
    public Image frameImage;
    public RelationshipMeter meter;
    public AudioSource audioSource;

    [Header("Playback")] public float pauseBetweenVignettes = 0.3f;
    [Tooltip("When the intro cutscene ends, its sound fades out over this many seconds so it doesn't play over the game music. 0 = cut instantly.")]
    public float introSoundFadeOut = 0.4f;

    [Tooltip("Multiplies EVERY frame's duration. 1 = as set on the vignettes, 0.5 = twice as fast, 2 = twice as slow.")]
    public float frameTimeScale = 1f;

    [Tooltip("Played if the player sabotaged nothing at all.")]
    public Vignette emptyFallback;

    [Header("Win condition (the player wants them to break up)")]
    [Tooltip("The player WINS (they break up) if the relationship score ends at or BELOW this. " +
             "Same scale as the RelationshipMeter (e.g. 0-10, starting at 5).")]
    public float winScoreAtOrBelow = 4f;

    [Tooltip("Optional: the bar flies to the middle and grows before the result shows. Auto-found on the RelationshipMeter if empty.")]
    public RelationshipReveal reveal;

    [Header("Caught")] [Tooltip("Played in this order when the timer ends and the player isn't hidden.")]
    public Vignette[] caughtVignettes;

    public bool Won { get; private set; }

    
    public event System.Action<Vignette, int> FrameShown;

    public IEnumerator Play(IReadOnlyList<RunLog.Entry> entries)
    {
        List<Vignette> queue = entries
            .OrderBy(e => e.vignette.playOrder) 
            .ThenBy(e => e.time)
            .Select(e => e.vignette)
            .ToList();

        if (queue.Count == 0 && emptyFallback != null)
        {
            queue.Add(emptyFallback);
        }

        yield return Run(queue, true);

        if (winScoreAtOrBelow < meter.min || winScoreAtOrBelow > meter.max)
        {
            Debug.LogWarning($"CutscenePlayer: 'Win Score At Or Below' ({winScoreAtOrBelow}) is outside the RelationshipMeter's range " +
                             $"({meter.min}-{meter.max}), so the result will always be the same.", this);
        }

        Won = meter.Value <= winScoreAtOrBelow;
        Debug.Log($"[Result] relationship score {meter.startValue} -> {meter.Value}; " +
                  $"player wins at {winScoreAtOrBelow} or lower -> {(Won ? "WIN (they broke up)" : "LOSE (still together)")}");
        
        if (reveal == null) reveal = FindReveal();
        if (reveal != null) yield return reveal.Play();
    }
    
    /// Closes the cutscene panel (used when the intro is skipped, e.g. on Retry).
    public void HidePanel()
    {
        StopVignetteSound();
        if (meter != null) meter.SetVisible(false);
        if (panel != null) panel.SetActive(false);
    }

    public IEnumerator PlayIntro(Vignette intro)
    {
        if (intro == null)
        {
            yield break;
        }

        if (panel == null || frameImage == null)
        {
            Debug.LogError("CutscenePlayer: 'panel' and 'frameImage' must be assigned.", this);
            yield break;
        }

        panel.SetActive(true);
        if (meter != null)
        {
            meter.SetVisible(false);
        }

        yield return PlayVignette(intro, false);

        panel.SetActive(false);

       
        FadeOutVignetteSound(introSoundFadeOut);
    }

    public IEnumerator PlayCaught()
    {
        List<Vignette> queue = caughtVignettes != null ? caughtVignettes.ToList() : new List<Vignette>();

        yield return Run(queue, false);

        Won = false; 
        Debug.Log("[Result] LOSE - the player wasn't hidden when the timer ran out (caught). The relationship bar isn't used.");
    }
    
    public IEnumerator PlayThrownOut(Vignette vignette)
    {
        List<Vignette> queue = vignette != null
            ? new List<Vignette> { vignette }
            : (caughtVignettes != null ? caughtVignettes.ToList() : new List<Vignette>());

        yield return Run(queue, false);

        Won = false; // thrown out = still together
        Debug.Log("[Result] LOSE - the suspicion meter filled (thrown out). The relationship bar isn't used.");
    }

    private IEnumerator Run(List<Vignette> queue, bool useMeter)
    {
        if (panel == null || frameImage == null || (useMeter && meter == null))
        {
            Debug.LogError(
                "CutscenePlayer: 'panel', 'frameImage' (and 'meter' for the normal cutscene) must be assigned.", this);
            yield break;
        }

        panel.SetActive(true);
        if (meter != null)
        {
            meter.SetVisible(useMeter);
        }

        if (useMeter)
        {
            meter.ResetMeter();
            if (reveal == null) reveal = FindReveal();
            if (reveal != null) reveal.ResetReveal();
        }

        foreach (Vignette v in queue)
        {
            if (v == null)
            {
                continue;
            }

            yield return PlayVignette(v, useMeter);
            yield return new WaitForSeconds(pauseBetweenVignettes);
        }
        
        if (useMeter)
        {
            yield return new WaitForSeconds(1f);
        }
    }
    
    private RelationshipReveal FindReveal()
    {
        RelationshipReveal r = meter.GetComponentInParent<RelationshipReveal>(true);
        if (r == null) r = meter.GetComponentInChildren<RelationshipReveal>(true);
        if (r == null) r = GetComponentInChildren<RelationshipReveal>(true);
        if (r == null) r = FindFirstObjectByType<RelationshipReveal>(FindObjectsInactive.Include);
        return r;
    }
    
    private void PlayVignetteSound(AudioClip clip)
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            if (AudioManager.Instance != null) audioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
    
    public void StopVignetteSound()
    {
        if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
        if (audioSource != null) { audioSource.Stop(); audioSource.volume = 1f; }
    }
    
    public void FadeOutVignetteSound(float seconds)
    {
        if (audioSource == null || !audioSource.isPlaying) return;
        if (seconds <= 0f) { StopVignetteSound(); return; }
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOutRoutine(seconds));
    }

    private Coroutine fadeRoutine;

    private IEnumerator FadeOutRoutine(float seconds)
    {
        float start = audioSource.volume, t = 0f;
        while (t < seconds && audioSource.isPlaying)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(start, 0f, t / seconds);
            yield return null;
        }
        fadeRoutine = null;
        audioSource.Stop();
        audioSource.volume = 1f;
    }

    private IEnumerator PlayVignette(Vignette v, bool useMeter)
    {
        StopVignetteSound();  
        if (v.frames == null || v.frames.Length == 0)
        {
            if (useMeter)
            {
                meter.Add(v.relationshipDelta);
            }

            yield return new WaitForSeconds(v.defaultFrameDuration);
            yield break;
        }

        int impact = Mathf.Clamp(v.impactFrame, 0, v.frames.Length - 1);
        for (int i = 0; i < v.frames.Length; i++)
        {
            frameImage.sprite = v.frames[i].sprite;
            FrameShown?.Invoke(v, i);

            if (i == impact)
            {
                if (useMeter)
                {
                    meter.Add(v.relationshipDelta);
                }

                if (v.sound != null)
                {
                    PlayVignetteSound(v.sound);
                }
            }

            yield return new WaitForSeconds(v.DurationOf(i) * frameTimeScale);
        }
    }
}