using UnityEngine;

public class DialogueTriggerZone : MonoBehaviour
{
    [Header("What happens when player enters")]
    public DialogueUI dialogueUI;
    public DialogueDataSO dialogueData;

    [Header("Behavior")]
    public bool triggerOnce = true;
    public bool disableAfterTrigger = true;

    private bool hasTriggered;

    private void Reset()
    {
        // Helps prevent trigger events failing due to missing setup
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (dialogueUI == null || dialogueData == null) return;
        if (dialogueUI.IsOpen) return;

        // Only react to player
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;
        dialogueUI.Open(dialogueData);

        if (disableAfterTrigger)
            gameObject.SetActive(false); // clean: stops re-triggering forever
    }
}