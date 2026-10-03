using System;
using UnityEngine;

/// One per scene. Needs a screen-space Canvas to put minigame panels on.
/// Optional 'dimBackground': a full-screen semi-transparent Image that's the FIRST child of the canvas.
/// Note: the game does NOT pause during a minigame (so the date countdown keeps ticking = tension).
public class MinigameManager : MonoBehaviour
{
    public static MinigameManager Instance { get; private set; }
    public static bool IsRunning => Instance != null && Instance.current != null;

    public Canvas canvas;
    public GameObject dimBackground;
    public KeyCode cancelKey = KeyCode.Escape;

    private Minigame current;

    private void Awake()
    {
        Instance = this;
        if (dimBackground != null)
        {
            dimBackground.SetActive(false);
        }
    }

    public void Run(Minigame prefab, MinigameContext ctx, Action<bool, Vignette> done)
    {
        if (current != null)
        {
            return;
        }

        PlayerController pc = ctx.player.GetComponent<PlayerController>();
        pc.inputLocked = true;
        if (dimBackground != null)
        {
            dimBackground.SetActive(true);
        }

        current = Instantiate(prefab, canvas.transform, false);
        current.onFinished = (success, result) =>
        {
            Minigame mg = current;
            current = null;
            if (mg != null)
            {
                Destroy(mg.gameObject);
            }

            pc.inputLocked = false;
            if (dimBackground != null)
            {
                dimBackground.SetActive(false);
            }

            done?.Invoke(success, result);
        };
        current.Begin(ctx);
    }

    public void CancelCurrent()
    {
        if (current != null)
        {
            current.Cancel();
        }
    }

    private void Update()
    {
        if (current != null && current.canCancel && Input.GetKeyDown(cancelKey))
        {
            current.Cancel();
        }
    }
}