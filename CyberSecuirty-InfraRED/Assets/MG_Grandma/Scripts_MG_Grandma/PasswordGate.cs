using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SubmitGate : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button submitButton;

    [Header("What happens on successful submit")]
    [SerializeField] private MinigameFinish minigameFinish; // drag the same object you already use

    [Header("Optional")]
    [SerializeField] private bool startDisabled = true;

    private bool submitted;

    private void Awake()
    {
        if (submitButton == null)
            submitButton = GetComponent<Button>();

        submitButton.onClick.AddListener(OnSubmitClicked);

        if (startDisabled)
            SetSubmitEnabled(false);
    }

    /// <summary>
    /// Call this from your minigame logic when the solution is correct/valid.
    /// </summary>
    public void SetSubmitEnabled(bool enabled)
    {
        if (submitButton != null)
            submitButton.interactable = enabled;
    }

    private void OnSubmitClicked()
    {
        // Hard guard against spam / double-click
        if (submitted) return;
        submitted = true;

        // Lock button immediately
        SetSubmitEnabled(false);

        // Only proceed if we have a finisher
        if (minigameFinish != null)
            minigameFinish.FinishMinigame();
        else
            Debug.LogWarning("SubmitGate: MinigameFinish reference not set.");
    }
}