using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PasswordDropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Slot")]
    [SerializeField] private PasswordClueType acceptedType;

    [Header("Password Builder")]
    [SerializeField] private SimplePasswordBuilder passwordBuilder;
    [SerializeField] private TMP_InputField passwordInput;

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

        if (passwordBuilder != null && !passwordBuilder.CanUseDragDrop)
        {
            card.ReturnToLibrary();
            return;
        }

        if (card.ClueType != acceptedType)
        {
            card.ReturnToLibrary();
            return;
        }

        ClearSlot(false);

        currentCard = card;
        currentCard.MarkAcceptedBySlot();
        currentCard.HideInSlot();

        SetHighlight(0f);

        if (passwordBuilder != null)
            passwordBuilder.RefreshPasswordFromSlots();
    }

    public void ClearSlot()
    {
        ClearSlot(true);
    }

    private void ClearSlot(bool notifyBuilder)
    {
        if (currentCard != null)
        {
            currentCard.ReturnToLibrary();
            currentCard = null;
        }

        if (notifyBuilder && passwordBuilder != null)
            passwordBuilder.RefreshPasswordFromSlots();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (passwordInput == null)
            return;

        passwordInput.Select();
        passwordInput.ActivateInputField();
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