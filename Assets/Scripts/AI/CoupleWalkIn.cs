using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoupleWalkIn : MonoBehaviour
{
    [System.Serializable]
    public class Walker
    {
        [Tooltip("Character prefab: sprite (+ BillboardSprite, + SpriteFlipbook for a walk animation).")]
        public GameObject prefab;

        [Tooltip("First = spawn point, last = where they stop. Put them on the floor, in order.")]
        public Transform[] path;

        [Tooltip("Tick if this character's art faces RIGHT by default.")]
        public bool artFacesRight = true;
    }

    public Walker first;
    public Walker second;

    public float walkDuration = 5f;

    [Tooltip("A short beat after they stop, before the cutscene starts.")]
    public float arrivalPause = 0.5f;

    private class Runner
    {
        public Transform transform;
        public SpriteFlipbook flipbook;
        public SpriteRenderer sprite;
        public Vector3[] points;
        public float[] cumulative;
        public float total;
        public bool artFacesRight;
    }

    public IEnumerator Play()
    {
        List<Runner> runners = new();
        TryAdd(runners, first, "first");
        TryAdd(runners, second, "second");

        if (runners.Count == 0)
        {
            yield break;
        }

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        float duration = Mathf.Max(0.1f, walkDuration);
        float t = 0f;

        foreach (Runner r in runners)
        {
            if (r.flipbook != null)
            {
                r.flipbook.Moving = true;
            }
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            foreach (Runner r in runners)
            {
                Vector3 before = r.transform.position;
                Vector3 after = PositionAt(r, r.total * k);
                r.transform.position = after;
                Face(r, after - before, cam);
            }

            yield return null;
        }

        foreach (Runner r in runners)
        {
            r.transform.position = r.points[r.points.Length - 1];
            if (r.flipbook != null)
            {
                r.flipbook.Moving = false;
            }
        }

        yield return new WaitForSeconds(arrivalPause);
    }

    private void TryAdd(List<Runner> runners, Walker w, string label)
    {
        if (w == null || w.prefab == null)
        {
            Debug.LogWarning($"CoupleWalkIn: the {label} walker has no prefab, skipping.", this);
            return;
        }

        if (w.path == null || w.path.Length < 2)
        {
            Debug.LogWarning($"CoupleWalkIn: the {label} walker needs at least 2 path points (spawn + end), skipping.",
                this);
            return;
        }

        Vector3[] pts = new Vector3[w.path.Length];
        float[] cum = new float[w.path.Length];
        for (int i = 0; i < w.path.Length; i++)
        {
            pts[i] = w.path[i].position;
            if (i > 0)
            {
                cum[i] = cum[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
            }
        }

        GameObject go = Instantiate(w.prefab, pts[0], Quaternion.identity);
        runners.Add(new Runner
        {
            transform = go.transform,
            flipbook = go.GetComponentInChildren<SpriteFlipbook>(),
            sprite = go.GetComponentInChildren<SpriteRenderer>(),
            points = pts,
            cumulative = cum,
            total = cum[cum.Length - 1],
            artFacesRight = w.artFacesRight
        });
    }

    private static Vector3 PositionAt(Runner r, float distance)
    {
        for (int i = 1; i < r.points.Length; i++)
        {
            if (distance <= r.cumulative[i])
            {
                float segment = r.cumulative[i] - r.cumulative[i - 1];
                float u = segment > 0.0001f ? (distance - r.cumulative[i - 1]) / segment : 1f;
                return Vector3.Lerp(r.points[i - 1], r.points[i], u);
            }
        }

        return r.points[r.points.Length - 1];
    }

    private static void Face(Runner r, Vector3 moved, Transform cam)
    {
        if (r.sprite == null || cam == null || moved.sqrMagnitude < 1e-8f)
        {
            return;
        }

        float screenX = Vector3.Dot(moved.normalized, cam.right);
        if (Mathf.Abs(screenX) > 0.1f)
        {
            bool goingLeft = screenX < 0f;
            r.sprite.flipX = r.artFacesRight ? goingLeft : !goingLeft;
        }
    }

    private void OnDrawGizmos()
    {
        DrawPath(first, new Color(1f, 0.6f, 0.2f));
        DrawPath(second, new Color(0.3f, 0.8f, 1f));
    }

    private static void DrawPath(Walker w, Color c)
    {
        if (w == null || w.path == null)
        {
            return;
        }

        Gizmos.color = c;
        for (int i = 0; i < w.path.Length; i++)
        {
            if (w.path[i] == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(w.path[i].position, i == 0 ? 0.3f : 0.15f);
            if (i > 0 && w.path[i - 1] != null)
            {
                Gizmos.DrawLine(w.path[i - 1].position, w.path[i].position);
            }
        }
    }
}