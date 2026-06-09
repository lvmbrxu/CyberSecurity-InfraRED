using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SimplePasswordBuilder : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private PasswordDropSlot wordSlot;
    [SerializeField] private PasswordDropSlot numberSlot;
    [SerializeField] private PasswordDropSlot symbolSlot;

    [Header("Manual Input")]
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text passwordDisplayText;

    [Header("Password")]
    [SerializeField] private string correctPassword = "Bowie1998!";

    [Header("UI")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button clearButton;

    [Header("Finish (drag your MinigameFinish here)")]
    [SerializeField] private MinigameFinish minigameFinish;

    private bool solved;
    private bool manualMode;
    private bool updatingInput;

    public bool CanUseDragDrop => !manualMode && IsInputEmpty();

    private void Awake()
    {
        if (passwordInput != null)
            passwordInput.onValueChanged.AddListener(HandleManualInputChanged);

        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitClicked);

        if (clearButton != null)
            clearButton.onClick.AddListener(Clear);

        RefreshPasswordFromSlots();
        SetFeedback("Drag clues or type the password manually.");

        UpdateSubmitInteractable();
    }

    private void OnDestroy()
    {
        if (passwordInput != null)
            passwordInput.onValueChanged.RemoveListener(HandleManualInputChanged);
    }

    public void RefreshPasswordFromSlots()
    {
        if (manualMode) return;

        string readablePassword = BuildReadablePassword();

        if (passwordDisplayText != null)
        {
            passwordDisplayText.gameObject.SetActive(!AllSlotsFilled());
            passwordDisplayText.text = readablePassword;
        }

        updatingInput = true;

        if (passwordInput != null)
            passwordInput.text = AllSlotsFilled() ? BuildPassword() : "";

        updatingInput = false;

        UpdateSubmitInteractable();
    }

    private void HandleManualInputChanged(string value)
    {
        if (updatingInput) return;

        bool hasTypedText = !string.IsNullOrWhiteSpace(value);

        if (hasTypedText)
        {
            manualMode = true;
            ReturnDraggedCards();

            if (passwordDisplayText != null)
                passwordDisplayText.gameObject.SetActive(false);

            SetFeedback("Manual typing enabled. Clear the field to use dragged clues again.");
        }
        else
        {
            manualMode = false;

            if (passwordDisplayText != null)
                passwordDisplayText.gameObject.SetActive(true);

            RefreshPasswordFromSlots();
            SetFeedback("Drag mode enabled again.");
        }

        UpdateSubmitInteractable();
    }

    private void OnSubmitClicked()
    {
        if (solved) return;

        // Only allow submit if it's correct (button should already be disabled otherwise)
        if (!IsCurrentPasswordCorrect())
        {
            SetFeedback("Password is not correct yet.");
            UpdateSubmitInteractable();
            return;
        }

        solved = true;
        SetFeedback("Access granted.");

        // Finish the minigame (this triggers scene change / cutscene depending on your setup)
        if (minigameFinish != null)
            minigameFinish.FinishMinigame();
        else
            Debug.LogWarning("SimplePasswordBuilder: MinigameFinish not assigned.");
    }

    public void Clear()
    {
        ReturnDraggedCards();

        updatingInput = true;
        if (passwordInput != null)
            passwordInput.text = "";
        updatingInput = false;

        manualMode = false;

        if (passwordDisplayText != null)
            passwordDisplayText.gameObject.SetActive(true);

        RefreshPasswordFromSlots();
        SetFeedback("Password cleared.");

        UpdateSubmitInteractable();
    }

    private void UpdateSubmitInteractable()
    {
        if (submitButton == null) return;

        // Submit only when correct + not already solved
        submitButton.interactable = !solved && IsCurrentPasswordCorrect();
    }

    private bool IsCurrentPasswordCorrect()
    {
        string password = GetCurrentPassword();
        if (string.IsNullOrWhiteSpace(password)) return false;

        // If using drag mode, require all 3 slots filled
        if (!manualMode && !AllSlotsFilled()) return false;

        return password == correctPassword;
    }

    private string GetCurrentPassword()
    {
        if (passwordInput != null && !string.IsNullOrWhiteSpace(passwordInput.text))
            return passwordInput.text.Trim();

        if (AllSlotsFilled())
            return BuildPassword();

        return "";
    }

    private void ReturnDraggedCards()
    {
        if (wordSlot != null) wordSlot.ClearSlot();
        if (numberSlot != null) numberSlot.ClearSlot();
        if (symbolSlot != null) symbolSlot.ClearSlot();
    }

    private bool AllSlotsFilled()
    {
        return wordSlot != null && wordSlot.HasValue
            && numberSlot != null && numberSlot.HasValue
            && symbolSlot != null && symbolSlot.HasValue;
    }

    private bool IsInputEmpty()
    {
        return passwordInput == null || string.IsNullOrWhiteSpace(passwordInput.text);
    }

    private string BuildPassword()
    {
        string word = wordSlot != null ? wordSlot.CurrentValue : "";
        string number = numberSlot != null ? numberSlot.CurrentValue : "";
        string symbol = symbolSlot != null ? symbolSlot.CurrentValue : "";
        return word + number + symbol;
    }

    private string BuildReadablePassword()
    {
        string word = wordSlot != null && wordSlot.HasValue ? wordSlot.CurrentValue : "Name";
        string number = numberSlot != null && numberSlot.HasValue ? numberSlot.CurrentValue : "Number";
        string symbol = symbolSlot != null && symbolSlot.HasValue ? symbolSlot.CurrentValue : "Symbol";
        return word + " + " + number + " + " + symbol;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }
}