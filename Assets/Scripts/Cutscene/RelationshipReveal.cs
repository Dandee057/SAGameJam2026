using System.Collections;
using UnityEngine;

/// The "verdict" moment after the cutscenes: the relationship bar flies to the middle of the screen,
/// grows, holds for a beat so the player can read where the needle ended up, then the result panel shows.
///
/// SETUP: put this on the RelationshipMeter object (the one with the bar + needle + hearts).
/// CutscenePlayer finds it automatically and plays it before the result screen.
/// Optionally drag MainPanel / TopStrip / BottomStrip into 'Hide During Reveal' so only the bar is on screen.
public class RelationshipReveal : MonoBehaviour
{
    [Tooltip("What moves and grows. Leave empty = this object.")]
    public RectTransform target;

    [Header("Motion")]
    [Tooltip("Seconds to fly to the middle and grow.")]
    public float moveTime = 0.6f;
    [Tooltip("How big it gets (1 = normal size).")]
    public float enlargeScale = 1.8f;
    [Tooltip("Nudge the centre position (UI units). 0,0 = dead centre of the cutscene panel.")]
    public Vector2 centreOffset = Vector2.zero;
    [Tooltip("Overshoot on arrival. 0 = smooth stop, ~1.5 = bouncy.")]
    public float overshoot = 1.2f;

    [Header("Timing")]
    [Tooltip("Pause before it starts moving (lets the needle finish settling).")]
    public float delayBefore = 0.2f;
    [Tooltip("How long it sits big in the middle before the result panel comes up.")]
    public float holdTime = 1.5f;

    [Header("Punch when it lands")]
    public float punchAmount = 0.15f;
    public float punchTime = 0.25f;

    [Header("Clean up the screen")]
    [Tooltip("Hidden while the bar is in the middle (e.g. MainPanel, TopStrip, BottomStrip). Shown again next time.")]
    public GameObject[] hideDuringReveal;

    [Header("Sound (optional)")]
    public AudioClip whooshSound;
    public AudioClip landSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    private Vector3 restLocalPos, restScale;
    private bool captured;

    private RectTransform T => target != null ? target : (RectTransform)transform;

    private void Awake() => Capture();

    private void Capture()
    {
        if (captured) return;
        restLocalPos = T.localPosition;
        restScale = T.localScale;
        captured = true;
    }

    /// Puts the bar back where it was (called automatically when a new cutscene starts).
    public void ResetReveal()
    {
        Capture();
        T.localPosition = restLocalPos;
        T.localScale = restScale;
        foreach (GameObject g in hideDuringReveal)
            if (g != null) g.SetActive(true);
    }

    public IEnumerator Play()
    {
        Capture();
        RectTransform rt = T;
        RectTransform parent = rt.parent as RectTransform;

        if (delayBefore > 0f) yield return new WaitForSeconds(delayBefore);

        foreach (GameObject g in hideDuringReveal)
            if (g != null) g.SetActive(false);

        Sfx.Play(whooshSound, soundVolume, 0.05f);

        Vector3 fromPos = rt.localPosition;
        Vector3 toPos = parent != null
            ? (Vector3)(parent.rect.center + centreOffset)
            : (Vector3)centreOffset;
        // keep the pivot honest: the centre of OUR rect should land on the centre of the parent
        Vector2 pivotShift = (rt.pivot - new Vector2(0.5f, 0.5f)) * rt.rect.size;
        toPos += (Vector3)(pivotShift * enlargeScale);
        toPos.z = fromPos.z;

        Vector3 fromScale = rt.localScale;
        Vector3 toScale = restScale * enlargeScale;

        float t = 0f;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / moveTime);
            float e = EaseOutBack(k, overshoot);
            rt.localPosition = Vector3.LerpUnclamped(fromPos, toPos, e);
            rt.localScale = Vector3.LerpUnclamped(fromScale, toScale, e);
            yield return null;
        }
        rt.localPosition = toPos;
        rt.localScale = toScale;

        Sfx.Play(landSound, soundVolume, 0.05f);

        // little punch so it feels like it "lands"
        t = 0f;
        while (t < punchTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / punchTime);
            rt.localScale = toScale * (1f + punchAmount * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        rt.localScale = toScale;

        if (holdTime > 0f) yield return new WaitForSeconds(holdTime);
    }

    private static float EaseOutBack(float x, float s)
    {
        float c3 = s + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + s * Mathf.Pow(x - 1f, 2f);
    }
}
