using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraRig : MonoBehaviour
{
    [Serializable]
    public class CameraEntry
    {
        public string cameraId;
        public CinemachineCamera camera;   // CM3 component
        public int cutscenePriority = 50;
    }

    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private int gameplayPriority = 10;
    [SerializeField] private List<CameraEntry> cameras = new();

    private Dictionary<string, CameraEntry> _map;

    private void Awake()
    {
        _map = new Dictionary<string, CameraEntry>();

        foreach (var entry in cameras)
        {
            if (!string.IsNullOrWhiteSpace(entry.cameraId) && entry.camera != null)
                _map[entry.cameraId] = entry;

            if (entry.camera != null)
                entry.camera.Priority = 0;
        }

        if (gameplayCamera != null)
            gameplayCamera.Priority = gameplayPriority;
    }

    public void SwitchTo(string cameraId)
    {
        if (string.IsNullOrWhiteSpace(cameraId) || !_map.TryGetValue(cameraId, out var entry))
        {
            ReturnToGameplay();
            return;
        }

        foreach (var kv in _map)
            kv.Value.camera.Priority = 0;

        if (gameplayCamera != null)
            gameplayCamera.Priority = gameplayPriority;

        entry.camera.Priority = entry.cutscenePriority;
    }

    public void ReturnToGameplay()
    {
        foreach (var kv in _map)
            kv.Value.camera.Priority = 0;

        if (gameplayCamera != null)
            gameplayCamera.Priority = gameplayPriority;
    }
}