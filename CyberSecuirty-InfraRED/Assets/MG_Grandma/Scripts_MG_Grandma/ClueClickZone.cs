using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SimpleClueClickZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Clue")]
    [SerializeField] private string clueId;
    [SerializeField] private string clueText;

    [Header("References")]
    [SerializeField] private SimpleClueInventory clueInventory;
    [SerializeField] private Image hoverImage;

    [Header("Hover")]
    [SerializeField] private float hoverAlpha = 0.18f;

    private bool collected;

    private void Awake()
    {
        SetHover(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!collected)
            SetHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHover(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Collect();
    }

    private void Collect()
    {
        if (collected || clueInventory == null)
            return;

        clueInventory.AddClue(clueId, clueText);
        collected = true;

        SetHover(false);
    }

    private void SetHover(bool visible)
    {
        if (hoverImage == null)
            return;

        Color color = hoverImage.color;
        color.a = visible ? hoverAlpha : 0f;
        hoverImage.color = color;
    }
}