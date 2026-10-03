using UnityEngine;
using UnityEngine.UI;

public class PourMinigame : Minigame
{
    [Header("UI")] public RectTransform jug;
    public Image waterStream;
    public GameObject hint;

    [Header("Input")] public KeyCode mashKey = KeyCode.A;

    [Header("Tuning (degrees)")] public float kickPerPress = 12f;
    public float uprightPull = 50f;
    public float drag = 0.8f;
    public float maxAngle = 110f;
    public float pourAngle = 50f;
    public float fillRate = 0.35f;
    public float timeLimit = 0f;
    public float finishDelay = 0.4f;
    public float tiltDirection = 1f;

    private float angle, angularVel, progress, elapsed, finishTimer;
    private bool done;

    protected override void OnBegin()
    {
        if (hint)
        {
            hint.SetActive(true);
        }

        Apply(0f);
    }

    private void Update()
    {
        if (done)
        {
            finishTimer -= Time.deltaTime;
            if (finishTimer <= 0f)
            {
                Finish(true);
            }

            return;
        }

        float dt = Time.deltaTime;
        elapsed += dt;

        if (Input.GetKeyDown(mashKey))
        {
            angularVel += kickPerPress;
            if (hint)
            {
                hint.SetActive(false);
            }
        }


        angularVel -= uprightPull * dt;
        angularVel *= Mathf.Exp(-drag * dt);
        angle += angularVel * dt;

        if (angle <= 0f)
        {
            angle = 0f;
            if (angularVel < 0f)
            {
                angularVel = 0f;
            }
        }

        if (angle >= maxAngle)
        {
            angle = maxAngle;
            if (angularVel > 0f)
            {
                angularVel = 0f;
            }
        }

        // pouring
        float tilt = Mathf.InverseLerp(pourAngle, maxAngle, angle);
        progress = Mathf.Clamp01(progress + tilt * fillRate * dt);

        Apply(tilt);

        if (progress >= 1f)
        {
            done = true;
            finishTimer = finishDelay;
            Apply(0f);
        }
        else if (timeLimit > 0f && elapsed >= timeLimit)
        {
            Finish(false);
        }
    }

    private void Apply(float tilt)
    {
        if (jug)
        {
            jug.localRotation = Quaternion.Euler(0f, 0f, -angle * tiltDirection);
        }

        if (waterStream)
        {
            Color c = waterStream.color;
            c.a = tilt > 0f ? Mathf.Lerp(0.4f, 1f, tilt) : 0f;
            waterStream.color = c;
        }
    }
}