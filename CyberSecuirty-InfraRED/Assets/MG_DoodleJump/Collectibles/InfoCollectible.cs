// InfoCollectible.cs (Security +/- pickup, CharacterController-safe)
using UnityEngine;

/// <summary>
/// Security pickup.
/// - Uses trigger volume + kinematic rigidbody for reliable trigger callbacks with CharacterController.
/// - Requires Player to have tag "Player".
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
        // Ensure trigger.
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // Ensure a Rigidbody exists so triggers are consistent with CharacterController.
        if (!TryGetComponent<Rigidbody>(out var rb))
            rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;

        // Must be player.
        if (!other.CompareTag("Player")) return;

        _collected = true;

        if (GameManager.Instance != null)
            GameManager.Instance.AddSecurityDelta01(securityDelta01);

        // Disable immediately to prevent double triggers.
        if (TryGetComponent<Collider>(out var c)) c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        if (sfx != null)
        {
            sfx.Play();
            Destroy(gameObject, sfx.clip != null ? sfx.clip.length : 0f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}