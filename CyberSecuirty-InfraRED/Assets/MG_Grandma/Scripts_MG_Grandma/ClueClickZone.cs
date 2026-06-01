using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SimpleClueClickZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Clue")]
    [SerializeField] private string clueId;
    [SerializeField] private string clueText;
    [SerializeField] private string passwordValue;
    [SerializeField] private PasswordClueType clueType;
    [SerializeField] private bool usableForPassword = true;

    [Header("References")]
    [SerializeField] private SimpleClueInventory clueInventory;
    [SerializeField] private Image highlightImage;

    [Header("Highlight")]
    [SerializeField] private float hoverAlpha = 0.22f;
    [SerializeField] private float foundAlpha = 0.12f;

    private bool collected;

    private void Awake()
    {
        SetHighlight(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!collected)
            SetHighlight(hoverAlpha);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(collected ? foundAlpha : 0f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (collected || clueInventory == null)
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