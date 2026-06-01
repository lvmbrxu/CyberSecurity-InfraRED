// FollowCameraY.cs
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FollowCameraY : MonoBehaviour
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