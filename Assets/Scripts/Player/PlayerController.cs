using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float acceleration = 45f;
    public float deceleration = 60f;

    [Header("Jump")]
    public float jumpHeight = 1.6f;
    public float gravity = -30f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    [Header("References")]
    public Transform cameraTransform;         
    public SpriteRenderer spriteRenderer;     
    
    
    public Vector3 Facing { get; private set; } = Vector3.forward;
    public bool IsGrounded => controller.isGrounded;

    CharacterController controller;
    Vector3 horizontalVel;
    float verticalVel;
    float coyoteTimer;
    float jumpBufferTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
       
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);

        if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

       
        Vector3 camF = cameraTransform.forward; camF.y = 0f; camF.Normalize();
        Vector3 camR = cameraTransform.right;   camR.y = 0f; camR.Normalize();
        Vector3 wishDir = camF * input.y + camR * input.x;

       
        bool hasInput = wishDir.sqrMagnitude > 0.01f;
        float rate = hasInput ? acceleration : deceleration;
        horizontalVel = Vector3.MoveTowards(horizontalVel, wishDir * moveSpeed, rate * Time.deltaTime);
        if (hasInput) Facing = wishDir.normalized;

      
        if (controller.isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (verticalVel < 0f) verticalVel = -2f; 
        }
        else coyoteTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        if (Input.GetButtonUp("Jump") && verticalVel > 0f)
            verticalVel *= jumpCutMultiplier;

        verticalVel += gravity * Time.deltaTime;

      
        Vector3 velocity = horizontalVel + Vector3.up * verticalVel;
        controller.Move(velocity * Time.deltaTime);

      
        if (spriteRenderer)
        {
            float screenX = Vector3.Dot(horizontalVel, camR);
            if (Mathf.Abs(screenX) > 0.1f) spriteRenderer.flipX = screenX < 0f;
        }
    }
}
