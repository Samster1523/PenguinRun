using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float forwardSpeed = 0f;   // keep 0 if you move the world
    public float jumpForce = 11f;     // 10–12 feels good

    [Header("Jump Feel")]
    public float coyoteTime = 0.12f;      // grace after leaving ground
    public float jumpBuffer = 0.12f;      // grace before landing
    public float fallGravityMultiplier = 2.0f;  // heavier on way down
    public float jumpCutMultiplier = 2.5f;      // short hop when releasing
    public int maxJumps = 2;                // double jump

    [Header("Collision")]
    public LayerMask groundMask;          // set to Ground only in Inspector

    Rigidbody2D rb;
    CapsuleCollider2D col;

    float coyoteCounter;
    float jumpBufferCounter;
    int jumpsUsed;
    bool wasGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Autofill if you forgot in Inspector
        if (groundMask.value == 0) groundMask = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        // Optional forward drift if you're not moving the world
        if (forwardSpeed > 0f)
            rb.velocity = new Vector2(forwardSpeed, rb.velocity.y);

        bool grounded = IsGrounded();

        // Reset jumps when you touch ground
        if (grounded && !wasGrounded) jumpsUsed = 0;
        wasGrounded = grounded;

        // Timers (coyote + buffer)
        if (grounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        if (Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBuffer;
        else jumpBufferCounter -= Time.deltaTime;

        // Jump: ground/coyote OR in-air extra jumps
        if (jumpBufferCounter > 0f)
        {
            if (coyoteCounter > 0f)
            {
                DoJump();
                jumpsUsed = 1;           // first jump used
            }
            else if (jumpsUsed < maxJumps)
            {
                DoJump();                // mid-air jump
                jumpsUsed++;
            }
            jumpBufferCounter = 0f;
        }

        // Better fall
        if (rb.velocity.y < 0f)
            rb.velocity += Vector2.up * (Physics2D.gravity.y * (fallGravityMultiplier - 1f) * Time.deltaTime);

        // Jump cut (release to cap height)
        if (rb.velocity.y > 0f && !Input.GetButton("Jump"))
            rb.velocity += Vector2.up * (Physics2D.gravity.y * (jumpCutMultiplier - 1f) * Time.deltaTime);
    }

    void DoJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        coyoteCounter = 0f;
    }

    bool IsGrounded()
    {
        // Thin box under feet using collider bounds
        Bounds b = col.bounds;
        Vector2 size = new Vector2(b.size.x - 0.04f, 0.08f);
        Vector2 center = new Vector2(b.center.x, b.min.y - 0.03f);
        return Physics2D.OverlapBox(center, size, 0f, groundMask) != null;
    }
}
