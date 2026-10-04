using System.Collections;
using UnityEngine;

public enum GamePhase
{
    Intro,
    Sabotage,
    Arrival,
    Cutscene,
    Finished
}

public class GameFlow : MonoBehaviour
{
    public float duration = 60f;

    [Header("Scene setup")]
    [Tooltip("Plays before the timer starts. Sets up the scene; its relationship value is ignored. " +
             "Tip: set its Impact Frame to 0 so the comic style doesn't shake/flash on it. Leave empty to skip.")]
    public Vignette introVignette;

    [Header("After the timer")] [Tooltip("The couple walk in before the cutscene. Leave empty to skip.")]
    public CoupleWalkIn coupleWalkIn;

    [Tooltip("Also show the couple walking in when the player got caught.")]
    public bool walkInWhenCaught = true;

    [Header("Thrown out (suspicion meter full)")]
    [Tooltip("Plays when the suspicion meter fills, then the player loses. " +
             "Leave empty to play the 'Caught Vignettes' from the CutscenePlayer instead.")]
    public Vignette thrownOutVignette;

    [Header("References")] public PlayerController player;
    public PlayerInteractor interactor;
    public CutscenePlayer cutscene;
    public ResultScreen resultScreen;

    [Header("Debug (until the real hiding mechanic exists)")]
    public bool allowDebugKeys = true;

    public KeyCode toggleHiddenKey = KeyCode.H;

    public GamePhase Phase { get; private set; } = GamePhase.Intro;
    public float Remaining { get; private set; }
    public bool WasCaught { get; private set; }

    private IEnumerator Start()
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

        if (SuspicionMeter.Instance != null)
        {
            SuspicionMeter.Instance.Full += OnSuspicionFull;
        }

        Phase = GamePhase.Intro;
        if (introVignette != null && cutscene != null)
        {
            player.inputLocked = true;
            interactor.enabled = false;

            yield return cutscene.PlayIntro(introVignette);

            player.inputLocked = false;
            interactor.enabled = true;
        }

        Phase = GamePhase.Sabotage;
    }

    private void OnDestroy()
    {
        if (SuspicionMeter.Instance != null)
        {
            SuspicionMeter.Instance.Full -= OnSuspicionFull;
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
        Phase = GamePhase.Arrival;

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

        if (coupleWalkIn != null && (!WasCaught || walkInWhenCaught))
        {
            yield return coupleWalkIn.Play();
        }

        Phase = GamePhase.Cutscene;

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


    private void OnSuspicionFull()
    {
        if (Phase != GamePhase.Sabotage)
        {
            return;
        }

        StartCoroutine(ThrownOut());
    }

    private IEnumerator ThrownOut()
    {
        Phase = GamePhase.Cutscene;
        WasCaught = true;

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

        if (SuspicionMeter.Instance != null)
        {
            SuspicionMeter.Instance.Hide();
        }

        yield return cutscene.PlayThrownOut(thrownOutVignette);

        Phase = GamePhase.Finished;
        if (resultScreen != null)
        {
            resultScreen.Show(false);
        }
    }
}