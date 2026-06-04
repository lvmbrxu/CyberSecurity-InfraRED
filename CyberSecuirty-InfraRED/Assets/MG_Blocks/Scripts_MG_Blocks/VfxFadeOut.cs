using UnityEngine;

public sealed class VfxFadeOut : MonoBehaviour
{
    Material runtimeMat;
    float lifetime;
    float baseAlpha;
    float t;

    public void Init(Material sourceMat, float lifetime, float baseAlpha)
    {
        runtimeMat = new Material(sourceMat);
        this.lifetime = Mathf.Max(0.01f, lifetime);
        this.baseAlpha = Mathf.Clamp01(baseAlpha);

        var mr = GetComponent<MeshRenderer>();
        if (mr) mr.material = runtimeMat;

        SetAlpha(this.baseAlpha);
    }

    void Update()
    {
        t += Time.deltaTime;
        float u = Mathf.Clamp01(t / lifetime);
        SetAlpha(Mathf.Lerp(baseAlpha, 0f, u));
    }

    void SetAlpha(float a)
    {
        if (!runtimeMat) return;

        if (runtimeMat.HasProperty("_BaseColor"))
        {
            Color c = runtimeMat.GetColor("_BaseColor");
            c.a = a;
            runtimeMat.SetColor("_BaseColor", c);
        }
        if (runtimeMat.HasProperty("_Color"))
        {
            Color c = runtimeMat.GetColor("_Color");
            c.a = a;
            runtimeMat.SetColor("_Color", c);
        }
    }
}