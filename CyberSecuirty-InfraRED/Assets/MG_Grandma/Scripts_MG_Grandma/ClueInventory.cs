using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class SimpleClueInventory : MonoBehaviour
{
    private readonly Dictionary<string, DraggableClueCard> spawnedCards = new();

    [Header("Library")]
    [SerializeField] private Transform clueLibraryParent;
    [SerializeField] private DraggableClueCard clueCardPrefab;

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;

    public bool HasClue(string clueId)
    {
        return spawnedCards.ContainsKey(clueId);
    }

    public void AddClue(string clueId, string clueText, string passwordValue, PasswordClueType clueType, bool usableForPassword)
    {
        if (string.IsNullOrWhiteSpace(clueId))
            return;

        if (spawnedCards.ContainsKey(clueId))
        {
            SetFeedback("You already found this clue");
            return;
        }

        DraggableClueCard card = Instantiate(clueCardPrefab, clueLibraryParent);
        card.Setup(clueId, clueText, passwordValue, clueType, usableForPassword);

        spawnedCards.Add(clueId, card);

        if (usableForPassword)
            SetFeedback("Password clue found: " + clueText);
        else
            SetFeedback("Info found: " + clueText);
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }
}