using UnityEngine;

/// Kinematic character controller: camera-relative walk + jump (coyote time, jump buffer, variable height).
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
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f; // release early = shorter hop

    [Header("References")] public Transform cameraTransform; // defaults to Camera.main
    public SpriteRenderer spriteRenderer; // optional, for left/right flip

    /// Set by minigames etc. Player stops moving/jumping but still falls.
    public bool inputLocked;

    /// Set by the (future) hiding mechanic. GameFlow checks it the moment the timer runs out.
    public bool IsHidden;

    [Header("Knockback")]
    public float knockbackDecel = 20f; // how fast a shove slows down (speed 9 / decel 20 = ~2 units)

    public bool IsStunned => stunTimer > 0f;

    private float stunTimer;
    private Vector3 knockbackVel;

    /// Shove the player in a flat direction and take away control for 'stunDuration' seconds.
    public void Knockback(Vector3 direction, float speed, float stunDuration)
    {
        direction.y = 0f;
        knockbackVel = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : Vector3.zero;
        stunTimer = Mathf.Max(stunTimer, stunDuration);
    }

    /// Last direction the player moved in (world space, flat). Used for dropping items in front of you.
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
    }

    private void Update()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
        }

        // ---- Input ----
        Vector2 input = inputLocked || stunTimer > 0f
            ? Vector2.zero
            : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);

        if (!inputLocked && stunTimer <= 0f && Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // ---- Camera-relative direction (flattened so the iso tilt doesn't slow us down) ----
        Vector3 camF = cameraTransform.forward;
        camF.y = 0f;
        camF.Normalize();
        Vector3 camR = cameraTransform.right;
        camR.y = 0f;
        camR.Normalize();
        Vector3 wishDir = camF * input.y + camR * input.x;

        // ---- Horizontal ----
        bool hasInput = wishDir.sqrMagnitude > 0.01f;
        if (knockbackVel.sqrMagnitude > 0.01f)
        {
            // being shoved: knockback overrides normal movement and fades out
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

        // ---- Vertical ----
        if (controller.isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (verticalVel < 0f)
            {
                verticalVel = -2f; // small stick-to-ground force
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

        // ---- Move ----
        Vector3 velocity = horizontalVel + Vector3.up * verticalVel;
        controller.Move(velocity * Time.deltaTime);

        // ---- Sprite flip based on screen-space direction ----
        if (spriteRenderer != null)
        {
            float screenX = Vector3.Dot(horizontalVel, camR);
            if (Mathf.Abs(screenX) > 0.1f)
            {
                spriteRenderer.flipX = screenX < 0f;
            }
        }
    }
}