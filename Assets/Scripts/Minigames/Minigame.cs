using System;
using UnityEngine;

/// Passed to a minigame when it starts, so it knows what it's being played for.
public class MinigameContext
{
    public PlayerInteractor player;
    public MainItem item;
    public InteractionZone zone;
}

/// Base class for every zone minigame. Make a UI prefab (full-screen panel), put a subclass on its root,
/// and drag the prefab into a zone's 'minigamePrefab'.
///
/// To write a new minigame: override Begin() to set up, do your thing in Update(), and call Finish(true/false).
public abstract class Minigame : MonoBehaviour
{
    [Tooltip("Can the player back out with the cancel key (Esc)?")]
    public bool canCancel = true;

    /// Set by MinigameManager.
    public Action<bool, Vignette> onFinished;

    protected MinigameContext context;
    private bool finished;

    public void Begin(MinigameContext ctx)
    {
        context = ctx;
        OnBegin();
    }

    protected virtual void OnBegin()
    {
    }

    public void Cancel()
    {
        Finish(false);
    }

    /// Call when the minigame ends. success = true triggers the zone's effect.
    /// 'result' (optional) is the vignette this run earned - it replaces the zone's own vignette.
    /// Leave it null to use the zone's default.
    protected void Finish(bool success, Vignette result = null)
    {
        if (finished)
        {
            return;
        }

        finished = true;
        onFinished?.Invoke(success, result);
    }
}