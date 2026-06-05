using UnityEngine;
using Unity.Cinemachine;

public class CameraPrioritySwap_CM3 : MonoBehaviour
{
    [Header("Cinemachine Cameras (CM3)")]
    public CinemachineCamera targetCamera;      // camera to switch TO (cutscene/dolly cam)
    public CinemachineCamera[] otherCameras;    // gameplay cams to push down (optional)

    [Header("Priorities")]
    public int targetPriority = 20;
    public int othersPriority = 0;

    private bool cached;
    private int targetPrev;
    private int[] othersPrev;

    public void ActivateTargetCamera()
    {
        if (targetCamera == null) return;

        CacheOnce();

        targetCamera.Priority = targetPriority;

        if (otherCameras != null)
        {
            for (int i = 0; i < otherCameras.Length; i++)
            {
                var cam = otherCameras[i];
                if (cam == null || cam == targetCamera) continue;
                cam.Priority = othersPriority;
            }
        }
    }

    public void Restore()
    {
        if (!cached) return;

        if (targetCamera != null)
            targetCamera.Priority = targetPrev;

        if (otherCameras != null && othersPrev != null)
        {
            for (int i = 0; i < otherCameras.Length && i < othersPrev.Length; i++)
            {
                if (otherCameras[i] == null) continue;
                otherCameras[i].Priority = othersPrev[i];
            }
        }

        cached = false; // allow re-cache next time
    }

    private void CacheOnce()
    {
        if (cached) return;

        targetPrev = targetCamera.Priority;

        if (otherCameras != null)
        {
            othersPrev = new int[otherCameras.Length];
            for (int i = 0; i < otherCameras.Length; i++)
                othersPrev[i] = otherCameras[i] != null ? otherCameras[i].Priority : 0;
        }

        cached = true;
    }
}