// Platform3D.cs
// Platform behavior + visual swap for phase 2.
// - Calls OnPlayerBounced() from PlayerController (breakables).
// - Swaps to Phase2 material when GameManager is in Phase2.
// - Uses sharedMaterial (no instancing).
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

    [Header("Debug")]
    [SerializeField] private bool logMissingMaterials = false;

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

        if (logMissingMaterials)
        {
            if (normalMat == null) Debug.LogWarning($"[Platform3D] normalMat not assigned on {name}", this);
            if (phase2Mat == null) Debug.LogWarning($"[Platform3D] phase2Mat not assigned on {name}", this);
            if (renderers == null || renderers.Length == 0) Debug.LogWarning($"[Platform3D] No renderers found on {name}", this);
        }
    }

    private void Start()
    {
        // Auto-sync visuals to current game phase at spawn time.
        if (GameManager.Instance != null && GameManager.Instance.Phase == GameManager.GamePhase.Phase2_IdHunt)
            SetVariant(VisualVariant.Phase2Special);
        else
            SetVariant(VisualVariant.Normal);
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