using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TooltipController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    [Header("Position")]
    [SerializeField] private Vector2 offset = new Vector2(25f, -35f);

    private RectTransform canvasRect;

    private void Awake()
    {
        if (targetCanvas != null)
            canvasRect = targetCanvas.transform as RectTransform;

        Hide();
    }

    public void Show(string message)
    {
        if (tooltipPanel == null || tooltipText == null)
            return;

        tooltipText.text = message;
        tooltipPanel.gameObject.SetActive(true);

        PlaceTooltipOnce();
    }

    public void Hide()
    {
        if (tooltipPanel != null)
            tooltipPanel.gameObject.SetActive(false);
    }

    private void PlaceTooltipOnce()
    {
        if (targetCanvas == null || canvasRect == null || tooltipPanel == null)
            return;

        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            mousePosition,
            targetCanvas.worldCamera,
            out Vector2 localPoint
        );

        tooltipPanel.anchoredPosition = localPoint + offset;
    }
}