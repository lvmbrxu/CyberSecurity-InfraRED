using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PasswordDropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot")]
    [SerializeField] private PasswordClueType acceptedType;

    [Header("UI")]
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Image highlightImage;

    [Header("Highlight")]
    [SerializeField] private float hoverAlpha = 0.2f;

    private DraggableClueCard currentCard;

    public string CurrentValue => currentCard != null ? currentCard.PasswordValue : "";

    private void Awake()
    {
        SetHighlight(0f);

        if (valueText != null)
            valueText.text = "";
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

        currentCard = card;

        if (valueText != null)
            valueText.text = card.PasswordValue;

        card.gameObject.SetActive(false);
        SetHighlight(0f);
    }

    public void ClearSlot()
    {
        if (currentCard != null)
        {
            currentCard.gameObject.SetActive(true);
            currentCard.ReturnToLibrary();
        }

        currentCard = null;

        if (valueText != null)
            valueText.text = "";
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