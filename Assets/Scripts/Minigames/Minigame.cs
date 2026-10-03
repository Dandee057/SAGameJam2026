using System;
using UnityEngine;

public class MinigameContext
{
    public PlayerInteractor player;
    public MainItem item;
    public InteractionZone zone;
}

public abstract class Minigame : MonoBehaviour
{
    [Tooltip("Can the player back out with the cancel key (Esc)?")]
    public bool canCancel = true;

    public Action<bool> onFinished;

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

    protected void Finish(bool success)
    {
        if (finished)
        {
            return;
        }

        finished = true;
        onFinished?.Invoke(success);
    }
}