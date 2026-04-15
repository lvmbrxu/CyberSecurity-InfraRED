// PlatformSpawner3D_PlayerAware.cs
using System.Collections.Generic;
using UnityEngine;

public class PlatformSpawner3D_PlayerAware : MonoBehaviour
{
    public Transform player;
    public GameObject platformPrefab;

    [Header("Spawn band ABOVE player")]
    public float bandMinAbove = 6f;   // lowest platform spawn height relative to player
    public float bandMaxAbove = 18f;  // highest platform spawn height relative to player

    [Header("Density")]
    public float minGapY = 1.5f;
    public float maxGapY = 2.6f;

    [Header("Area")]
    public float minX = -6f;
    public float maxX = 6f;
    public float fixedZ = 0f;

    [Header("Cleanup")]
    public float despawnBelowPlayerY = 25f;

    [Header("Safety")]
    public int maxSpawnsPerFrame = 12;
    public float minGapEpsilon = 0.1f;

    float nextSpawnY;
    readonly List<GameObject> spawned = new();

    void Start()
    {
        if (!player || !platformPrefab) return;
        Sanitize();

        // Start a bit above the player so nothing spawns in their face at frame 0
        nextSpawnY = player.position.y + bandMinAbove;

        // Pre-fill up to bandMaxAbove
        float top = player.position.y + bandMaxAbove;
        int guard = 0;
        while (nextSpawnY < top && guard++ < 500)
            SpawnNext();
    }

    void Update()
    {
        if (!player || !platformPrefab) return;
        Sanitize();

        // Keep filled only in a band above the player.
        float bandBottom = player.position.y + bandMinAbove;
        float bandTop = player.position.y + bandMaxAbove;

        // If player jumped up past our nextSpawnY, snap it to the band bottom.
        if (nextSpawnY < bandBottom)
            nextSpawnY = bandBottom;

        int spawnedThisFrame = 0;
        while (nextSpawnY < bandTop && spawnedThisFrame < maxSpawnsPerFrame)
        {
            SpawnNext();
            spawnedThisFrame++;
        }

        // Cleanup
        float killY = player.position.y - despawnBelowPlayerY;
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            var go = spawned[i];
            if (!go) { spawned.RemoveAt(i); continue; }

            if (go.transform.position.y < killY)
            {
                Destroy(go);
                spawned.RemoveAt(i);
            }
        }
    }

    void SpawnNext()
    {
        float gap = Random.Range(minGapY, maxGapY);
        if (gap < minGapEpsilon) gap = minGapEpsilon;

        float x = Random.Range(minX, maxX);
        float y = nextSpawnY + gap;
        nextSpawnY = y;

        var go = Instantiate(platformPrefab, new Vector3(x, y, fixedZ), Quaternion.identity, transform);
        spawned.Add(go);
    }

    void Sanitize()
    {
        if (bandMaxAbove < bandMinAbove) bandMaxAbove = bandMinAbove + 1f;
        if (bandMinAbove < 0f) bandMinAbove = 0f;
        if (bandMaxAbove < 1f) bandMaxAbove = 1f;

        if (maxGapY < minGapY) maxGapY = minGapY;
        if (minGapY < minGapEpsilon) minGapY = minGapEpsilon;
        if (maxGapY < minGapEpsilon) maxGapY = minGapEpsilon;

        if (maxSpawnsPerFrame < 1) maxSpawnsPerFrame = 1;

        if (maxX < minX) { float t = minX; minX = maxX; maxX = t; }
    }
}