using UnityEngine;

public sealed class VFX_SweepFade : MonoBehaviour
{
    [Header("Lifetime")]
    public float lifetime = 0.22f;

    [Header("Visual")]
    public AnimationCurve alpha = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public AnimationCurve thickness = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Tooltip("If your material uses _BaseColor (URP Lit/Unlit), this will work too.")]
    public string colorPropertyA = "_BaseColor";
    public string colorPropertyB = "_Color";

    Renderer rend;
    MaterialPropertyBlock mpb;
    int propIdA, propIdB;

    float t;
    Vector3 baseScale;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        propIdA = Shader.PropertyToID(colorPropertyA);
        propIdB = Shader.PropertyToID(colorPropertyB);
        baseScale = transform.localScale;
    }

    void Update()
    {
        t += Time.deltaTime;
        float u = lifetime <= 0.001f ? 1f : Mathf.Clamp01(t / lifetime);

        // Thickness pulse (uses local Y)
        var s = baseScale;
        s.y = Mathf.Max(0.001f, baseScale.y * thickness.Evaluate(u));
        transform.localScale = s;

        // Fade alpha
        if (rend)
        {
            rend.GetPropertyBlock(mpb);

            // get current color from shared material (fallback)
            Color c = Color.white;
            if (rend.sharedMaterial)
            {
                if (rend.sharedMaterial.HasProperty(propIdA)) c = rend.sharedMaterial.GetColor(propIdA);
                else if (rend.sharedMaterial.HasProperty(propIdB)) c = rend.sharedMaterial.GetColor(propIdB);
            }

            c.a *= alpha.Evaluate(u);

            if (rend.sharedMaterial && rend.sharedMaterial.HasProperty(propIdA))
                mpb.SetColor(propIdA, c);
            else
                mpb.SetColor(propIdB, c);

            rend.SetPropertyBlock(mpb);
        }

        if (u >= 1f)
            Destroy(gameObject);
    }
}