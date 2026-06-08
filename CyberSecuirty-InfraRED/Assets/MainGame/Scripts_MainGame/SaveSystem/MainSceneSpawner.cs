// MainSceneSpawner.cs
using UnityEngine;

public class MainSceneSpawner : MonoBehaviour
{
    public static string NextSpawnId;

    [SerializeField] private Transform defaultSpawn;
    [SerializeField] private SpawnPoint[] spawnPoints;

    void Start()
    {
        Transform spawn = defaultSpawn;

        if (!string.IsNullOrEmpty(NextSpawnId))
        {
            foreach (var sp in spawnPoints)
            {
                if (sp != null && sp.spawnId == NextSpawnId)
                {
                    spawn = sp.transform;
                    break;
                }
            }
            NextSpawnId = null;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && spawn != null)
            player.transform.position = spawn.position;
    }
}