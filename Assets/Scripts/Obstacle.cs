using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Obstacle : MonoBehaviour
{
    [Header("Cleanup")]
    public float killX = -40f;      // destroy when past this X (left side)

    [Header("Hit logic")]
    public bool oneHit = true;      // call death only once per obstacle
    bool consumed;

    void Reset()
    {
        // Make sure prefab is set up sensibly
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;                 // we use triggers for hits
        int idx = LayerMask.NameToLayer("Obstacle");
        if (idx >= 0) gameObject.layer = idx;          // put obstacle on Obstacle layer if it exists
    }

    void Update()
    {
        // Move left at the global game speed
        float speed = (GameManager.I != null) ? GameManager.I.CurrentScrollSpeed : 8f;
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        // Clean up off-screen
        if (transform.position.x < killX)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;               // only care about the player
        if (consumed && oneHit) return;                         // avoid double-hits
        if (GameManager.I != null && GameManager.I.Invulnerable) return; // revive grace

        consumed = true;
        GameManager.I?.OnPlayerHit();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Visualize killX in Scene view
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(killX, -1000f, 0f), new Vector3(killX, 1000f, 0f));
    }
#endif
}
