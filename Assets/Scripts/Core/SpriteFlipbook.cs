using UnityEngine;

public class SpriteFlipbook : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [Header("Frames")] public Sprite[] idleFrames;
    public Sprite[] walkFrames;
    public Sprite[] hiddenFrames;

    [Header("Timing (seconds per frame)")] public float idleFrameTime = 0.5f;
    public float walkFrameTime = 0.15f;
    public float hiddenFrameTime = 0.5f;

    [System.NonSerialized] public bool Moving;
    [System.NonSerialized] public bool Hidden;

    private enum State
    {
        Idle,
        Walk,
        Hidden
    }

    private State currentState;
    private int frame;
    private float timer;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // Hidden takes priority, then walking, then idle
        State newState = Hidden ? State.Hidden : Moving ? State.Walk : State.Idle;

        Sprite[] frames;
        float frameTime;
        switch (newState)
        {
            case State.Hidden:
                frames = hiddenFrames;
                frameTime = hiddenFrameTime;
                break;
            case State.Walk:
                frames = walkFrames;
                frameTime = walkFrameTime;
                break;
            default:
                frames = idleFrames;
                frameTime = idleFrameTime;
                break;
        }

        if (frames == null || frames.Length == 0)
        {
            return;
        }

        // Restart the animation whenever the state changes
        if (newState != currentState)
        {
            currentState = newState;
            frame = 0;
            timer = 0f;
        }

        frameTime = Mathf.Max(0.01f, frameTime);
        timer += Time.deltaTime;
        while (timer >= frameTime)
        {
            timer -= frameTime;
            frame++;
        }

        spriteRenderer.sprite = frames[frame % frames.Length];
    }
}