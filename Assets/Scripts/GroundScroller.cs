using System.Linq;
using UnityEngine;

public class GroundScroller : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;                  // Drag Main Camera (auto-fills if null)
    public Transform[] chunks;          // Drag your ground chunks (2–3). If empty, auto-finds by name "GroundChunk"

    [Header("Settings")]
    public float recycleBuffer = 0.25f; // extra room off-screen before recycling
    public float fallbackSpeed = 8f;    // used only if GameManager is missing
    public bool lockYToFirst = true;    // keeps all chunks at the first chunk's Y

    float[] widths;
    float baseY;

    void Awake()
    {
        if (!cam) cam = Camera.main;

        // Auto-fill if nothing assigned: find objects named "GroundChunk*"
        if (chunks == null || chunks.Length == 0)
        {
            chunks = FindObjectsOfType<Transform>(true)
                .Where(t => t.name.StartsWith("GroundChunk"))
                .OrderBy(t => t.position.x)
                .ToArray();
        }

        if (chunks == null || chunks.Length == 0)
        {
            Debug.LogError("[GroundScroller] No ground chunks assigned or found.");
            enabled = false;
            return;
        }

        widths = new float[chunks.Length];
        for (int i = 0; i < chunks.Length; i++)
            widths[i] = GetWorldBounds(chunks[i]).size.x;

        if (lockYToFirst) baseY = chunks[0].position.y;
    }

    void Update()
    {
        float speed = (GameManager.I != null) ? GameManager.I.CurrentScrollSpeed : fallbackSpeed;
        Vector3 delta = Vector3.left * speed * Time.deltaTime;

        // Move
        for (int i = 0; i < chunks.Length; i++)
        {
            if (lockYToFirst)
                chunks[i].position = new Vector3(chunks[i].position.x, baseY, chunks[i].position.z);

            chunks[i].position += delta;
        }

        // Camera left edge (world)
        float camLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect - recycleBuffer;

        // Track the rightmost edge
        float rightmostEdge = float.NegativeInfinity;
        for (int i = 0; i < chunks.Length; i++)
        {
            var b = GetWorldBounds(chunks[i]);
            if (b.max.x > rightmostEdge) rightmostEdge = b.max.x;
        }

        // Recycle any chunk fully off-screen left
        for (int i = 0; i < chunks.Length; i++)
        {
            var b = GetWorldBounds(chunks[i]);
            if (b.max.x < camLeft)
            {
                float newCenterX = rightmostEdge + widths[i] * 0.5f;
                float shift = newCenterX - b.center.x;
                chunks[i].position += new Vector3(shift, 0f, 0f);

                // update rightmostEdge after moving this chunk
                rightmostEdge = GetWorldBounds(chunks[i]).max.x;
            }
        }
    }

    // ---- helpers ----
    static Bounds GetWorldBounds(Transform t)
    {
        var rend = t.GetComponentInChildren<Renderer>();
        if (rend) return rend.bounds;

        var col = t.GetComponentInChildren<Collider2D>();
        if (col) return col.bounds;

        // Fallback: a small box around the transform
        return new Bounds(t.position, new Vector3(10f, 1f, 0f));
    }
}

