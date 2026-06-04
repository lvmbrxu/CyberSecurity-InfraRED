using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum PasswordClueType
{
    Word,
    Number,
    Symbol,
    Info
}

public sealed class DraggableClueCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image background;

    [Header("Visual")]
    [SerializeField] private Color usableColor = Color.white;
    [SerializeField] private Color infoColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private Transform libraryParent;
    private bool acceptedBySlot;

    public string PasswordValue { get; private set; }
    public PasswordClueType ClueType { get; private set; }
    public bool UsableForPassword { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(string clueId, string clueText, string passwordValue, PasswordClueType clueType, bool usableForPassword)
    {
        PasswordValue = passwordValue;
        ClueType = clueType;
        UsableForPassword = usableForPassword;
        libraryParent = transform.parent;

        if (labelText != null)
            labelText.text = clueText;

        if (background != null)
            background.color = usableForPassword ? usableColor : infoColor;

        ResetRaycast();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!UsableForPassword)
            return;

        acceptedBySlot = false;

        if (libraryParent == null)
            libraryParent = transform.parent;

        transform.SetParent(parentCanvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!UsableForPassword || parentCanvas == null)
            return;

        RectTransform canvasRect = parentCanvas.transform as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            parentCanvas.worldCamera,
            out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!UsableForPassword)
            return;

        ResetRaycast();

        if (!acceptedBySlot)
            ReturnToLibrary();
    }

    public void MarkAcceptedBySlot()
    {
        acceptedBySlot = true;
        ResetRaycast();
    }

    public void HideInSlot()
    {
        ResetRaycast();
        gameObject.SetActive(false);
    }

    public void ReturnToLibrary()
    {
        gameObject.SetActive(true);
        ResetRaycast();

        if (libraryParent != null)
            transform.SetParent(libraryParent, false);

        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.anchoredPosition = Vector2.zero;

        transform.SetAsLastSibling();

        if (libraryParent is RectTransform parentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    private void ResetRaycast()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}