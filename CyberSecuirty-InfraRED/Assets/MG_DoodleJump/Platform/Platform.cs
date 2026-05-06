// Platform.cs
using UnityEngine;

/// <summary>
/// Platform landing surface.
/// - CharacterController bounce is handled in the Player (OnControllerColliderHit).
/// - This script only handles platform-specific behaviors (break, etc).
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class Platform : MonoBehaviour
{
    [Header("Breakable")]
    [SerializeField] private bool breaks = false;
    [SerializeField, Min(0f)] private float breakDelay = 0.05f;

    private bool _used;

    private void Awake()
    {
        // Ensure stable collision surface.
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        var col = GetComponent<Collider>();
        col.isTrigger = false;
    }

    public void OnPlayerBounced()
    {
        if (!breaks || _used) return;
        _used = true;
        Invoke(nameof(BreakNow), breakDelay);
    }

    private void BreakNow() => Destroy(gameObject);
}