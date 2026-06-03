using UnityEngine;
using TMPro;

public sealed class ComboPop : MonoBehaviour
{
    public float popScale = 1.25f;
    public float popTime = 0.12f;
    public float returnTime = 0.10f;

    Vector3 baseScale;
    float t;
    int phase; // 0 idle, 1 popping up, 2 returning

    void Awake()
    {
        baseScale = transform.localScale;
    }

    public void Trigger()
    {
        t = 0f;
        phase = 1;
    }

    void Update()
    {
        if (phase == 0) return;

        t += Time.deltaTime;

        if (phase == 1)
        {
            float u = popTime <= 0.001f ? 1f : Mathf.Clamp01(t / popTime);
            transform.localScale = Vector3.Lerp(baseScale, baseScale * popScale, u);
            if (u >= 1f)
            {
                phase = 2;
                t = 0f;
            }
        }
        else
        {
            float u = returnTime <= 0.001f ? 1f : Mathf.Clamp01(t / returnTime);
            transform.localScale = Vector3.Lerp(baseScale * popScale, baseScale, u);
            if (u >= 1f)
            {
                transform.localScale = baseScale;
                phase = 0;
            }
        }
    }
}