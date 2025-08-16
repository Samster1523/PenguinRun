using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                   // Main Camera (auto if null)
    public Transform player;             // Player (for fallback Y)
    public PlayerController playerCtrl;  // optional; auto
    public Rigidbody2D playerRb;         // optional; auto
    public GameObject coinPrefab;        // Coin prefab with CircleCollider2D (IsTrigger ON)

    [Header("Layers")]
    public LayerMask groundMask;         // Ground ONLY
    public LayerMask obstacleMask;       // Obstacle ONLY

    [Header("Timing")]
    public float spawnMin = 1.0f;
    public float spawnMax = 1.6f;
    public float spawnXOffset = 18f;

    [Header("Burst (now 1–3)")]
    public int minCoins = 1;
    public int maxCoins = 3;
    public float gapX = 1.1f;            // spacing in a line
    public float wobbleY = 0.0f;         // keep 0 for tight clearance (can set to 0.15f later)

    [Header("Clearance")]
    public float extraPad = 0.20f;       // extra free space around each coin (world units)
    public float laneExtraWidth = 0.5f;  // extra X margin when testing lane box
    public float laneExtraHeight = 0.25f;// extra Y margin when testing lane box

    [Header("Per-coin fallback nudges")]
    public int placementAttempts = 8;    // positions to try if initial spot touches an obstacle
    public float verticalStep = 0.30f;
    public float horizontalStep = 0.50f;

    // internal
    float nextTime;
    float coinR = 0.22f;                 // world radius (auto)
    int laneLow, laneMid, laneHigh;      // soft balance

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!playerCtrl && player) playerCtrl = player.GetComponent<PlayerController>();
        if (!playerRb && player) playerRb = player.GetComponent<Rigidbody2D>();

        // auto radius from prefab collider (scaled)
        var cc = coinPrefab ? coinPrefab.GetComponent<CircleCollider2D>() : null;
        if (cc)
        {
            // prefab local scale is ignored for bounds; use lossy after instantiation
            // we assume prefab scale ~ 1; radius is a good estimate here
            coinR = cc.radius;
        }

        Schedule();
    }

    void Update()
    {
        if (Time.time >= nextTime)
        {
            SpawnBurst();
            Schedule();
        }
    }

    void Schedule() => nextTime = Time.time + Random.Range(spawnMin, spawnMax);

    void SpawnBurst()
    {
        if (!coinPrefab || !cam) return;

        // Spawn X and ground surface
        float x0 = cam.transform.position.x + spawnXOffset;
        float rayY = cam.transform.position.y + cam.orthographicSize + 5f;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x0, rayY), Vector2.down, 100f, groundMask);
        float surfaceY = hit ? hit.point.y : (player ? player.position.y - 1f : 0f);

        // Estimate jump heights for lanes
        float singleH = EstimateSingleJumpHeight();
        float doubleH = singleH * 1.9f;

        // three lanes: low (near ground), mid (single jump), high (double)
        float lowY = surfaceY + 0.65f;
        float midY = surfaceY + Mathf.Clamp(singleH * 0.80f, 0.9f, singleH * 0.95f);
        float highY = surfaceY + Mathf.Min(singleH * 1.20f, doubleH * 0.90f);

        int count = Random.Range(minCoins, maxCoins + 1);

        // pick balanced lane, but require the WHOLE burst box to be clear
        int lane = PickBalancedLane();
        if (!LaneIsClear(lane, x0, count, gapX, lowY, midY, highY))
        {
            int l1 = (lane + 1) % 3, l2 = (lane + 2) % 3;
            if (LaneIsClear(l1, x0, count, gapX, lowY, midY, highY)) lane = l1;
            else if (LaneIsClear(l2, x0, count, gapX, lowY, midY, highY)) lane = l2;
            else return; // all blocked -> skip burst
        }

        float baseY = (lane == 0) ? lowY : (lane == 1) ? midY : highY;

        // place coins; each coin gets a local safety check + nudges
        for (int i = 0; i < count; i++)
        {
            float xi = x0 + i * gapX;
            float yi = baseY + Mathf.Sin(i * 0.6f) * wobbleY;
            TryPlaceCoin(new Vector3(xi, yi, 0f));
        }

        if (lane == 0) laneLow++;
        else if (lane == 1) laneMid++;
        else laneHigh++;
    }

    bool LaneIsClear(int lane, float x0, int count, float dx, float lowY, float midY, float highY)
    {
        float baseY = (lane == 0) ? lowY : (lane == 1) ? midY : highY;

        float burstW = (count - 1) * dx;
        float centerX = x0 + burstW * 0.5f;
        float centerY = baseY;

        float boxW = burstW + 2f * (coinR + extraPad + laneExtraWidth);
        float boxH = 2f * (coinR + extraPad + laneExtraHeight) + Mathf.Abs(wobbleY) * 2f;

        // single wide box representing the whole burst must be obstacle-free
        return Physics2D.OverlapBox(new Vector2(centerX, centerY), new Vector2(boxW, boxH), 0f, obstacleMask) == null;
    }

    bool TryPlaceCoin(Vector3 pos)
    {
        // Generate candidate positions: exact, up/down, forward + slight up/down
        Vector3[] cand = new Vector3[placementAttempts];
        cand[0] = pos; int idx = 1;

        for (int k = 1; idx < placementAttempts && k <= 2; k++)
        {
            cand[idx++] = pos + new Vector3(0f, k * verticalStep, 0f);
            if (idx < placementAttempts) cand[idx++] = pos + new Vector3(0f, -k * verticalStep, 0f);
        }
        for (int k = 1; idx < placementAttempts && k <= 2; k++)
        {
            cand[idx++] = pos + new Vector3(k * horizontalStep, k * 0.5f * verticalStep, 0f);
            if (idx < placementAttempts) cand[idx++] = pos + new Vector3(k * horizontalStep, -k * 0.5f * verticalStep, 0f);
        }

        float r = coinR + extraPad;
        Vector2 size = new Vector2(r * 2f, r * 2f); // small box ≥ circle bound

        for (int i = 0; i < cand.Length; i++)
        {
            if (Physics2D.OverlapBox(cand[i], size, 0f, obstacleMask) != null)
                continue;

            var go = Instantiate(coinPrefab, cand[i], Quaternion.identity);
            // Coin.cs already moves at GameManager speed; nothing to set here.
            return true;
        }
        return false; // skip this coin if every candidate is blocked
    }

    int PickBalancedLane()
    {
        int min = Mathf.Min(laneLow, Mathf.Min(laneMid, laneHigh));
        bool lowOK = laneLow == min, midOK = laneMid == min, highOK = laneHigh == min;
        int pool = (lowOK ? 1 : 0) + (midOK ? 1 : 0) + (highOK ? 1 : 0);
        int pick = Random.Range(0, pool);
        if (lowOK) { if (pick-- == 0) return 0; }
        if (midOK) { if (pick-- == 0) return 1; }
        return 2;
    }

    float EstimateSingleJumpHeight()
    {
        float g = Mathf.Abs(Physics2D.gravity.y);
        if (playerRb) g *= playerRb.gravityScale;
        float v0 = playerCtrl ? playerCtrl.jumpForce : 11f; // impulse ≈ initial vel (mass~1)
        return (v0 * v0) / (2f * Mathf.Max(0.01f, g));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Visualize the three lane Y levels where coins may appear
        if (!cam) cam = Camera.main;
        float x0 = cam.transform.position.x + spawnXOffset;

        float rayY = cam.transform.position.y + cam.orthographicSize + 5f;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x0, rayY), Vector2.down, 100f, groundMask);
        float surfaceY = hit ? hit.point.y : 0f;

        float singleH = 2f;
        if (Application.isPlaying && player) singleH = EstimateSingleJumpHeight();
        float doubleH = singleH * 1.9f;

        float lowY = surfaceY + 0.65f;
        float midY = surfaceY + Mathf.Clamp(singleH * 0.80f, 0.9f, singleH * 0.95f);
        float highY = surfaceY + Mathf.Min(singleH * 1.20f, doubleH * 0.90f);

        Gizmos.color = Color.yellow; Gizmos.DrawLine(new Vector3(x0 - 1, lowY, 0), new Vector3(x0 + 1, lowY, 0));
        Gizmos.color = Color.cyan; Gizmos.DrawLine(new Vector3(x0 - 1, midY, 0), new Vector3(x0 + 1, midY, 0));
        Gizmos.color = Color.magenta; Gizmos.DrawLine(new Vector3(x0 - 1, highY, 0), new Vector3(x0 + 1, highY, 0));
    }
#endif
}
