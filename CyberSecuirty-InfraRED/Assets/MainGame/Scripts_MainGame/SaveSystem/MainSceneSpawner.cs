using UnityEngine;

public class MainSceneSpawner : MonoBehaviour
{
    [SerializeField] private Transform player; // optional (will find by tag)

    private void Start()
    {
        SaveManager.EnsureLoaded();

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player == null)
        {
            Debug.LogError("[MainSceneSpawner] Player not found. Tag your player as 'Player' or assign it.");
            return;
        }

        string targetId = SaveManager.GetLastMainSpawn();
        targetId = string.IsNullOrWhiteSpace(targetId) ? "" : targetId.Trim();

        var points = FindObjectsOfType<SpawnPoint>(true);

        // Debug: show what we're trying to use
        Debug.Log($"[MainSceneSpawner] lastMainSpawnId='{targetId}' (points={points.Length})");

        SpawnPoint found = null;
        foreach (var sp in points)
        {
            if (sp == null || string.IsNullOrWhiteSpace(sp.spawnId)) continue;

            if (string.Equals(sp.spawnId.Trim(), targetId, System.StringComparison.OrdinalIgnoreCase))
            {
                found = sp;
                break;
            }
        }

        if (found == null)
        {
            // Print all available IDs to catch typos instantly
            Debug.LogWarning("[MainSceneSpawner] SpawnPoint id not found. Available spawnIds:");
            foreach (var sp in points)
                if (sp != null) Debug.Log($"  - '{sp.spawnId}'", sp);

            return; // stays at default
        }

        // Teleport
        player.position = found.transform.position;
        player.rotation = found.transform.rotation;

        Debug.Log($"[MainSceneSpawner] Spawned at '{found.spawnId}'", found);
    }
}