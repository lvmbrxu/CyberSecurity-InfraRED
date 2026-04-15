// Platform3D.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Platform3D : MonoBehaviour
{
    public bool breaks = false;
    public float breakDelay = 0.05f;

    bool used;

    void Awake()
    {
        // Best: no Rigidbody at all on platforms.
        // If one exists, force it to be immovable.
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // Never be a trigger (we want a real landing surface)
        var col = GetComponent<Collider>();
        col.isTrigger = false;
    }

    public void OnPlayerBounced()
    {
        if (!breaks || used) return;
        used = true;
        Invoke(nameof(BreakNow), breakDelay);
    }

    void BreakNow() => Destroy(gameObject);
}