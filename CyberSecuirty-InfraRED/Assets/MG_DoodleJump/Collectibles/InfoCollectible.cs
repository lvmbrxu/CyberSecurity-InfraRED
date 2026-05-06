// InfoCollectible.cs
using UnityEngine;

/// <summary>
/// Security pickup.
/// Positive delta => Security+
/// Negative delta => Security-
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class InfoCollectible : MonoBehaviour
{
    [Tooltip("+0.05 = +5%, -0.10 = -10%")]
    [SerializeField] private float securityDelta01 = 0.05f;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null) return;

        var player = GameManager.Instance.GetComponent<GameManager>().GetComponent<GameManager>(); // no-op safety (keeps designer from wiring wrong object)
        // Correct player check:
        var p = FindFirstObjectByType<PlayerController>();
        if (!p || other.transform != p.transform) return;

        GameManager.Instance.AddSecurityDelta01(securityDelta01);
        Destroy(gameObject);
    }
}