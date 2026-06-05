using UnityEngine;

public class NarrationTrigger_CameraSwap_CM3 : MonoBehaviour
{
    public DialogueUI dialogueUI;
    public NarrationSequenceSO sequence;

    [Header("Camera swap (CM3)")]
    public bool swapCamera = true;
    public CameraPrioritySwap_CM3 cameraSwap;

    [Header("Trigger Behavior")]
    public bool triggerOnce = true;
    public bool disableAfterTrigger = true;

    private bool triggered;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && triggered) return;
        if (!other.CompareTag("Player")) return;

        if (dialogueUI == null || sequence == null) return;
        if (dialogueUI.IsOpen) return;

        triggered = true;

        if (swapCamera && cameraSwap != null)
            cameraSwap.ActivateTargetCamera();

        dialogueUI.Closed -= OnDialogueClosed;
        dialogueUI.Closed += OnDialogueClosed;

        dialogueUI.OpenNarration(sequence);

        if (disableAfterTrigger)
            gameObject.SetActive(false);
    }

    private void OnDialogueClosed()
    {
        if (dialogueUI != null)
            dialogueUI.Closed -= OnDialogueClosed;

        if (swapCamera && cameraSwap != null)
            cameraSwap.Restore();
    }
}