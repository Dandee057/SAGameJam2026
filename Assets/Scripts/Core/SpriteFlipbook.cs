using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dead-simple sprite animation: flips through a list of sprites (e.g. 2 frames) on a loop.
/// Works on BOTH world sprites (SpriteRenderer) and UI (Image) - just add it to the object
/// that shows the sprite and drag the frames in.
/// </summary>
public class SpriteFlipbook : MonoBehaviour
{
    [Tooltip("The frames, in order. Two is fine.")]
    public Sprite[] frames;

    [Tooltip("Seconds each frame is shown. Lower = faster.")]
    public float frameTime = 0.25f;

    public bool loop = true;
    public bool playOnEnable = true;

    [Tooltip("Start on a random frame/time so several copies (e.g. diners) don't animate in sync.")]
    public bool randomStart = true;

    [Tooltip("Keep animating even when the game is paused (Time.timeScale = 0). Use for menus/UI.")]
    public bool ignorePause = false;

    private SpriteRenderer spriteRenderer;
    private Image image;
    private int index;
    private float timer;
    private bool playing;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        image = GetComponent<Image>();
        if (spriteRenderer == null && image == null)
            Debug.LogWarning("SpriteFlipbook needs a SpriteRenderer or an Image on the same object.", this);
    }

    private void OnEnable()
    {
        if (frames == null || frames.Length == 0) return;
        index = randomStart ? Random.Range(0, frames.Length) : 0;
        timer = randomStart ? Random.Range(0f, frameTime) : frameTime;
        Show(index);
        playing = playOnEnable;
    }

    private void Update()
    {
        if (!playing || frames == null || frames.Length < 2) return;

        timer -= ignorePause ? Time.unscaledDeltaTime : Time.deltaTime;
        if (timer > 0f) return;
        timer += Mathf.Max(0.01f, frameTime);

        index++;
        if (index >= frames.Length)
        {
            if (!loop) { index = frames.Length - 1; playing = false; return; }
            index = 0;
        }
        Show(index);
    }

    // ---------- controls (callable from other scripts or UnityEvents) ----------

    public void Play() { playing = true; }
    public void Stop() { playing = false; }

    /// <summary>Restart from the first frame.</summary>
    public void Restart()
    {
        index = 0;
        timer = frameTime;
        Show(0);
        playing = true;
    }

    private void Show(int i)
    {
        if (frames == null || i < 0 || i >= frames.Length || frames[i] == null) return;
        if (spriteRenderer) spriteRenderer.sprite = frames[i];
        if (image) image.sprite = frames[i];
    }
}
