using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GamePhase
{
    Sabotage,
    Cutscene,
    Finished
}

/// Runs the round: 60s of sabotage -> lock the player -> if hidden, play the sabotage cutscene; if not, the caught cutscene
/// -> win/lose screen.
public class GameFlow : MonoBehaviour
{
    public float duration = 60f;

    [Header("References")] public PlayerController player;
    public PlayerInteractor interactor;
    public CutscenePlayer cutscene;
    public ResultScreen resultScreen;
    public TMP_Text timerText; // legacy UI Text; swap for TMP_Text if you use TextMeshPro

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
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(Mathf.Max(Remaining, 0f)).ToString();
        }

        if (Remaining <= 0f)
        {
            StartCoroutine(EndSabotage());
        }
    }

    private IEnumerator EndSabotage()
    {
        Phase = GamePhase.Cutscene;

        // kill any open minigame first (it unlocks the player when it closes), then lock everything
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.CancelCurrent();
        }

        player.inputLocked = true;
        interactor.enabled = false;
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        // checked exactly once, the moment the timer hits zero
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