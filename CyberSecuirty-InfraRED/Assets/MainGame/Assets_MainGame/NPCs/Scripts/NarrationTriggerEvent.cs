using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class EnqueueNarrationEventOnTrigger : MonoBehaviour
{
    [SerializeField] private string eventId = "end_narrator";
    [SerializeField] private bool once = true;

    private bool fired;

    private void Awake()
    {
        // Make this object a proper trigger source (CharacterController-safe)
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (once && fired) return;

        // Works with CharacterController player
        if (!other.CompareTag("Player")) return;

        fired = true;

        Debug.Log($"[EnqueueNarrationEventOnTrigger] Enqueue '{eventId}'", this);
        SaveManager.EnqueueMainEvent(eventId);
    }
}