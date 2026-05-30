using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class SimpleClueInventory : MonoBehaviour
{
    private readonly Dictionary<string, string> collectedClues = new();

    [Header("UI")]
    [SerializeField] private TMP_Text clueListText;
    [SerializeField] private TMP_Text feedbackText;

    public bool HasClue(string clueId)
    {
        return collectedClues.ContainsKey(clueId);
    }

    public void AddClue(string clueId, string clueText)
    {
        if (string.IsNullOrWhiteSpace(clueId))
            return;

        if (collectedClues.ContainsKey(clueId))
        {
            SetFeedback("You already found this clue");
            return;
        }

        collectedClues.Add(clueId, clueText);

        RefreshClueList();
        SetFeedback("Clue found: " + clueText);
    }

    private void RefreshClueList()
    {
        if (clueListText == null)
            return;

        clueListText.text = "";

        foreach (string clue in collectedClues.Values)
            clueListText.text += "• " + clue + "\n";
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }
}