using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraBackgroundSwap : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material phase1Material;
    [SerializeField] private Material phase2Material;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        SetPhase1();
    }

    public void SetPhase1()
    {
        if (targetRenderer != null && phase1Material != null)
            targetRenderer.sharedMaterial = phase1Material;
    }

    public void SetPhase2()
    {
        if (targetRenderer != null && phase2Material != null)
            targetRenderer.sharedMaterial = phase2Material;
    }
}   