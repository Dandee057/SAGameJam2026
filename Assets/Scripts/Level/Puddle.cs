using UnityEngine;


public class Puddle : MonoBehaviour
{
    [Header("Floor")]
    [Tooltip("On = look straight down for the floor and sit on it. Off = stay exactly at the spawn point's height.")]
    public bool snapToFloor = true;

    [Tooltip("How far above the floor the puddle sits (stops it flickering into the floor).")]
    public float heightAboveFloor = 0.02f;

    [Tooltip("How far up/down from the spawn point to search for the floor.")]
    public float searchDistance = 3f;

    [Tooltip("Which layers count as floor. 'Everything' is fine unless it lands on tables.")]
    public LayerMask floorLayers = ~0;

    [Tooltip("Extra nudge up/down after snapping, if it still looks slightly off.")]
    public float extraHeightOffset = 0f;

    [Header("Appear")] [Tooltip("Seconds to grow from nothing to full size. 0 = appear instantly.")]
    public float appearTime = 0.3f;

    private Vector3 fullScale;
    private float t;

    private void Start()
    {
        Vector3 p = transform.position;

        if (snapToFloor)
        {
            Vector3 origin = p + Vector3.up * searchDistance;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, searchDistance * 2f,
                floorLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length > 0)
            {
                float lowest = float.MaxValue;
                foreach (RaycastHit h in hits)
                {
                    if (h.point.y < lowest)
                    {
                        lowest = h.point.y;
                    }
                }

                p.y = lowest + heightAboveFloor;
            }
            else
            {
                Debug.LogWarning("[Puddle] No floor collider found below the spawn point - keeping spawn height. " +
                                 "Untick 'Snap To Floor' and move the zone's SpawnPoint onto the floor instead.", this);
            }
        }

        p.y += extraHeightOffset;
        transform.position = p;

        fullScale = transform.localScale;
        if (appearTime > 0f)
        {
            transform.localScale = Vector3.zero;
        }
        else
        {
            enabled = false;
        }
    }

    private void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / appearTime);
        float ease = 1f - Mathf.Pow(1f - k, 3f);
        transform.localScale = fullScale * ease;
        if (k >= 1f)
        {
            enabled = false;
        }
    }
}