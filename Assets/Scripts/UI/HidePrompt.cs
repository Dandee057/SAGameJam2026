using TMPro;
using UnityEngine;

/// A "PRESS H TO HIDE!" prompt by the clock. Pops in when time is running low, shakes so the player
/// notices, and disappears while the player is hidden (comes back if they un-hide).
///
/// SETUP: make a TextMeshPro text next to the clock (e.g. Canvas > Clock > HidePrompt),
/// add this script to it. Everything else is automatic.
[RequireComponent(typeof(RectTransform))]
public class HidePrompt : MonoBehaviour
{
    [Tooltip("Optional - auto-found on this object. The message is written into it.")]
    public TMP_Text label;
    public string message = "PRESS H TO HIDE!";

    [Header("When")]
    [Tooltip("Shows when this many seconds (or fewer) are left. Set it to the round length (60) to show all round.")]
    public float showAtSecondsLeft = 10f;
    [Tooltip("Hide the prompt while the player is hidden.")]
    public bool hideWhilePlayerHidden = true;

    [Header("Shake")]
    [Tooltip("How far it jitters (UI units).")]
    public float shakeAmount = 6f;
    [Tooltip("Wobble rotation (degrees).")]
    public float wobbleAngle = 6f;
    public float shakeSpeed = 30f;
    [Tooltip("Shake gets this many times stronger in the last seconds.")]
    public float urgencyBoost = 2f;

    [Header("Pulse")]
    public float pulseAmount = 0.12f;
    public float pulseSpeed = 6f;
    [Tooltip("Flashes between its normal colour and this.")]
    public Color flashColour = new Color(1f, 0.2f, 0.2f);

    [Header("Pop in/out")]
    public float fadeSpeed = 8f;

    private GameFlow flow;
    private RectTransform rt;
    private CanvasGroup group;
    private Vector2 restPos;
    private Vector3 restScale;
    private Quaternion restRot;
    private Color baseColour = Color.white;
    private float visible; // 0..1

    private void Awake()
    {
        rt = (RectTransform)transform;
        restPos = rt.anchoredPosition;
        restScale = rt.localScale;
        restRot = rt.localRotation;

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;

        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            if (!string.IsNullOrEmpty(message)) label.text = message;
            baseColour = label.color;
        }
    }

    private void Start()
    {
        flow = FindFirstObjectByType<GameFlow>();
    }

    private void Update()
    {
        bool show = false;
        float left = 999f;
        if (flow != null && flow.Phase == GamePhase.Sabotage)
        {
            left = flow.Remaining;
            show = left <= showAtSecondsLeft;
            if (hideWhilePlayerHidden && flow.player != null && flow.player.IsHidden) show = false;
        }
        if (PauseMenu.IsPaused) show = false;

        visible = Mathf.MoveTowards(visible, show ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
        group.alpha = visible;

        if (visible <= 0f)
        {
            rt.anchoredPosition = restPos;
            rt.localRotation = restRot;
            rt.localScale = restScale;
            return;
        }

        // stronger as time runs out
        float urgency = 1f + urgencyBoost * (1f - Mathf.Clamp01(left / Mathf.Max(0.01f, showAtSecondsLeft)));
        float t = Time.unscaledTime;

        Vector2 jitter = new Vector2(
            (Mathf.PerlinNoise(t * shakeSpeed, 0.3f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(0.7f, t * shakeSpeed) - 0.5f) * 2f) * shakeAmount * urgency;
        rt.anchoredPosition = restPos + jitter;
        rt.localRotation = restRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * shakeSpeed * 0.5f) * wobbleAngle * urgency);

        // pop in from small, then pulse
        float pop = Mathf.SmoothStep(0.6f, 1f, visible);
        float pulse = 1f + pulseAmount * Mathf.Abs(Mathf.Sin(t * pulseSpeed));
        rt.localScale = restScale * pop * pulse;

        if (label != null)
            label.color = Color.Lerp(baseColour, flashColour, Mathf.Abs(Mathf.Sin(t * pulseSpeed)));
    }
}
