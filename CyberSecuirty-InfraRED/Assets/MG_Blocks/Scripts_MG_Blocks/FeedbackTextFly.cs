using UnityEngine;
using TMPro;

public sealed class FeedbackTextFly : MonoBehaviour
{
    public float lifetime = 0.9f;
    public float rise = 80f;
    public AnimationCurve alpha = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public AnimationCurve scale = AnimationCurve.EaseInOut(0, 1.2f, 1, 1f);

    TMP_Text tmp;
    RectTransform rt;
    Vector2 start;
    float t;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        rt = GetComponent<RectTransform>();
        start = rt.anchoredPosition;
    }

    public void SetText(string s)
    {
        if (tmp) tmp.text = s;
    }

    void Update()
    {
        t += Time.deltaTime;
        float u = lifetime <= 0.001f ? 1f : Mathf.Clamp01(t / lifetime);

        var p = start;
        p.y += rise * u;
        rt.anchoredPosition = p;

        rt.localScale = Vector3.one * scale.Evaluate(u);

        if (tmp)
        {
            var c = tmp.color;
            c.a = alpha.Evaluate(u);
            tmp.color = c;
        }

        if (u >= 1f)
            Destroy(gameObject);
    }
}