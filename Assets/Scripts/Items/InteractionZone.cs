using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class InteractionZone : MonoBehaviour
{
    public static readonly List<InteractionZone> All = new();

    [Header("Requirements")] public string requiredItemId = "water";
    public bool singleUse = true;

    [Header("Detection")]
    [Tooltip("How close (on the floor, in world units) the player must be to this zone's centre.")]
    public float reach = 1.5f;

    [Tooltip("Max height difference, so you can't trigger it from a balcony above.")]
    public float maxHeightDifference = 1.5f;

    [Header("Thought bubble")] public Sprite thoughtSprite;
    public GameObject thoughtPrefab;

    [Header("Minigame (optional)")]
    [Tooltip(
        "Leave empty for an instant interaction. Otherwise this UI prefab opens and the effect only happens on success.")]
    public Minigame minigamePrefab;

    [Header("What happens")] [Tooltip("The cutscene clip this sabotage adds to the end-of-round playback.")]
    public Vignette vignette;

    public bool consumeItem = true;
    public GameObject spawnPrefab;
    public Transform spawnPoint;
    public UnityEvent<PlayerInteractor> onActivated;
    public UnityEvent<PlayerInteractor> onFailed;

    [Header("Editor gizmo")] public Color gizmoColor = new(0.2f, 0.9f, 1f, 1f);

    public bool Used { get; private set; }
    public bool InProgress { get; private set; }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public bool CanUse(MainItem held)
    {
        if (held == null)
        {
            return false;
        }

        if (singleUse && Used)
        {
            return false;
        }

        if (InProgress)
        {
            return false;
        }

        return held.itemId == requiredItemId;
    }

    public bool IsInRange(Vector3 playerPos)
    {
        Vector3 d = playerPos - transform.position;
        if (Mathf.Abs(d.y) > maxHeightDifference)
        {
            return false;
        }

        d.y = 0f;
        return d.sqrMagnitude <= reach * reach;
    }

    public void Interact(MainItem held, PlayerInteractor user, Action<bool> finished)
    {
        if (minigamePrefab == null)
        {
            finished?.Invoke(Complete(user));
            return;
        }

        if (MinigameManager.Instance == null)
        {
            Debug.LogWarning(
                $"{name}: has a minigame but there's no MinigameManager in the scene. Skipping the minigame.", this);
            finished?.Invoke(Complete(user));
            return;
        }

        InProgress = true;
        MinigameContext ctx = new() { player = user, item = held, zone = this };
        MinigameManager.Instance.Run(minigamePrefab, ctx, success =>
        {
            InProgress = false;
            if (success)
            {
                finished?.Invoke(Complete(user));
            }
            else
            {
                onFailed?.Invoke(user);
                finished?.Invoke(false);
            }
        });
    }

    private bool Complete(PlayerInteractor user)
    {
        Used = true;
        RunLog.Add(vignette, Time.time);

        if (spawnPrefab != null)
        {
            Transform p = spawnPoint != null ? spawnPoint : transform;
            Instantiate(spawnPrefab, p.position, p.rotation);
        }

        onActivated?.Invoke(user);
        return consumeItem;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Color c = Used ? new Color(0.5f, 0.5f, 0.5f, 1f) : gizmoColor;
        Vector3 pos = transform.position;

        Handles.color = new Color(c.r, c.g, c.b, 0.12f);
        Handles.DrawSolidDisc(pos, Vector3.up, reach);
        Handles.color = c;
        Handles.DrawWireDisc(pos, Vector3.up, reach);

        Gizmos.color = c;
        Gizmos.DrawWireSphere(pos, 0.1f);
        Gizmos.color = new Color(c.r, c.g, c.b, 0.3f);
        Gizmos.DrawLine(pos + Vector3.down * maxHeightDifference, pos + Vector3.up * maxHeightDifference);

        if (spawnPrefab != null)
        {
            Transform p = spawnPoint != null ? spawnPoint : transform;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, p.position);
            Gizmos.DrawWireCube(p.position, Vector3.one * 0.15f);
        }

        Handles.Label(pos + Vector3.up * 0.6f, $"Zone: needs \"{requiredItemId}\"" + (Used ? " (used)" : ""));
    }
#endif
}