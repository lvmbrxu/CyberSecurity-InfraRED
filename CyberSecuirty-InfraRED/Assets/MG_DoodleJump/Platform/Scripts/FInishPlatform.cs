// FInishPlatform.cs
using UnityEngine;

/// <summary>
/// Legacy finish trigger (optional).
/// In the new security-driven flow, the level ends when Security hits 100%.
/// Keep this in case you still want a physical finish trigger.
/// </summary>
[DisallowMultipleComponent]
public sealed class FInishPlatform : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameManager.Instance?.Win();
    }
}