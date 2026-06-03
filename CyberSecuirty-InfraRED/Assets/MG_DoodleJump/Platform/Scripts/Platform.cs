// Platform3D.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class Platform3D : MonoBehaviour
{
    public enum VisualVariant { Normal, Phase2Special }

    [Header("Breakable")]
    [SerializeField] private bool breaks = false;
    [SerializeField, Min(0f)] private float breakDelay = 0.05f;

    [Header("Visuals")]
    [Tooltip("If empty, auto-fills from children.")]
    [SerializeField] private Renderer[] renderers;

    [SerializeField] private Material normalMat;
    [SerializeField] private Material phase2Mat;

    private bool _used;
    private VisualVariant _current = (VisualVariant)(-1);

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        GetComponent<Collider>().isTrigger = false;
    }

    public void OnPlayerBounced()
    {
        if (!breaks || _used) return;
        _used = true;
        Invoke(nameof(BreakNow), breakDelay);
    }

    public void SetVariant(VisualVariant variant)
    {
        if (_current == variant) return;
        _current = variant;

        Material m = (variant == VisualVariant.Phase2Special) ? phase2Mat : normalMat;
        if (m == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].sharedMaterial = m;
        }
    }

    private void BreakNow() => Destroy(gameObject);
}