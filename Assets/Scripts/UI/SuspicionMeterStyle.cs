using UnityEngine;
using UnityEngine.UI;

public class SuspicionMeterStyle : MonoBehaviour
{
    [Tooltip("Leave empty to use SuspicionMeter.Instance.")]
    public SuspicionMeter meter;

    [Header("Eye")] public Image eye;
    public Sprite eyeClosed;
    public Sprite eyeHalf;
    public Sprite eyeOpen;
    [Range(0f, 1f)] public float halfAt = 0.3f;
    [Range(0f, 1f)] public float openAt = 0.65f;
    public float pulseAmount = 0.08f;

    [Header("Bar shake")] [Tooltip("The object that trembles (usually the bar's parent).")]
    public RectTransform shakeTarget;

    [Range(0f, 1f)] public float shakeFrom = 0.7f;
    public float shakeStrength = 6f;

    [Header("Exclaim burst")] public RectTransform exclaim;
    public float exclaimTime = 0.5f;

    private Vector2 shakeBase;
    private Vector3 eyeBase = Vector3.one;
    private float exclaimTimer, pulseClock, last;

    private void Start()
    {
        if (meter == null)
        {
            meter = SuspicionMeter.Instance;
        }

        if (shakeTarget)
        {
            shakeBase = shakeTarget.anchoredPosition;
        }

        if (eye)
        {
            eyeBase = eye.rectTransform.localScale;
        }

        if (exclaim)
        {
            exclaim.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (meter == null)
        {
            meter = SuspicionMeter.Instance;
            if (meter == null)
            {
                return;
            }
        }

        float v = meter.Value, dt = Time.deltaTime;


        if (eye)
        {
            Sprite s = v >= openAt ? eyeOpen : v >= halfAt ? eyeHalf : eyeClosed;
            if (s && eye.sprite != s)
            {
                eye.sprite = s;
            }

            pulseClock += dt * Mathf.Lerp(3f, 14f, v);
            eye.rectTransform.localScale =
                eyeBase * (1f + Mathf.Sin(pulseClock) * pulseAmount * Mathf.Lerp(0.3f, 1.5f, v));
        }


        if (shakeTarget)
        {
            float k = Mathf.InverseLerp(shakeFrom, 1f, v);
            shakeTarget.anchoredPosition =
                shakeBase + (k > 0f ? Random.insideUnitCircle * shakeStrength * k : Vector2.zero);
        }


        if ((last < openAt && v >= openAt) || (last < 1f && v >= 1f))
        {
            PopExclaim();
        }

        last = v;

        if (exclaim && exclaimTimer > 0f)
        {
            exclaimTimer -= dt;
            float t = 1f - exclaimTimer / exclaimTime;
            float s = t < 0.25f ? Mathf.Lerp(0.3f, 1.25f, t / 0.25f) : Mathf.Lerp(1.25f, 1f, (t - 0.25f) / 0.75f);
            exclaim.localScale = Vector3.one * s;
            exclaim.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 40f) * 12f * (1f - t));
            if (exclaimTimer <= 0f)
            {
                exclaim.gameObject.SetActive(false);
            }
        }
    }

    private void PopExclaim()
    {
        if (exclaim == null)
        {
            return;
        }

        exclaim.gameObject.SetActive(true);
        exclaimTimer = exclaimTime;
    }
}