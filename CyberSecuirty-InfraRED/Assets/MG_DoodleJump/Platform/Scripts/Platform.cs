// Platform.cs
using UnityEngine;

/// <summary>
/// Platform behavior hook.
/// - Player bounce is handled in DoodleJumpPlayer3D_CC via CharacterController hit.
/// - This script is for platform-specific logic (break, etc).
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class Platform3D : MonoBehaviour
{
    [Header("Breakable")]
    [SerializeField] private bool breaks = false;
    [SerializeField, Min(0f)] private float breakDelay = 0.05f;

    private bool _used;

    private void Awake()
    {
        // Stable collision surface (avoid physics impulses if a Rigidbody exists).
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        GetComponent<Collider>().isTrigger = false;
    }

    public void OnPlayerBounced()
    {
        if (!breaks || _used) return;
        _used = true;
        Invoke(nameof(BreakNow), breakDelay);
    }

    private void BreakNow() => Destroy(gameObject);
}