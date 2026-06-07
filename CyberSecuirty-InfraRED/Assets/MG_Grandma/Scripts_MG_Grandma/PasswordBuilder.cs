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
    [SerializeField] private int maxAttempts = 5;

    [Header("UI")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button clearButton;

    private int attemptsLeft;
    private bool solved;
    private bool locked;
    private bool manualMode;
    private bool updatingInput;

    public bool CanUseDragDrop => !manualMode && IsInputEmpty();

    private void Awake()
    {
        attemptsLeft = maxAttempts;

        if (passwordInput != null)
            passwordInput.onValueChanged.AddListener(HandleManualInputChanged);

        if (submitButton != null)
            submitButton.onClick.AddListener(Submit);

        if (clearButton != null)
            clearButton.onClick.AddListener(Clear);

        RefreshPasswordFromSlots();
        RefreshAttempts();
        SetFeedback("Drag clues or type the password manually.");
    }

    private void OnDestroy()
    {
        if (passwordInput != null)
            passwordInput.onValueChanged.RemoveListener(HandleManualInputChanged);
    }

    public void RefreshPasswordFromSlots()
    {
        if (manualMode)
            return;

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
    }

    private void HandleManualInputChanged(string value)
    {
        if (updatingInput)
            return;

        bool hasTypedText = !string.IsNullOrWhiteSpace(value);

        if (hasTypedText)
        {
            manualMode = true;
            ReturnDraggedCards();

            if (passwordDisplayText != null)
                passwordDisplayText.gameObject.SetActive(false);

            SetFeedback("Manual typing enabled. Clear the field to use dragged clues again.");
            return;
        }

        manualMode = false;

        if (passwordDisplayText != null)
            passwordDisplayText.gameObject.SetActive(true);

        RefreshPasswordFromSlots();
        SetFeedback("Drag mode enabled again.");
    }

    public void Submit()
    {
        if (solved || locked)
            return;

        string password = GetSubmittedPassword();

        if (string.IsNullOrWhiteSpace(password))
        {
            SetFeedback("Enter a password or build one from clues.");
            return;
        }

        if (!manualMode && !AllSlotsFilled())
        {
            SetFeedback("Complete the password with a name, number, and symbol.");
            return;
        }

        if (password == correctPassword)
        {
            solved = true;
            SetFeedback("Access granted. This password was guessed from public information.");
            return;
        }

        attemptsLeft--;
        RefreshAttempts();

        if (attemptsLeft <= 0)
        {
            locked = true;
            SetFeedback("Account locked. Too many wrong attempts.");
            return;
        }

        SetFeedback("Wrong password. Check if the clues belong to the Brightspace admin.");
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
    }

    private string GetSubmittedPassword()
    {
        if (passwordInput != null && !string.IsNullOrWhiteSpace(passwordInput.text))
            return passwordInput.text.Trim();

        if (AllSlotsFilled())
            return BuildPassword();

        return "";
    }

    private void ReturnDraggedCards()
    {
        if (wordSlot != null)
            wordSlot.ClearSlot();

        if (numberSlot != null)
            numberSlot.ClearSlot();

        if (symbolSlot != null)
            symbolSlot.ClearSlot();
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

    private void RefreshAttempts()
    {
        if (attemptsText != null)
            attemptsText.text = "Attempts left: " + attemptsLeft;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }
}