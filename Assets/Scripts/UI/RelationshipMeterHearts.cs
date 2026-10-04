using UnityEngine;

public class RelationshipMeterHearts : MonoBehaviour
{
    public RelationshipMeter meter;

    [Tooltip("Heart at the 'bad for the couple' end (low value).")]
    public RectTransform brokenHeart;

    [Tooltip("Heart at the 'good for the couple' end (high value).")]
    public RectTransform flourishHeart;

    [Header("Size by closeness")] public float farScale = 0.85f;
    public float nearScale = 1.2f;

    [Header("Punch on change")] public float punchScale = 0.35f;
    public float punchTime = 0.35f;
    public float shakeAngle = 14f;

    [Header("Idle bob")] public float bobAmount = 0.04f;
    public float bobSpeed = 3f;

    private float lastValue;
    private float brokenPunch, flourishPunch;
    private Vector3 brokenBase = Vector3.one, flourishBase = Vector3.one;

    private void Start()
    {
        if (meter == null)
        {
            meter = FindAnyObjectByType<RelationshipMeter>();
        }

        if (brokenHeart)
        {
            brokenBase = brokenHeart.localScale;
        }

        if (flourishHeart)
        {
            flourishBase = flourishHeart.localScale;
        }

        lastValue = meter ? meter.Value : 50f;
    }

    private void Update()
    {
        if (meter == null)
        {
            return;
        }

        float dt = Time.deltaTime;

        float v = meter.Value;
        if (!Mathf.Approximately(v, lastValue))
        {
            if (v < lastValue)
            {
                brokenPunch = 1f;
            }
            else
            {
                flourishPunch = 1f;
            }

            lastValue = v;
        }

        float t = Mathf.InverseLerp(meter.min, meter.max, v);
        brokenPunch = Mathf.MoveTowards(brokenPunch, 0f, dt / Mathf.Max(0.01f, punchTime));
        flourishPunch = Mathf.MoveTowards(flourishPunch, 0f, dt / Mathf.Max(0.01f, punchTime));

        Apply(brokenHeart, brokenBase, Mathf.Lerp(nearScale, farScale, t), brokenPunch, 0f);
        Apply(flourishHeart, flourishBase, Mathf.Lerp(farScale, nearScale, t), flourishPunch, 1.7f);
    }

    private void Apply(RectTransform heart, Vector3 baseScale, float size, float punch, float bobPhase)
    {
        if (heart == null)
        {
            return;
        }

        float bob = 1f + Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobAmount;
        float p = 1f + punch * punchScale;
        heart.localScale = baseScale * (size * bob * p);
        heart.localRotation =
            Quaternion.Euler(0f, 0f, punch > 0f ? Mathf.Sin(Time.time * 60f) * shakeAngle * punch : 0f);
    }
}