// FollowCamera.cs
using UnityEngine;

/// <summary>
/// Up-only follow camera (your original logic).
/// - Moves camera Y up to target+offset, never down.
/// - BottomY uses camera projection (ortho or perspective).
/// </summary>
[DisallowMultipleComponent]
public sealed class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float minY = 0f;
    [SerializeField] private float followOffsetY = 2f;

    private Camera _cam;

    public float BottomY
    {
        get
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam != null && _cam.orthographic)
                return transform.position.y - _cam.orthographicSize;

            // Perspective fallback: viewport bottom at some depth; use near-plane approximation.
            // For fall-kill checks we just need a stable "below screen" threshold.
            return transform.position.y - 10f;
        }
    }

    private void LateUpdate()
    {
        if (!target) return;

        Vector3 p = transform.position;
        float desiredY = target.position.y + followOffsetY;

        if (desiredY > p.y) p.y = desiredY;
        if (p.y < minY) p.y = minY;

        transform.position = p;
    }
}