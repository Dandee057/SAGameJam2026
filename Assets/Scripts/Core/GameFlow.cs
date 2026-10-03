using System.Collections;
using UnityEngine;

public enum GamePhase
{
    Sabotage,
    Cutscene,
    Finished
}

public class GameFlow : MonoBehaviour
{
    public float duration = 60f;

    [Header("References")] public PlayerController player;
    public PlayerInteractor interactor;
    public CutscenePlayer cutscene;
    public ResultScreen resultScreen;

    [Header("Debug (until the real hiding mechanic exists)")]
    public bool allowDebugKeys = true;

    public KeyCode toggleHiddenKey = KeyCode.H;

    public GamePhase Phase { get; private set; } = GamePhase.Sabotage;
    public float Remaining { get; private set; }
    public bool WasCaught { get; private set; }

    private void Start()
    {
        RunLog.Clear();
        Remaining = duration;

        if (player == null)
        {
            Debug.LogError("GameFlow: 'player' isn't assigned.", this);
        }

        if (interactor == null)
        {
            Debug.LogError("GameFlow: 'interactor' isn't assigned.", this);
        }

        if (cutscene == null)
        {
            Debug.LogError("GameFlow: 'cutscene' isn't assigned.", this);
        }

        if (resultScreen == null)
        {
            Debug.LogError("GameFlow: 'resultScreen' isn't assigned - no win/lose screen will show!", this);
        }
    }

    private void Update()
    {
        if (Phase != GamePhase.Sabotage)
        {
            return;
        }

        if (allowDebugKeys && Input.GetKeyDown(toggleHiddenKey))
        {
            player.IsHidden = !player.IsHidden;
            Debug.Log($"[debug] IsHidden = {player.IsHidden}");
        }

        Remaining -= Time.deltaTime;

        if (Remaining <= 0f)
        {
            StartCoroutine(EndSabotage());
        }
    }

    private IEnumerator EndSabotage()
    {
        Phase = GamePhase.Cutscene;

        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.CancelCurrent();
        }

        player.inputLocked = true;
        interactor.enabled = false;
        foreach (WaiterWander w in WaiterWander.All)
        {
            w.Freeze();
        }

        WasCaught = !player.IsHidden;

        if (WasCaught)
        {
            yield return cutscene.PlayCaught();
        }
        else
        {
            yield return cutscene.Play(RunLog.Entries);
        }

        Phase = GamePhase.Finished;
        if (resultScreen != null)
        {
            resultScreen.Show(cutscene.Won);
        }
    }
}