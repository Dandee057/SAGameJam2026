using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// Two modes:
///  Play()       - the normal cutscene: logged vignettes in playOrder, moving the bar. Judged from the FINAL value.
///  PlayCaught() - the player wasn't hidden: plays 'caughtVignettes' in order, no bar. Counts as a loss (they stay together).
/// Both always play to the end. Afterwards, read 'Won'. The player is the saboteur: Won = they broke up.
public class CutscenePlayer : MonoBehaviour
{
    [Header("UI")] public GameObject panel;
    public Image frameImage;
    public RelationshipMeter meter;
    public AudioSource audioSource;

    [Header("Playback")] public float pauseBetweenVignettes = 0.3f;

    [Tooltip("Multiplies EVERY frame's duration. 1 = as set on the vignettes, 0.5 = twice as fast, 2 = twice as slow.")]
    public float frameTimeScale = 1f;

    [Tooltip("Played if the player sabotaged nothing at all.")]
    public Vignette emptyFallback;

    [Header("Win condition (the player wants them to break up)")]
    [Tooltip("They break up (player wins) if the bar ends at or BELOW this value. The bar starts at 50.")]
    public float breakupThreshold = 25f;

    [Header("Caught")] [Tooltip("Played in this order when the timer ends and the player isn't hidden.")]
    public Vignette[] caughtVignettes;

    public bool Won { get; private set; }

    /// Fired every time a vignette frame goes on screen: (vignette, frame index).
    /// Used by ComicCutsceneStyle for the strips / pop / shake. Safe to ignore.
    public event System.Action<Vignette, int> FrameShown;

    public IEnumerator Play(IReadOnlyList<RunLog.Entry> entries)
    {
        List<Vignette> queue = entries
            .OrderBy(e => e.vignette.playOrder) // OrderBy is stable, so ties keep the order done
            .ThenBy(e => e.time)
            .Select(e => e.vignette)
            .ToList();

        if (queue.Count == 0 && emptyFallback != null)
        {
            queue.Add(emptyFallback);
        }

        yield return Run(queue, true);

        Won = meter.Value <= breakupThreshold;
    }

    /// Plays a one-off scene-setup vignette before the round starts. No bar, no relationship value:
    /// the vignette's relationshipDelta is ignored. Closes the panel afterwards so the world is visible again.
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
    }

    public IEnumerator PlayCaught()
    {
        List<Vignette> queue = caughtVignettes != null ? caughtVignettes.ToList() : new List<Vignette>();

        yield return Run(queue, false);

        Won = false; // got caught = still together
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

        // let the marker finish settling before the result shows
        if (useMeter)
        {
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator PlayVignette(Vignette v, bool useMeter)
    {
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

                if (v.sound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(v.sound);
                }
            }

            yield return new WaitForSeconds(v.DurationOf(i) * frameTimeScale);
        }
    }
}