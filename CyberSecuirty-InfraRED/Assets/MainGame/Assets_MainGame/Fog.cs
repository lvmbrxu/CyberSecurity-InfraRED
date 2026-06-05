using UnityEngine;

[DisallowMultipleComponent]
public class FogSphereApplier : MonoBehaviour
{
    [Header("Shader")]
    public Shader fogShader; // assign "Custom/FogSphereDepth_URP" or leave empty to auto-find

    [Header("Fog Look")]
    public Color fogColor = new Color(0.75f, 0.78f, 0.82f, 1f);
    [Range(0f, 4f)] public float density = 1.0f;

    public float fogStart = 40f;
    public float fogEnd = 180f;

    public float bottomY = 0f;
    public float topY = 30f;

    [Header("Noise")]
    public Texture2D noiseTex;
    [Range(0.001f, 1f)] public float noiseScale = 0.05f;
    [Range(0f, 2f)] public float noiseStrength = 1f;
    public Vector2 noiseSpeed = new Vector2(0.02f, 0.01f);

    Material _mat;

    static readonly int FogColorID = Shader.PropertyToID("_FogColor");
    static readonly int DensityID = Shader.PropertyToID("_Density");
    static readonly int FogStartID = Shader.PropertyToID("_FogStart");
    static readonly int FogEndID = Shader.PropertyToID("_FogEnd");
    static readonly int BottomYID = Shader.PropertyToID("_BottomY");
    static readonly int TopYID = Shader.PropertyToID("_TopY");
    static readonly int NoiseTexID = Shader.PropertyToID("_NoiseTex");
    static readonly int NoiseScaleID = Shader.PropertyToID("_NoiseScale");
    static readonly int NoiseStrengthID = Shader.PropertyToID("_NoiseStrength");
    static readonly int NoiseSpeedID = Shader.PropertyToID("_NoiseSpeed");

    void Reset()
    {
        // Reasonable defaults if you drop it on an object
        fogStart = 40f;
        fogEnd = 180f;
        density = 1f;
    }

    void OnEnable()
    {
        var r = GetComponent<Renderer>();
        if (!r)
        {
            Debug.LogError("FogSphereApplier needs a Renderer on the same GameObject.");
            enabled = false;
            return;
        }

        if (fogShader == null)
            fogShader = Shader.Find("Custom/FogSphereDepth_URP");

        if (fogShader == null)
        {
            Debug.LogError("Fog shader not found. Did you create Custom/FogSphereDepth_URP?");
            enabled = false;
            return;
        }

        // Use a dedicated material instance for this sphere
        _mat = new Material(fogShader)
        {
            name = "M_FogSphere_Runtime"
        };
        r.sharedMaterial = _mat;

        // No shadows for fog
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        ApplyParams();
    }

    void OnValidate()
    {
        if (_mat) ApplyParams();
    }

    void ApplyParams()
    {
        _mat.SetColor(FogColorID, fogColor);
        _mat.SetFloat(DensityID, density);
        _mat.SetFloat(FogStartID, fogStart);
        _mat.SetFloat(FogEndID, fogEnd);
        _mat.SetFloat(BottomYID, bottomY);
        _mat.SetFloat(TopYID, topY);

        if (noiseTex) _mat.SetTexture(NoiseTexID, noiseTex);
        _mat.SetFloat(NoiseScaleID, noiseScale);
        _mat.SetFloat(NoiseStrengthID, noiseStrength);
        _mat.SetVector(NoiseSpeedID, new Vector4(noiseSpeed.x, noiseSpeed.y, 0, 0));
    }

    void OnDisable()
    {
        if (_mat)
        {
            // Prevent leaks in edit mode / play mode transitions
            if (Application.isPlaying) Destroy(_mat);
            else DestroyImmediate(_mat);
        }
    }
}