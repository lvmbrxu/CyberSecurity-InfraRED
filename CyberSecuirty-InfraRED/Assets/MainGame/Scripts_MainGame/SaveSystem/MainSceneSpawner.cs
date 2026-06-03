using UnityEngine;

public class MainSceneSpawner : MonoBehaviour
{
    [SerializeField] private Transform player; // drag your player here

    private void Start()
    {
        SaveManager.EnsureLoaded();

        string targetId = SaveManager.Data.lastMainSpawnId;
        SpawnPoint[] points = FindObjectsOfType<SpawnPoint>();

        foreach (var p in points)
        {
            if (p.spawnId == targetId)
            {
                player.position = p.transform.position;
                player.rotation = p.transform.rotation;
                return;
            }
        }

        // Fallback: if ID not found, do nothing or use Default
    }
}