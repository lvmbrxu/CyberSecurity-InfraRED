// CameraBackgroundSwap.cs (material swap version)
// Attach to the Quad under Main Camera.
// Assign phase1Material + phase2Material in Inspector.
// Call SetPhase2() during the transition swap.
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraBackgroundSwap : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Materials")]
    [SerializeField] private Material phase1Material;
    [SerializeField] private Material phase2Material;

    [Header("Debug")]
    [SerializeField] private bool logSwaps = false;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        SetPhase1();
    }

    public void SetPhase1() => Apply(phase1Material);
    public void SetPhase2() => Apply(phase2Material);

    private void Apply(Material mat)
    {
        if (targetRenderer == null || mat == null) return;

        // sharedMaterial = no runtime instancing/allocations.
        targetRenderer.sharedMaterial = mat;

        if (logSwaps)
            Debug.Log($"[BG] Swapped background material -> {mat.name}", this);
    }
}