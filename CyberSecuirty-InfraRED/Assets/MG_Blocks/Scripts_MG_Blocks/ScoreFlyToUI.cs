using System;
using UnityEngine;
using TMPro;

public sealed class ScoreFlyToUI : MonoBehaviour
{
    public RectTransform uiTarget;
    public Canvas canvas;
    public Camera worldCamera;

    [Header("Motion")]
    public float travelTime = 0.9f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float arcHeight = 70f;

    public Action OnArrive; 

    TMP_Text tmp;
    RectTransform rt;
    RectTransform canvasRect;

    Vector2 start;
    Vector2 end;
    float t;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        rt = GetComponent<RectTransform>();
    }

    public void Init(Vector3 worldStart, string text)
    {
        tmp.text = text;

        canvasRect = canvas.transform as RectTransform;

        // start = world -> canvas local
        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(worldCamera, worldStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, startScreen,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            out start);

        // end = target -> canvas local
        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
            uiTarget.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, targetScreen,
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