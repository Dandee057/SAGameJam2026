using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// Marks a place a rat can appear from (a hole in the wall, behind a plant...).
/// Drop one or more in the scene, on the NavMesh. The cheese picks the nearest one.
public class RatSpawnPoint : MonoBehaviour
{
    public static readonly List<RatSpawnPoint> All = new();

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public static RatSpawnPoint Nearest(Vector3 from)
    {
        RatSpawnPoint best = null;
        float bestDist = float.MaxValue;

        foreach (RatSpawnPoint p in All)
        {
            float d = (p.transform.position - from).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }

        return best;
    }

    public static RatSpawnPoint PickRandom()
    {
        return All.Count == 0 ? null : All[Random.Range(0, All.Count)];
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.8f, 0.5f, 0.3f, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Handles.Label(transform.position + Vector3.up * 0.5f, "Rat spawn");
    }
#endif
}
