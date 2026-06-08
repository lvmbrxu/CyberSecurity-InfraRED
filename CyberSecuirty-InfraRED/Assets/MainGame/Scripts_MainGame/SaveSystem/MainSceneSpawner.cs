using System.Collections;
using UnityEngine;

public class MainSceneSpawner : MonoBehaviour
{
    [SerializeField] private Transform player; 

    private IEnumerator Start()
    {
        SaveManager.EnsureLoaded();

        yield return null;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player == null)
        {
            Debug.LogError("[MainSceneSpawner] Player not found. Tag your player as 'Player' or assign it.");
            yield break;
        }

        string targetId = SaveManager.GetLastMainSpawn(); // now never empty
        var points = FindObjectsOfType<SpawnPoint>(true);

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
            Debug.LogWarning($"[MainSceneSpawner] SpawnPoint id '{targetId}' not found. Available spawnIds:");
            foreach (var sp in points)
                if (sp != null) Debug.Log($"  - '{sp.spawnId}'", sp);

            yield break;
        }

        player.position = found.transform.position;
        player.rotation = found.transform.rotation;

        Debug.Log($"[MainSceneSpawner] Spawned at '{found.spawnId}'", found);
    }
}