using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PasswordDropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public event Action OnSlotChanged;

    [Header("Slot")]
    [SerializeField] private PasswordClueType acceptedType;

    [Header("Highlight")]
    [SerializeField] private Image highlightImage;
    [SerializeField] private float hoverAlpha = 0.18f;

    private DraggableClueCard currentCard;

    public string CurrentValue => currentCard != null ? currentCard.PasswordValue : "";
    public bool HasValue => currentCard != null;

    private void Awake()
    {
        SetHighlight(0f);
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableClueCard card = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<DraggableClueCard>()
            : null;

        if (card == null || !card.UsableForPassword)
            return;

        if (card.ClueType != acceptedType)
        {
            card.ReturnToLibrary();
            return;
        }

        ClearSlot();

        currentCard = card;
        currentCard.MarkAcceptedBySlot();
        currentCard.HideInSlot();

        SetHighlight(0f);
        OnSlotChanged?.Invoke();
    }

    public void ClearSlot()
    {
        if (currentCard != null)
        {
            currentCard.ReturnToLibrary();
            currentCard = null;
        }

        OnSlotChanged?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlight(hoverAlpha);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(0f);
    }

    private void SetHighlight(float alpha)
    {
        if (highlightImage == null)
            return;

        Color color = highlightImage.color;
        color.a = alpha;
        highlightImage.color = color;
    }
}