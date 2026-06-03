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
    private Transform startParent;
    private Vector2 startPosition;

    public string ClueId { get; private set; }
    public string PasswordValue { get; private set; }
    public PasswordClueType ClueType { get; private set; }
    public bool UsableForPassword { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(string clueId, string clueText, string passwordValue, PasswordClueType clueType, bool usableForPassword)
    {
        ClueId = clueId;
        PasswordValue = passwordValue;
        ClueType = clueType;
        UsableForPassword = usableForPassword;

        if (labelText != null)
            labelText.text = clueText;

        if (background != null)
            background.color = usableForPassword ? usableColor : infoColor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!UsableForPassword)
            return;

        startParent = transform.parent;
        startPosition = rectTransform.anchoredPosition;

        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!UsableForPassword)
            return;

        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!UsableForPassword)
            return;

        canvasGroup.blocksRaycasts = true;

        if (transform.parent == transform.root)
            ReturnToLibrary();
    }

    public void ReturnToLibrary()
    {
        transform.SetParent(startParent);
        rectTransform.anchoredPosition = startPosition;
    }
}