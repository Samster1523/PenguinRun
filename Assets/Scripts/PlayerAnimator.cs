using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Refs")]
    public Transform art;        // drag the Art child
    public Animator animator;    // Animator on Art
    public LayerMask groundMask; // set to Ground

    [Header("Run speed mapping")]
    public float baseRunSpeed = 8f; // world speed that equals RunSpeedMult = 1

    [Header("Keep height stable")]
    public bool lockArtLocalY = true;

    Rigidbody2D rb;
    CapsuleCollider2D col;
    float baseLocalY;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();

        if (!art) art = transform.Find("Art");
        if (!animator && art) animator = art.GetComponent<Animator>();
        if (groundMask.value == 0) groundMask = LayerMask.GetMask("Ground");

        if (art) baseLocalY = art.localPosition.y;

        // Align Art once to collider bottom if it's at zero:
        if (art && Mathf.Approximately(baseLocalY, 0f))
        {
            float H = col.size.y * Mathf.Abs(transform.lossyScale.y);
            baseLocalY = -H * 0.5f;
            art.localPosition = new Vector3(art.localPosition.x, baseLocalY, art.localPosition.z);
        }
    }

    void Update()
    {
        if (!animator) return;

        if (art && lockArtLocalY)
            art.localPosition = new Vector3(art.localPosition.x, baseLocalY, art.localPosition.z);

        bool grounded = IsGrounded();
        animator.SetBool("Grounded", grounded);

        // fire jump trigger on takeoff
        if (!grounded && rb.velocity.y > 0.01f)
            animator.SetTrigger("JumpTrigger");

        float worldSpeed = (GameManager.I != null) ? GameManager.I.CurrentScrollSpeed : baseRunSpeed;
        float xSpeed = Mathf.Abs(rb.velocity.x);
        float visual = (xSpeed > 0.01f) ? xSpeed : worldSpeed;
        float mult = Mathf.Clamp(visual / Mathf.Max(0.01f, baseRunSpeed), 0.8f, 1.8f);
        animator.SetFloat("RunSpeedMult", mult);
    }

    bool IsGrounded()
    {
        Bounds b = col.bounds;
        float pad = 0.06f;
        Vector2 p1 = new Vector2(b.min.x + 0.02f, b.min.y - pad);
        Vector2 p2 = new Vector2(b.max.x - 0.02f, b.min.y + 0.02f);
        return Physics2D.OverlapArea(p1, p2, groundMask) != null;
    }
}
