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

    [Header("UI")]
    [SerializeField] private TMP_Text builtPasswordText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button clearButton;

    private void Awake()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(Submit);

        if (clearButton != null)
            clearButton.onClick.AddListener(Clear);

        RefreshBuiltPassword();
    }

    public void Submit()
    {
        string password = BuildPassword();

        if (string.IsNullOrWhiteSpace(password))
        {
            SetFeedback("Build the password first");
            return;
        }

        if (password == correctPassword)
        {
            SetFeedback("Access granted. The password was guessed from public information.");
            return;
        }

        SetFeedback("Wrong password. Try using clues from the correct person.");
    }

    public void Clear()
    {
        if (wordSlot != null)
            wordSlot.ClearSlot();

        if (numberSlot != null)
            numberSlot.ClearSlot();

        if (symbolSlot != null)
            symbolSlot.ClearSlot();

        RefreshBuiltPassword();
        SetFeedback("Password cleared");
    }

    private string BuildPassword()
    {
        string word = wordSlot != null ? wordSlot.CurrentValue : "";
        string number = numberSlot != null ? numberSlot.CurrentValue : "";
        string symbol = symbolSlot != null ? symbolSlot.CurrentValue : "";

        string password = word + number + symbol;

        if (builtPasswordText != null)
            builtPasswordText.text = password;

        return password;
    }

    private void RefreshBuiltPassword()
    {
        if (builtPasswordText != null)
            builtPasswordText.text = BuildPassword();
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }
}