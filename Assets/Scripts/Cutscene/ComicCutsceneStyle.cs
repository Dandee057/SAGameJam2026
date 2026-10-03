using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Comic-book dressing for the CutscenePlayer:
///  - Two skewed coloured strips slash in across the top and bottom at the start of every event,
///    each time in new random colours.
///  - The main frame pops in with a slight random tilt on every frame.
///  - On the vignette's impact frame (the consequence) the page shakes and flashes.
///
/// Put this on the cutscene panel (or anywhere) and drag in the references.
/// It listens to CutscenePlayer.FrameShown, so it needs no other hookup.
/// </summary>
public class ComicCutsceneStyle : MonoBehaviour
{
    [Tooltip("Leave empty to find the CutscenePlayer in the scene.")]
    [SerializeField] private CutscenePlayer player;

    [Header("Strips (parent objects that slide; rotate them in the scene for the skew)")]
    [SerializeField] private RectTransform topStrip;
    [SerializeField] private RectTransform bottomStrip;
    [Tooltip("Images that get a random colour for the TOP strip (e.g. the main colour bar, an accent stripe).")]
    [SerializeField] private Image[] topStripColour;
    [Tooltip("Images that get a random colour for the BOTTOM strip.")]
    [SerializeField] private Image[] bottomStripColour;

    [Header("Main frame")]
    [Tooltip("The panel holding the frame image (border + picture). Pops and tilts.")]
    [SerializeField] private RectTransform mainPanel;
    [Tooltip("Optional full-screen white Image (alpha 0) for the impact flash. Untick Raycast Target on it.")]
    [SerializeField] private Image flash;

    [Header("Colours (picked at random, never the same twice in a row)")]
    [SerializeField] private Color[] palette =
    {
        new Color(0.90f, 0.08f, 0.14f), // P5 red
        new Color(1.00f, 0.84f, 0.00f), // yellow
        new Color(0.00f, 0.75f, 0.95f), // cyan
        new Color(1.00f, 0.25f, 0.65f), // hot pink
        new Color(0.45f, 0.90f, 0.20f), // lime
        new Color(1.00f, 0.50f, 0.05f), // orange
        new Color(0.55f, 0.30f, 0.95f), // purple
    };

    [Header("Strip slide-in")]
    [SerializeField] private float slideDistance = 2600f;
    [SerializeField] private float slideTime = 0.28f;
    [SerializeField] private float bottomStripDelay = 0.08f;

    [Header("Frame pop")]
    [SerializeField] private float popScale = 1.12f;
    [SerializeField] private float popTime = 0.18f;
    [SerializeField] private float maxTilt = 2.5f;

    [Header("Impact (consequence frame)")]
    [SerializeField] private float shakeStrength = 22f;
    [SerializeField] private float shakeTime = 0.35f;
    [SerializeField] private float flashAlpha = 0.75f;
    [SerializeField] private float flashTime = 0.25f;

    private Vector2 topRest, bottomRest, mainRest;
    private int lastTop = -1, lastBottom = -1;
    private Coroutine topRoutine, bottomRoutine, popRoutine, shakeRoutine, flashRoutine;

    private void Awake()
    {
        if (topStrip != null) topRest = topStrip.anchoredPosition;
        if (bottomStrip != null) bottomRest = bottomStrip.anchoredPosition;
        if (mainPanel != null) mainRest = mainPanel.anchoredPosition;
        if (flash != null) SetAlpha(flash, 0f);
    }

    private void OnEnable()
    {
        if (player == null) player = FindAnyObjectByType<CutscenePlayer>();
        if (player != null) player.FrameShown += OnFrameShown;
        else Debug.LogWarning("ComicCutsceneStyle: no CutscenePlayer found.", this);
    }

    private void OnDisable()
    {
        if (player != null) player.FrameShown -= OnFrameShown;
    }

    private void OnFrameShown(Vignette v, int index)
    {
        // New event: fresh colours + strips slam in
        if (index == 0)
        {
            Recolour(topStripColour, ref lastTop, lastBottom);
            Recolour(bottomStripColour, ref lastBottom, lastTop);
            Restart(ref topRoutine, Slide(topStrip, topRest, -1f, 0f));
            Restart(ref bottomRoutine, Slide(bottomStrip, bottomRest, +1f, bottomStripDelay));
        }

        // Every frame: pop + small random tilt
        Restart(ref popRoutine, Pop());

        // Consequence frame: shake + flash
        int impact = (v.frames == null || v.frames.Length == 0) ? 0 : Mathf.Clamp(v.impactFrame, 0, v.frames.Length - 1);
        if (index == impact && index > 0)
        {
            Restart(ref shakeRoutine, Shake());
            if (flash != null) Restart(ref flashRoutine, Flash());
        }
    }

    // ---------- Effects ----------

    private IEnumerator Slide(RectTransform strip, Vector2 rest, float fromSide, float delay)
    {
        if (strip == null) yield break;
        strip.anchoredPosition = rest + new Vector2(fromSide * slideDistance, 0f);
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < slideTime)
        {
            t += Time.deltaTime;
            float k = EaseOutBack(Mathf.Clamp01(t / slideTime));
            strip.anchoredPosition = Vector2.LerpUnclamped(rest + new Vector2(fromSide * slideDistance, 0f), rest, k);
            yield return null;
        }
        strip.anchoredPosition = rest;
    }

    private IEnumerator Pop()
    {
        if (mainPanel == null) yield break;
        float tilt = Random.Range(-maxTilt, maxTilt);
        float t = 0f;
        while (t < popTime)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / popTime), 3f);   // ease-out cubic
            mainPanel.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, k);
            mainPanel.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(tilt * 2.5f, tilt, k));
            yield return null;
        }
        mainPanel.localScale = Vector3.one;
        mainPanel.localEulerAngles = new Vector3(0f, 0f, tilt);
    }

    private IEnumerator Shake()
    {
        if (mainPanel == null) yield break;
        float t = 0f;
        while (t < shakeTime)
        {
            t += Time.deltaTime;
            float strength = shakeStrength * (1f - t / shakeTime);
            mainPanel.anchoredPosition = mainRest + Random.insideUnitCircle * strength;
            yield return null;
        }
        mainPanel.anchoredPosition = mainRest;
    }

    private IEnumerator Flash()
    {
        SetAlpha(flash, flashAlpha);
        float t = 0f;
        while (t < flashTime)
        {
            t += Time.deltaTime;
            SetAlpha(flash, Mathf.Lerp(flashAlpha, 0f, t / flashTime));
            yield return null;
        }
        SetAlpha(flash, 0f);
    }

    // ---------- Helpers ----------

    private void Recolour(Image[] images, ref int last, int avoid)
    {
        if (images == null || images.Length == 0 || palette == null || palette.Length == 0) return;

        int pick = Random.Range(0, palette.Length);
        for (int tries = 0; tries < 10 && palette.Length > 2 && (pick == last || pick == avoid); tries++)
            pick = Random.Range(0, palette.Length);
        last = pick;

        // First image gets the main colour; any extra images (accent stripes) get a different one
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null) continue;
            Color c = palette[(pick + i * 3) % palette.Length];
            c.a = images[i].color.a;
            images[i].color = c;
        }
    }

    private void Restart(ref Coroutine handle, IEnumerator routine)
    {
        if (handle != null) StopCoroutine(handle);
        handle = isActiveAndEnabled ? StartCoroutine(routine) : null;
    }

    private static void SetAlpha(Image img, float a)
    {
        Color c = img.color; c.a = a; img.color = c;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
