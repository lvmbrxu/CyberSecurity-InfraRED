using UnityEngine;
using UnityEngine.UI;

public sealed class ClueFlyToUI : MonoBehaviour
{
    public RectTransform uiTarget;     // set by manager
    public Canvas canvas;              // set by manager
    public Camera worldCamera;         // set by manager
    public float travelTime = 0.45f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float arcHeight = 60f;      // in canvas pixels

    RectTransform rt;
    Vector2 start;
    Vector2 end;
    float t;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    public void Init(Vector3 worldStart)
    {
        // world -> screen -> canvas local
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(worldCamera, worldStart);
        RectTransform canvasRect = canvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera, out start);

        // target local point
        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(worldCamera, uiTarget.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreen, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera, out end);

        rt.anchoredPosition = start;
    }

    void Update()
    {
        if (!uiTarget || !canvas) { Destroy(gameObject); return; }

        t += Time.deltaTime;
        float u = travelTime <= 0.001f ? 1f : Mathf.Clamp01(t / travelTime);
        float e = ease.Evaluate(u);

        // Arc
        Vector2 p = Vector2.Lerp(start, end, e);
        float arc = Mathf.Sin(e * Mathf.PI) * arcHeight;
        p.y += arc;

        rt.anchoredPosition = p;

        if (u >= 1f)
            Destroy(gameObject);
    }
}