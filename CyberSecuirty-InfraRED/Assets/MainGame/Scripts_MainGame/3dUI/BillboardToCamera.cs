using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool lockY = true; // keeps it upright

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null) return;

        Vector3 dir = transform.position - targetCamera.transform.position;

        if (lockY) dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}