using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Analog clock that shows GameFlow's countdown.
///  - A radial "pie" that empties as time runs out
///  - A hand that sweeps one full turn over the round
///  - Colour: green -> yellow (45s) -> orange (30s) -> red (15s)
///  - Pulses from 30s, faster and bigger in the last 15s
///  - Hides itself when the sabotage phase ends (cutscene starts)
///
/// Reads GameFlow.Remaining / GameFlow.duration, so there's only ONE timer in the game.
/// </summary>
public class ClockTimerUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to find the GameFlow in the scene automatically.")]
    [SerializeField] private GameFlow gameFlow;
    [Tooltip("Image with Image Type = Filled, Fill Method = Radial 360, Clockwise OFF.")]
    [SerializeField] private Image fill;
    [Tooltip("The clock hand. Its pivot must be at the bottom (0.5, 0).")]
    [SerializeField] private RectTransform hand;
    [Tooltip("What scales when pulsing. Usually the clock's root object (this one).")]
    [SerializeField] private RectTransform pulseTarget;
    [Tooltip("Optional number in the middle. Leave empty if GameFlow's Timer Text already points at it.")]
    [SerializeField] private TMP_Text secondsText;

    [Header("Colours")]
    [SerializeField] private Color green  = new Color(0.30f, 0.85f, 0.35f);
    [SerializeField] private Color yellow = new Color(1.00f, 0.88f, 0.20f);
    [SerializeField] private Color orange = new Color(1.00f, 0.55f, 0.10f);
    [SerializeField] private Color red    = new Color(0.95f, 0.20f, 0.20f);

    [Header("Colour thresholds (seconds left)")]
    [SerializeField] private float yellowAt = 45f;
    [SerializeField] private float orangeAt = 30f;
    [SerializeField] private float redAt    = 15f;
    [Tooltip("Off = snaps to the next colour at each threshold. On = blends smoothly.")]
    [SerializeField] private bool smoothColours = false;

    [Header("Pulse")]
    [SerializeField] private float pulseStartsAt = 30f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseScale = 0.06f;     // 0.06 = grows 6%
    [Tooltip("In the red zone the pulse gets this much faster and bigger.")]
    [SerializeField] private float redPulseMultiplier = 1.8f;

    [Header("End of round")]
    [SerializeField] private bool hideWhenRoundEnds = true;

    private Vector3 baseScale = Vector3.one;
    private float pulseClock;

    private void Awake()
    {
        if (pulseTarget == null) pulseTarget = transform as RectTransform;
        if (pulseTarget != null) baseScale = pulseTarget.localScale;
    }

    private void Start()
    {
        if (gameFlow == null) gameFlow = FindAnyObjectByType<GameFlow>();
        if (gameFlow == null) Debug.LogWarning("ClockTimerUI: no GameFlow found in this scene.", this);
        Refresh();
    }

    private void Update() => Refresh();

    private void Refresh()
    {
        if (gameFlow == null) return;

        if (gameFlow.Phase != GamePhase.Sabotage)
        {
            if (hideWhenRoundEnds) gameObject.SetActive(false);
            return;
        }

        float duration = Mathf.Max(0.01f, gameFlow.duration);
        // Before GameFlow.Start runs, Remaining is still 0; treat that as a full clock.
        float t = gameFlow.Remaining > 0f || Time.timeSinceLevelLoad > 0.1f
            ? Mathf.Clamp(gameFlow.Remaining, 0f, duration)
            : duration;
        float n = t / duration;   // 1 = full, 0 = empty

        if (fill != null)
        {
            fill.fillAmount = n;
            fill.color = ColourFor(t, duration);
        }

        // 12 o'clock at full, sweeps clockwise one full turn to empty
        if (hand != null)
            hand.localEulerAngles = new Vector3(0f, 0f, -360f * (1f - n));

        if (secondsText != null)
            secondsText.text = Mathf.CeilToInt(t).ToString();

        UpdatePulse(t);
    }

    private Color ColourFor(float t, float duration)
    {
        if (!smoothColours)
        {
            if (t <= redAt) return red;
            if (t <= orangeAt) return orange;
            if (t <= yellowAt) return yellow;
            return green;
        }

        if (t > yellowAt) return Color.Lerp(yellow, green, Mathf.InverseLerp(yellowAt, duration, t));
        if (t > orangeAt) return Color.Lerp(orange, yellow, Mathf.InverseLerp(orangeAt, yellowAt, t));
        if (t > redAt)    return Color.Lerp(red, orange, Mathf.InverseLerp(redAt, orangeAt, t));
        return red;
    }

    private void UpdatePulse(float t)
    {
        if (pulseTarget == null) return;

        if (t > pulseStartsAt || t <= 0f)
        {
            pulseTarget.localScale = Vector3.Lerp(pulseTarget.localScale, baseScale, Time.unscaledDeltaTime * 10f);
            return;
        }

        float speed = pulseSpeed, amount = pulseScale;
        if (t <= redAt)
        {
            speed *= redPulseMultiplier;
            amount *= redPulseMultiplier;
        }

        // Scaled time, same as GameFlow's countdown, so both freeze together if the game is paused.
        pulseClock += Time.deltaTime * speed;
        float s = 1f + (Mathf.Sin(pulseClock) * 0.5f + 0.5f) * amount;
        pulseTarget.localScale = baseScale * s;
    }
}
