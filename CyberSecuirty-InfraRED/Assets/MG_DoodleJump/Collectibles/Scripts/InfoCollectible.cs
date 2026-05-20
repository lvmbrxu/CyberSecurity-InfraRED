// InfoCollectible.cs (Security +/- pickup, production)
using UnityEngine;

/// <summary>
/// Security pickup.
/// - Adds/subtracts Security via GameManager.
/// - CharacterController-safe: trigger + kinematic Rigidbody.
/// - Player detection by component (supports child colliders).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class InfoCollectible : MonoBehaviour
{
    [Tooltip("+0.05 = +5%, -0.10 = -10%")]
    [SerializeField] private float securityDelta01 = 0.05f;

    [Header("Optional")]
    [SerializeField] private AudioSource sfx;

    private bool _collected;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (!TryGetComponent<Rigidbody>(out var rb))
            rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;

        if (other.GetComponentInParent<DoodleJumpPlayer3D_CC>() == null) return;

        _collected = true;

        GameManager.Instance?.AddSecurityDelta01(securityDelta01);

        if (TryGetComponent<Collider>(out var c)) c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;

        if (sfx != null)
        {
            sfx.Play();
            Destroy(gameObject, (sfx.clip != null) ? sfx.clip.length : 0f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}