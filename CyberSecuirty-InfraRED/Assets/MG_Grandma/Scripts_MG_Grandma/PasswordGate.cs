using TMPro;
using UnityEngine;

public sealed class SimplePasswordGate : MonoBehaviour
{
    [Header("Password")]
    [SerializeField] private string correctPassword = "Bowie1998!";
    [SerializeField] private int maxAttempts = 5;

    [Header("Required Clues")]
    [SerializeField] private SimpleClueInventory clueInventory;
    [SerializeField] private string[] requiredClueIds;

    [Header("UI")]
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private TMP_Text feedbackText;

    private int attemptsLeft;
    private bool solved;
    private bool locked;

    private void Awake()
    {
        attemptsLeft = maxAttempts;
        RefreshAttempts();
        SetFeedback("Admin access required");
    }

    public void SubmitPassword()
    {
        if (solved || locked)
            return;

        if (!HasRequiredClues())
        {
            SetFeedback("Find the important clues first");
            return;
        }

        string typedPassword = passwordInput != null ? passwordInput.text.Trim() : "";

        if (typedPassword == correctPassword)
        {
            solved = true;
            SetFeedback("Access granted");

            return;
        }

        attemptsLeft--;
        RefreshAttempts();

        if (attemptsLeft <= 0)
        {
            locked = true;
            SetFeedback("Account locked");
            return;
        }

        SetFeedback("Wrong password");
    }

    private bool HasRequiredClues()
    {
        if (clueInventory == null)
            return true;

        foreach (string clueId in requiredClueIds)
        {
            if (!clueInventory.HasClue(clueId))
                return false;
        }

        return true;
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