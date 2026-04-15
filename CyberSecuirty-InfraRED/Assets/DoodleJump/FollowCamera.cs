// FollowCameraY.cs
using UnityEngine;

public class FollowCameraY : MonoBehaviour
{
    public Transform target;
    public float minY = 0f;
    public float followOffsetY = 2f;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 p = transform.position;
        float desiredY = target.position.y + followOffsetY;
        if (desiredY > p.y) p.y = desiredY;
        if (p.y < minY) p.y = minY;
        transform.position = p;
    }
}