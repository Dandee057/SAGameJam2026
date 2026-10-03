using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// A spot in the world where you can do something.
///  - requiredItemId set   -> you must be HOLDING that item.
///  - requiredItemId empty -> no item needed, but your hands must be empty.
/// Setup: empty GameObject + this script. The zone owns its own reach.
public class InteractionZone : MonoBehaviour
{
    /// Every active zone, so the player can check them without physics.
    public static readonly List<InteractionZone> All = new();

    [Header("Requirements")] [Tooltip("Leave EMPTY for a zone that needs no item (hands must be empty).")]
    public string requiredItemId = "water";

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

    [Header("What happens")]
    [Tooltip(
        "The cutscene clip this adds. A minigame can override it with its own result (e.g. the pizza picks one of three).")]
    public Vignette vignette;

    public bool consumeItem = true;
    public GameObject spawnPrefab;
    public Transform spawnPoint;
    public UnityEvent<PlayerInteractor> onActivated; // on success
    public UnityEvent<PlayerInteractor> onFailed; // minigame failed / cancelled

    [Header("Editor gizmo")] public Color gizmoColor = new(0.2f, 0.9f, 1f, 1f);

    public bool Used { get; private set; }
    public bool InProgress { get; private set; }
    public bool NeedsItem => !string.IsNullOrEmpty(requiredItemId);

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
        if (singleUse && Used)
        {
            return false;
        }

        if (InProgress)
        {
            return false;
        }

        if (NeedsItem)
        {
            return held != null && held.itemId == requiredItemId;
        }

        return held == null; // item-less zone: hands must be empty
    }

    /// Flat (XZ) distance check using this zone's own reach.
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

    /// Start the interaction. 'held' may be null for item-less zones.
    /// 'finished' is called with true if the held item should be consumed.
    public void Interact(MainItem held, PlayerInteractor user, Action<bool> finished)
    {
        if (minigamePrefab == null)
        {
            finished?.Invoke(Complete(user, held, null));
            return;
        }

        if (MinigameManager.Instance == null)
        {
            Debug.LogWarning(
                $"{name}: has a minigame but there's no MinigameManager in the scene. Skipping the minigame.", this);
            finished?.Invoke(Complete(user, held, null));
            return;
        }

        InProgress = true;
        MinigameContext ctx = new() { player = user, item = held, zone = this };
        MinigameManager.Instance.Run(minigamePrefab, ctx, (success, result) =>
        {
            InProgress = false;
            if (success)
            {
                finished?.Invoke(Complete(user, held, result));
            }
            else
            {
                onFailed?.Invoke(user);
                finished?.Invoke(false);
            }
        });
    }

    /// The actual effect. Returns true if the held item should be consumed.
    private bool Complete(PlayerInteractor user, MainItem held, Vignette result)
    {
        Used = true;
        RunLog.Add(result != null ? result : vignette, Time.time);

        if (spawnPrefab != null)
        {
            Transform p = spawnPoint != null ? spawnPoint : transform;
            Instantiate(spawnPrefab, p.position, p.rotation);
        }

        onActivated?.Invoke(user);
        return consumeItem && held != null;
    }

#if UNITY_EDITOR
    // ---------- Scene view gizmos ----------
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

        string label = NeedsItem ? $"Zone: needs \"{requiredItemId}\"" : "Zone: no item needed";
        Handles.Label(pos + Vector3.up * 0.6f, label + (Used ? " (used)" : ""));
    }
#endif
}