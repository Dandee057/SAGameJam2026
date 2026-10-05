using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")] public float moveSpeed = 5f;
    public float acceleration = 45f;
    public float deceleration = 60f;

    [Header("Jump")] public float jumpHeight = 1.6f;
    public float gravity = -30f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    [Header("References")] public Transform cameraTransform;
    public SpriteRenderer spriteRenderer;

    [Tooltip(
        "Drives the idle/walk animation. Auto-found on the sprite if empty. Leave out to use a single static sprite.")]
    public SpriteFlipbook flipbook;

    public bool inputLocked;

    [Header("Hiding")]
    [Tooltip("Set IsHidden = true from anywhere (a hiding spot, a button) and the player swaps to this sprite. " +
             "Moving or jumping un-hides them again.")]
    public bool IsHidden;

    [Tooltip("Sprite shown while hidden. Leave empty to keep the normal sprite.")]
    public Sprite hiddenSprite;

    [Tooltip(
        "Movement is ignored for this long right after hiding, so a held direction key doesn't instantly un-hide you.")]
    public float unhideGraceTime = 0.1f;

    [Tooltip("Played when the player hides (Hide_Newspaper).")]
    public AudioClip hideSound;
    [Tooltip("Played when the player comes out of hiding (Unhide_Newspaper). Optional.")]
    public AudioClip unhideSound;
    [Range(0f, 1f)] public float hideSoundVolume = 1f;

    [Header("Knockback")] public float knockbackDecel = 20f;

    public bool IsStunned => stunTimer > 0f;

    private float stunTimer;
    private Vector3 knockbackVel;

    private Sprite normalSprite;
    private bool wasHidden;
    private float hiddenSince;


    public void Knockback(Vector3 direction, float speed, float stunDuration)
    {
        direction.y = 0f;
        knockbackVel = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : Vector3.zero;
        stunTimer = Mathf.Max(stunTimer, stunDuration);
    }


    public Vector3 Facing { get; private set; } = Vector3.forward;

    public bool IsGrounded => controller.isGrounded;

    private CharacterController controller;
    private Vector3 horizontalVel;
    private float verticalVel;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (spriteRenderer != null)
        {
            normalSprite = spriteRenderer.sprite;
        }

        if (flipbook == null)
        {
            flipbook = GetComponentInChildren<SpriteFlipbook>();
        }
    }

    private void SyncHiddenVisual()
    {
        if (IsHidden == wasHidden)
        {
            return;
        }

        wasHidden = IsHidden;
        if (IsHidden)
        {
            hiddenSince = Time.time;
        }

        Sfx.Play(IsHidden ? hideSound : unhideSound, hideSoundVolume, 0.1f);

        if (flipbook != null)
        {
            flipbook.Hidden = IsHidden;
        }
        else if (spriteRenderer != null && hiddenSprite != null)
        {
           
            spriteRenderer.sprite = IsHidden ? hiddenSprite : normalSprite;
        }
    }

    private void Update()
    {
        SyncHiddenVisual();

        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
        }


        Vector2 input = inputLocked || stunTimer > 0f
            ? Vector2.zero
            : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);

        bool jumpPressed = !inputLocked && stunTimer <= 0f && Input.GetButtonDown("Jump");
        if (jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (IsHidden && (input.sqrMagnitude > 0.01f || jumpPressed) && Time.time - hiddenSince >= unhideGraceTime)
        {
            IsHidden = false;
            SyncHiddenVisual();
        }

        Vector3 camF = cameraTransform.forward;
        camF.y = 0f;
        camF.Normalize();
        Vector3 camR = cameraTransform.right;
        camR.y = 0f;
        camR.Normalize();
        Vector3 wishDir = camF * input.y + camR * input.x;


        bool hasInput = wishDir.sqrMagnitude > 0.01f;
        if (knockbackVel.sqrMagnitude > 0.01f)
        {
            horizontalVel = knockbackVel;
            knockbackVel = Vector3.MoveTowards(knockbackVel, Vector3.zero, knockbackDecel * Time.deltaTime);
        }
        else
        {
            float rate = hasInput ? acceleration : deceleration;
            horizontalVel = Vector3.MoveTowards(horizontalVel, wishDir * moveSpeed, rate * Time.deltaTime);
            if (hasInput)
            {
                Facing = wishDir.normalized;
            }
        }

        if (flipbook != null)
        {
            flipbook.Moving = hasInput;
        }


        if (controller.isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (verticalVel < 0f)
            {
                verticalVel = -2f;
            }
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        if (Input.GetButtonUp("Jump") && verticalVel > 0f)
        {
            verticalVel *= jumpCutMultiplier;
        }

        verticalVel += gravity * Time.deltaTime;


        Vector3 velocity = horizontalVel + Vector3.up * verticalVel;
        controller.Move(velocity * Time.deltaTime);


        if (spriteRenderer)
        {
            float screenX = Vector3.Dot(horizontalVel, camR);
            if (Mathf.Abs(screenX) > 0.1f)
            {
                spriteRenderer.flipX = screenX < 0f;
            }
        }
    }
}