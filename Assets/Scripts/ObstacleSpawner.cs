using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                    // drag Main Camera (auto if null)
    public Transform player;              // drag Player
    public PlayerController playerCtrl;   // optional; auto-fills
    public Rigidbody2D playerRb;          // optional; auto-fills
    public CapsuleCollider2D playerCol;   // for head height (drag Player's CapsuleCollider2D)

    [Header("Prefabs")]
    public GameObject groundPrefab;       // your ground obstacle prefab (with Obstacle.cs + trigger collider)
    public GameObject overheadPrefab;     // your overhead bar prefab (with Obstacle.cs + trigger collider)

    [Header("Layers")]
    public LayerMask groundMask;          // set to Ground ONLY

    [Header("Timing")]
    public float spawnMin = 1.0f;         // seconds between spawns
    public float spawnMax = 1.6f;
    public float spawnXOffset = 18f;      // how far to the right of camera to spawn

    [Header("Ground (constant size)")]
    public float groundScaleX = 1.00f;
    public float groundScaleY = 0.60f;
    public float groundPadY = 0.02f;    // tiny lift so it sits cleanly on ground

    [Header("Overhead (thin bar @ 3 heights)")]
    public float barThicknessWorld = 0.20f; // world height of the overhead bar
    public float runUnderClearance = 0.20f; // gap above player head for run-under
    [Tooltip("Top of bar as a fraction of single-jump apex (below 1 = single jump).")]
    public float singleTopFrac = 0.90f;
    [Tooltip("Top of bar as a fraction of single-jump apex (above 1 = needs double jump).")]
    public float doubleTopFrac = 1.30f;
    public float overheadPadY = 0.02f;  // tiny extra space

    [Header("Mix balance")]
    public bool keepTypeBalanced = true;  // keep ground vs overhead roughly even

    // internals
    float nextTime;
    int countGround, countOver;           // type balancing
    int cRun, cSingle, cDouble;           // lane balancing

    enum HeightMode { RunUnder = 0, SingleOver = 1, DoubleOver = 2 }

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!playerCtrl && player) playerCtrl = player.GetComponent<PlayerController>();
        if (!playerRb && player) playerRb = player.GetComponent<Rigidbody2D>();
        Schedule();
    }

    void Update()
    {
        if (Time.time >= nextTime)
        {
            SpawnOne();
            Schedule();
        }
    }

    void Schedule() => nextTime = Time.time + Random.Range(spawnMin, spawnMax);

    void SpawnOne()
    {
        if (!groundPrefab || !overheadPrefab || !cam) return;

        // Spawn X and surface Y
        float x = cam.transform.position.x + spawnXOffset;
        float rayY = cam.transform.position.y + cam.orthographicSize + 5f;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, rayY), Vector2.down, 100f, groundMask);
        float surfaceY = hit ? hit.point.y : (player ? player.position.y - 1f : 0f);

        bool spawnOverhead = DecideType();
        if (!spawnOverhead)
        {
            SpawnGround(x, surfaceY);
            countGround++;
        }
        else
        {
            SpawnOverhead(x, surfaceY);
            countOver++;
        }
    }

    bool DecideType()
    {
        if (!keepTypeBalanced) return Random.value < 0.5f;
        if (countGround < countOver) return false;  // choose ground to catch up
        if (countOver < countGround) return true; // choose overhead to catch up
        return Random.value < 0.5f;
    }

    // -------- Ground ----------
    void SpawnGround(float x, float surfaceY)
    {
        var go = Instantiate(groundPrefab, new Vector3(x, surfaceY, 0f), Quaternion.identity);
        go.transform.localScale = new Vector3(groundScaleX, groundScaleY, 1f);
        Physics2D.SyncTransforms();

        var col = go.GetComponent<Collider2D>();
        if (col)
        {
            Bounds b = col.bounds;
            float dy = (surfaceY + groundPadY) - b.min.y;      // snap bottom to ground
            go.transform.position += new Vector3(0f, dy, 0f);
        }
    }

    // -------- Overhead ----------
    void SpawnOverhead(float x, float surfaceY)
    {
        var go = Instantiate(overheadPrefab, new Vector3(x, surfaceY, 0f), Quaternion.identity);

        // Ensure bar has desired world thickness (height in world units)
        SetWorldThickness(go, barThicknessWorld);
        Physics2D.SyncTransforms();

        var col = go.GetComponent<Collider2D>();
        if (!col) return;

        Bounds b = col.bounds;
        float h = b.size.y;

        // Jump heights
        float singleH = EstimateSingleJumpHeight();
        float doubleH = singleH * 1.9f; // guard cap
        float playerH = playerCol ? playerCol.bounds.size.y : 1.8f;
        float headY = surfaceY + playerH; // top of standing player

        // Choose height mode with soft balancing (keep variety)
        HeightMode mode = NextHeightMode();

        float bottom;
        switch (mode)
        {
            case HeightMode.RunUnder:
                bottom = headY + runUnderClearance;
                cRun++;
                break;

            case HeightMode.SingleOver:
                {
                    float top = surfaceY + Mathf.Clamp(singleH * singleTopFrac, 0.5f, singleH * 0.98f);
                    bottom = Mathf.Max(surfaceY + 0.05f, top - h); // don't intersect ground
                    cSingle++;
                    break;
                }

            default: // DoubleOver
                {
                    float topTarget = surfaceY + Mathf.Max(singleH * 1.01f, singleH * doubleTopFrac);
                    float topMax = surfaceY + doubleH * 0.90f;
                    float top = Mathf.Min(topTarget, topMax);
                    bottom = Mathf.Max(surfaceY + 0.05f, top - h);
                    cDouble++;
                    break;
                }
        }

        float y = bottom + h * 0.5f + overheadPadY;
        go.transform.position = new Vector3(x, y, 0f);
    }

    HeightMode NextHeightMode()
    {
        int min = Mathf.Min(cRun, Mathf.Min(cSingle, cDouble));
        bool runOk = cRun == min, singleOk = cSingle == min, doubleOk = cDouble == min;

        int pool = (runOk ? 1 : 0) + (singleOk ? 1 : 0) + (doubleOk ? 1 : 0);
        int pick = Random.Range(0, pool);
        if (runOk) { if (pick-- == 0) return HeightMode.RunUnder; }
        if (singleOk) { if (pick-- == 0) return HeightMode.SingleOver; }
        return HeightMode.DoubleOver;
    }

    float EstimateSingleJumpHeight()
    {
        float g = Mathf.Abs(Physics2D.gravity.y);
        if (playerRb) g *= playerRb.gravityScale;
        float v0 = playerCtrl ? playerCtrl.jumpForce : 11f; // impulse ~ initial vel (mass≈1)
        return (v0 * v0) / (2f * Mathf.Max(0.01f, g));
    }

    void SetWorldThickness(GameObject go, float desiredH)
    {
        var c = go.GetComponent<Collider2D>(); if (!c) return;
        float currentH = c.bounds.size.y;
        if (currentH <= 0.0001f) return;
        float scaleY = desiredH / currentH;
        var t = go.transform;
        t.localScale = new Vector3(t.localScale.x, t.localScale.y * scaleY, t.localScale.z);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!cam) return;
        float x = cam.transform.position.x + spawnXOffset;
        float rayY = cam.transform.position.y + cam.orthographicSize + 5f;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, rayY), Vector2.down, 100f, groundMask);
        float surfaceY = hit ? hit.point.y : 0f;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(x, surfaceY, 0f), new Vector3(x, surfaceY + 0.5f, 0f));
    }
#endif
}
