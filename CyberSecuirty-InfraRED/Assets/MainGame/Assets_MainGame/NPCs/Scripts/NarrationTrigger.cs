using UnityEngine;

public class NarrationTrigger : MonoBehaviour
{
    [Header("Event Trigger (SaveManager)")]
    [SerializeField] private string triggerEventId = "INTRO";

    [Header("Dialogue")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private NarrationSequenceSO sequence;

    [Header("Trigger Behavior")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool disableAfterTrigger = true;

    private bool triggered;

    private void Start()
    {
        // Ensure data is loaded before checking
        SaveManager.EnsureLoaded();
        TryTrigger();
    }

    private void TryTrigger()
    {
        if (triggerOnce && triggered) return;

        string key = string.IsNullOrWhiteSpace(triggerEventId) ? "" : triggerEventId.Trim();
        if (string.IsNullOrEmpty(key)) return;

        if (!SaveManager.HasMainEvent(key))
        {
            Debug.Log($"[NarrationTrigger] Event '{key}' not found.");
            return;
        }

        Debug.Log($"[NarrationTrigger] Event '{key}' found -> playing narration");

        triggered = true;

        // consume the event so it plays once
        SaveManager.RemoveMainEvent(key);

        if (dialogueUI != null && sequence != null)
            dialogueUI.OpenNarration(sequence);
        else
            Debug.LogError("[NarrationTrigger] dialogueUI or sequence missing.");

        if (disableAfterTrigger)
            gameObject.SetActive(false);
    }
}