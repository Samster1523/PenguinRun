using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    public int value = 1;
    public float killX = -40f;

    [Header("Overlap safety")]
    public LayerMask obstacleMask;         // set to Obstacle
    public LayerMask coinMask;             // set to Collectible
    public float avoidHalfWidthX = 1.0f;   // rectangle wider than coin
    public float avoidHalfHeightY = 0.4f;  // extra Y margin
    public int settleAttempts = 10;      // nudges to try
    public float verticalStep = 0.35f;     // Y nudge
    public float horizontalStep = 0.6f;    // X nudge
    public float minCoinSeparation = 0.30f;// spacing vs other coins

    float coinRadius = 0.22f;
    Collider2D myCol;
    int verifyFrames = 3; // re-check a few early frames in case an obstacle spawns same frame

    void OnEnable()
    {
        myCol = GetComponent<Collider2D>();

        // measure world radius
        var cc = GetComponent<CircleCollider2D>();
        if (cc)
        {
            float s = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            coinRadius = cc.radius * s;
        }
        else
        {
            coinRadius = myCol.bounds.extents.y;
        }

        // try to settle now; destroy if impossible
        if (!SettleClearPosition())
            Destroy(gameObject);
    }

    void LateUpdate()
    {
        // extra safety: re-verify a few first frames (handles race/resize cases)
        if (verifyFrames-- > 0 && !PositionIsClear(transform.position))
        {
            if (!SettleClearPosition())
                Destroy(gameObject);
        }

        float speed = (GameManager.I != null) ? GameManager.I.CurrentScrollSpeed : 8f;
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        if (transform.position.x < killX)
            Destroy(gameObject);
    }

    bool SettleClearPosition()
    {
        Vector3 start = transform.position;
        List<Vector3> candidates = new List<Vector3>(settleAttempts);
        candidates.Add(start);

        // symmetric vertical nudges
        for (int k = 1; candidates.Count < settleAttempts && k <= 3; k++)
        {
            candidates.Add(start + new Vector3(0f, k * verticalStep, 0f));
            if (candidates.Count < settleAttempts)
                candidates.Add(start + new Vector3(0f, -k * verticalStep, 0f));
        }
        // forward + vertical combos
        for (int k = 1; candidates.Count < settleAttempts && k <= 3; k++)
        {
            candidates.Add(start + new Vector3(k * horizontalStep, k * 0.5f * verticalStep, 0f));
            if (candidates.Count < settleAttempts)
                candidates.Add(start + new Vector3(k * horizontalStep, -k * 0.5f * verticalStep, 0f));
        }

        foreach (var p in candidates)
        {
            if (PositionIsClear(p))
            {
                transform.position = p;
                return true;
            }
        }
        return false;
    }

    bool PositionIsClear(Vector2 p)
    {
        // obstacle check (rect wider than coin)
        Vector2 size = new Vector2(avoidHalfWidthX * 2f, (coinRadius * 2f) + avoidHalfHeightY * 2f);
        if (Physics2D.OverlapBox(p, size, 0f, obstacleMask) != null)
            return false;

        // coin-vs-coin check (circle), ignoring self
        float coinSepRadius = coinRadius + minCoinSeparation * 0.5f;
        var hits = Physics2D.OverlapCircleAll(p, coinSepRadius, coinMask);
        for (int h = 0; h < hits.Length; h++)
        {
            if (hits[h] && hits[h] != myCol) return false;
        }
        return true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.I?.AddCoins(value);
            Destroy(gameObject);
        }
    }
}
