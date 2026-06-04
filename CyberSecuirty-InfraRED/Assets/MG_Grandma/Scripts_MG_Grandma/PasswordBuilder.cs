using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SimplePasswordBuilder : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private PasswordDropSlot wordSlot;
    [SerializeField] private PasswordDropSlot numberSlot;
    [SerializeField] private PasswordDropSlot symbolSlot;

    [Header("Password")]
    [SerializeField] private string correctPassword = "Bowie1998!";
    [SerializeField] private int maxAttempts = 5;

    [Header("UI")]
    [SerializeField] private TMP_Text passwordFieldText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button clearButton;

    private int attemptsLeft;
    private bool solved;
    private bool locked;

    private void Awake()
    {
        attemptsLeft = maxAttempts;

        if (wordSlot != null)
            wordSlot.OnSlotChanged += RefreshPasswordField;

        if (numberSlot != null)
            numberSlot.OnSlotChanged += RefreshPasswordField;

        if (symbolSlot != null)
            symbolSlot.OnSlotChanged += RefreshPasswordField;

        if (submitButton != null)
            submitButton.onClick.AddListener(Submit);

        if (clearButton != null)
            clearButton.onClick.AddListener(Clear);

        RefreshPasswordField();
        RefreshAttempts();
        SetFeedback("Drag clues into the password field.");
    }

    private void OnDestroy()
    {
        if (wordSlot != null)
            wordSlot.OnSlotChanged -= RefreshPasswordField;

        if (numberSlot != null)
            numberSlot.OnSlotChanged -= RefreshPasswordField;

        if (symbolSlot != null)
            symbolSlot.OnSlotChanged -= RefreshPasswordField;
    }

    public void Submit()
    {
        if (solved || locked)
            return;

        if (!AllSlotsFilled())
        {
            SetFeedback("Complete the password with a name, number, and symbol.");
            return;
        }

        string password = BuildPassword();

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
        if (wordSlot != null)
            wordSlot.ClearSlot();

        if (numberSlot != null)
            numberSlot.ClearSlot();

        if (symbolSlot != null)
            symbolSlot.ClearSlot();

        RefreshPasswordField();
        SetFeedback("Password cleared.");
    }

    private bool AllSlotsFilled()
    {
        return wordSlot != null && wordSlot.HasValue
            && numberSlot != null && numberSlot.HasValue
            && symbolSlot != null && symbolSlot.HasValue;
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

    private void RefreshPasswordField()
    {
        if (passwordFieldText == null)
            return;

        passwordFieldText.text = AllSlotsFilled()
            ? BuildPassword()
            : BuildReadablePassword();
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