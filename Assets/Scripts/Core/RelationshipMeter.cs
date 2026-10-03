using UnityEngine;

/// Bar-style relationship meter. A marker slides along a bar: it starts in the middle, good outcomes push it
/// toward the green end, bad ones toward the red end. The marker is a spring, so it overshoots and settles.
///
/// UI setup (inside the cutscene panel so it only shows while the cutscene plays):
///   Bar     (Image: red on the left, green on the right)           -> 'track'
///     Marker (Image/needle, child of Bar, anchored to the CENTRE of the bar) -> 'marker'
///
/// The marker travels from the bar's left edge to its right edge. If you want it to stop short of the edges
/// (e.g. to keep the marker art inside the bar), use 'edgePadding'.
public class RelationshipMeter : MonoBehaviour
{
    public float startValue = 50f; // 50 of 0..100 = marker dead centre
    public float min = 0f;
    public float max = 100f;

    [Header("Bar")] public RectTransform track;
    public RectTransform marker;
    public float edgePadding = 0f; // UI units kept clear at each end
    public bool greenOnRight = true; // false = green on the left

    [Header("Spring feel")] public float stiffness = 80f;
    public float damping = 8f; // lower = more wobble

    public float Value { get; private set; }

    private float pos, vel; // 0 = red end, 1 = green end (can overshoot slightly past 0..1)

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

    /// Shows/hides the bar UI. Deliberately toggles the bar object, NOT this script's own GameObject
    /// (if this script shares an object with other scripts, deactivating it would stop them too).
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