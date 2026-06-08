using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class Platform3D : MonoBehaviour
{
    public enum VisualVariant { Normal, Phase2Special }

    [Header("Visuals")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Material normalMat;
    [SerializeField] private Material phase2Mat;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        GetComponent<Collider>().isTrigger = false;
    }

    public void OnPlayerBounced() { }

    public void SetVariant(VisualVariant variant)
    {
        Material m = (variant == VisualVariant.Phase2Special) ? phase2Mat : normalMat;
        if (m == null) return;

        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null)
                renderers[i].sharedMaterial = m;
    }
}