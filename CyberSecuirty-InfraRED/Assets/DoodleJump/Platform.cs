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
        // Ensure it never falls even if someone added a Rigidbody
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    public void OnPlayerBounced()
    {
        if (!breaks || used) return;
        used = true;
        Invoke(nameof(BreakNow), breakDelay);
    }

    void BreakNow() => Destroy(gameObject);
}