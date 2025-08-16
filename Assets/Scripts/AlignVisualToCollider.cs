using UnityEngine;

[ExecuteAlways]
public class AlignVisualToCollider : MonoBehaviour
{
    public Transform visual;   // assign Player/Sprite child
    public Collider2D col;     // assign the CapsuleCollider2D on Player
    public float footPadding = 0f; // small lift/lower if you see z-fighting

    [ContextMenu("Align Now")]
    public void AlignNow()
    {
        if (!visual || !col) return;
        var sr = visual.GetComponentInChildren<SpriteRenderer>();
        if (!sr) return;

        Bounds colB = col.bounds;
        Bounds sprB = sr.bounds;

        float deltaY = (colB.min.y + footPadding) - sprB.min.y; // move sprite so bottoms match
        visual.position += new Vector3(0f, deltaY, 0f);
    }

#if UNITY_EDITOR
    void OnValidate() { AlignNow(); }  // auto-align when something changes in Inspector
#endif
}
