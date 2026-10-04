using UnityEngine;

public class SpriteFlipbook : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [Header("Frames")] public Sprite[] idleFrames;
    public Sprite[] walkFrames;

    [Header("Timing (seconds per frame)")] public float idleFrameTime = 0.5f;
    public float walkFrameTime = 0.15f;
    
    [System.NonSerialized] public bool Moving;
    
    [System.NonSerialized] public Sprite holdSprite;

    private int frame;
    private float timer;
    private bool wasMoving;

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

        if (holdSprite != null)
        {
            spriteRenderer.sprite = holdSprite;
            return;
        }

        Sprite[] frames = Moving ? walkFrames : idleFrames;
        if (frames == null || frames.Length == 0)
        {
            return;
        }
        
        if (Moving != wasMoving)
        {
            wasMoving = Moving;
            frame = 0;
            timer = 0f;
        }

        float frameTime = Mathf.Max(0.01f, Moving ? walkFrameTime : idleFrameTime);
        timer += Time.deltaTime;
        while (timer >= frameTime)
        {
            timer -= frameTime;
            frame++;
        }

        spriteRenderer.sprite = frames[frame % frames.Length];
    }
}