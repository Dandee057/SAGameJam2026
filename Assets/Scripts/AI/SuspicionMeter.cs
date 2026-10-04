using System;
using UnityEngine;
using UnityEngine.UI;

/// The "waiters are watching you" meter. One per scene.
/// Waiters that can see the player call ReportSeen() every frame; while at least one does, the meter fills.
/// When nobody sees the player it drains. At full, 'Full' fires (GameFlow throws the player out).
///
/// UI: an Image with Image Type = Filled for 'fill', and 'root' = the object holding the bar, which is only
/// shown while the meter is above zero. 'root' must be a different object from the one with this script.
public class SuspicionMeter : MonoBehaviour
{
    public static SuspicionMeter Instance { get; private set; }

    [Header("UI")] public Image fill;
    public GameObject root;
    public Color lowColour = new(1f, 0.9f, 0.3f);
    public Color highColour = new(0.95f, 0.15f, 0.15f);

    [Header("Tuning")] [Tooltip("Fraction of the bar that drains per second while nobody can see you.")]
    public float decayPerSecond = 0.35f;

    public float Value { get; private set; }

    /// Fired once when the meter reaches full.
    public event Action Full;

    private bool reported;
    private float reportedRate;
    private bool fired;

    private void Awake()
    {
        Instance = this;
        Refresh();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// Call every frame while the player is being seen. 'ratePerSecond' is how fast it fills (1 = a full bar per second).
    /// Several waiters at once don't stack: the highest rate wins.
    public void ReportSeen(float ratePerSecond)
    {
        reported = true;
        reportedRate = Mathf.Max(reportedRate, ratePerSecond);
    }

    // LateUpdate so every waiter has reported this frame before we apply it.
    private void LateUpdate()
    {
        if (reported)
        {
            Value += reportedRate * Time.deltaTime;
        }
        else
        {
            Value -= decayPerSecond * Time.deltaTime;
        }

        reported = false;
        reportedRate = 0f;
        Value = Mathf.Clamp01(Value);

        Refresh();

        if (Value >= 1f && !fired)
        {
            fired = true;
            Full?.Invoke();
        }
    }

    /// Called by GameFlow when the round is over.
    public void Hide()
    {
        enabled = false;
        if (root != null && root != gameObject)
        {
            root.SetActive(false);
        }
    }

    private void Refresh()
    {
        if (fill != null)
        {
            fill.fillAmount = Value;
            fill.color = Color.Lerp(lowColour, highColour, Value);
        }

        if (root != null && root != gameObject)
        {
            root.SetActive(Value > 0.01f);
        }
    }
}