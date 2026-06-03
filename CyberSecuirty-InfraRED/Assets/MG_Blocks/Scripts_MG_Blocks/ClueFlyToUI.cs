using System;
using UnityEngine;

public sealed class ClueFlyToUI : MonoBehaviour
{
    public RectTransform uiTarget;
    public Canvas canvas;
    public Camera worldCamera;

    public float travelTime = 0.45f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float arcHeight = 60f;

    public Action OnArrive;

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
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(worldCamera, worldStart);
        RectTransform canvasRect = canvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screen,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            out start);

        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(worldCamera, uiTarget.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetScreen,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            out end);

        rt.anchoredPosition = start;
    }

    void Update()
    {
        if (!uiTarget || !canvas) { Destroy(gameObject); return; }

        t += Time.deltaTime;
        float u = travelTime <= 0.001f ? 1f : Mathf.Clamp01(t / travelTime);
        float e = ease.Evaluate(u);

        Vector2 p = Vector2.Lerp(start, end, e);
        p.y += Mathf.Sin(e * Mathf.PI) * arcHeight;
        rt.anchoredPosition = p;

        if (u >= 1f)
        {
            OnArrive?.Invoke();
            Destroy(gameObject);
        }
    }
}