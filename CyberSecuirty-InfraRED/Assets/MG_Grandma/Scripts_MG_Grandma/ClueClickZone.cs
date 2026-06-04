using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SimpleClueClickZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Tooltip")]
    [SerializeField] private TooltipController tooltipController;
    [TextArea]
    [SerializeField] private string tooltipText;

    [Header("Highlight")]
    [SerializeField] private Image highlightImage;
    [SerializeField] private float hoverAlpha = 0.22f;
    [SerializeField] private float foundAlpha = 0.12f;

    [Header("Collectable Clue")]
    [SerializeField] private bool canCollectClue;
    [SerializeField] private string clueId;
    [SerializeField] private string clueText;
    [SerializeField] private string passwordValue;
    [SerializeField] private PasswordClueType clueType;
    [SerializeField] private bool usableForPassword = true;
    [SerializeField] private SimpleClueInventory clueInventory;

    private bool collected;

    private void Awake()
    {
        SetHighlight(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!collected)
            SetHighlight(hoverAlpha);

        if (tooltipController != null && !string.IsNullOrWhiteSpace(tooltipText))
            tooltipController.Show(tooltipText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(collected ? foundAlpha : 0f);

        if (tooltipController != null)
            tooltipController.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canCollectClue || collected || clueInventory == null)
            return;

        clueInventory.AddClue(clueId, clueText, passwordValue, clueType, usableForPassword);

        collected = true;
        SetHighlight(foundAlpha);
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