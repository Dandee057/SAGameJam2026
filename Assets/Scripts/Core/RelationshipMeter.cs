using UnityEngine;

public class RelationshipMeter : MonoBehaviour
{
    public float startValue = 50f;
    public float min = 0f;
    public float max = 100f;

    [Header("Bar")] public RectTransform track;
    public RectTransform marker;
    public float edgePadding = 0f;
    public bool greenOnRight = true;

    [Header("Spring feel")] public float stiffness = 80f;
    public float damping = 8f;

    public float Value { get; private set; }

    private float pos, vel;

    private void Awake()
    {
        ResetMeter();
    }

    public void ResetMeter()
    {
        Value = startValue;
        pos = TargetPos();
        vel = 0f;
        ApplyMarker();
    }

    public void SetVisible(bool visible)
    {
        if (track != null)
        {
            track.gameObject.SetActive(visible);
        }

        if (marker != null)
        {
            marker.gameObject.SetActive(visible);
        }
    }

    public void Add(float delta)
    {
        Value = Mathf.Clamp(Value + delta, min, max);
    }

    private float TargetPos()
    {
        return Mathf.InverseLerp(min, max, Value);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        vel += (TargetPos() - pos) * stiffness * dt;
        vel *= Mathf.Exp(-damping * dt);
        pos += vel * dt;
        ApplyMarker();
    }

    private void ApplyMarker()
    {
        if (marker == null || track == null)
        {
            return;
        }

        float left = track.rect.xMin + edgePadding;
        float right = track.rect.xMax - edgePadding;
        float t = greenOnRight ? pos : 1f - pos;

        Vector2 p = marker.anchoredPosition;
        p.x = Mathf.LerpUnclamped(left, right, t);
        marker.anchoredPosition = p;
    }
}