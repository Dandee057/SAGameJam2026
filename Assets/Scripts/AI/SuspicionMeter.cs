using System;
using UnityEngine;
using UnityEngine.UI;

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

    public void ReportSeen(float ratePerSecond)
    {
        reported = true;
        reportedRate = Mathf.Max(reportedRate, ratePerSecond);
    }

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