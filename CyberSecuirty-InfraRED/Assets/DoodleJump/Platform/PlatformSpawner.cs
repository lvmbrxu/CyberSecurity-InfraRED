using System.Collections.Generic;
using UnityEngine;

public class PlatformSpawner : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public GameObject platformPrefab;     // normal platform prefab 
    public GameObject finishPlatformPrefab; // final platform prefab 

    [Header("Where the finish is")]
    public float finishWorldY = 120f;     // level end height in world units
    public float finishExtraClearance = 3f; // spawn finish a bit above last normal platform

    [Header("Spawn band ABOVE player")]
    public float bandMinAbove = 7f;
    public float bandMaxAbove = 28f;

    [Header("Quality")]
    public float minGapY = 1.7f;
    public float maxGapY = 2.8f;
    public float minX = -6f;
    public float maxX = 6f;
    public float fixedZ = 0f;
    public float minDeltaXFromLast = 1.3f;

    [Header("Cleanup / Safety")]
    public float despawnBelowPlayerY = 35f;
    public int maxSpawnsPerFrame = 12;
    public int initialFillGuard = 600;
    public float minGapEpsilon = 0.1f;

    [Header("Output (read-only)")]
    public Transform finishTransform;     // assigned at runtime

    float nextSpawnY;
    float lastSpawnX;
    bool finishSpawned;

    readonly List<GameObject> spawned = new();

    void Start()
    {
        if (!player || !platformPrefab) return;

        Sanitize();

        nextSpawnY = player.position.y + bandMinAbove;
        lastSpawnX = player.position.x;
        
        float top = player.position.y + bandMaxAbove;
        int guard = 0;
        while (nextSpawnY < top && guard++ < Mathf.Max(1, initialFillGuard))
            SpawnNormal();

        // If finish is already within/under the current band, spawn it now.
        TrySpawnFinishIfReached();
    }

    void Update()
    {
        if (!player || !platformPrefab) return;

        Sanitize();

        // If finish exists, we can stop normal spawning above it.
        float hardTop = finishSpawned ? finishTransform.position.y : float.PositiveInfinity;

        float bandBottom = player.position.y + bandMinAbove;
        float bandTop = Mathf.Min(player.position.y + bandMaxAbove, hardTop);

        if (nextSpawnY < bandBottom)
            nextSpawnY = bandBottom;

        int spawnedThisFrame = 0;
        while (!finishSpawned && nextSpawnY < bandTop && spawnedThisFrame++ < maxSpawnsPerFrame)
            SpawnNormal();

        TrySpawnFinishIfReached();
        Cleanup();
    }

    void TrySpawnFinishIfReached()
    {
        if (finishSpawned) return;
        if (!finishPlatformPrefab) return;

        // When our normal spawn cursor passes finishWorldY, place finish platform once.
        if (nextSpawnY >= finishWorldY)
        {
            float x = PickGoodX(lastSpawnX);
            float y = Mathf.Max(finishWorldY, nextSpawnY + finishExtraClearance);

            var go = Instantiate(finishPlatformPrefab, new Vector3(x, y, fixedZ), Quaternion.identity, transform);
            EnsureImmovable(go);

            finishTransform = go.transform;
            finishSpawned = true;

            // Notify game manager if present
            if (GameManager.I) GameManager.I.SetFinish(finishTransform);
        }
    }

    void SpawnNormal()
    {
        float gap = Random.Range(minGapY, maxGapY);
        if (gap < minGapEpsilon) gap = minGapEpsilon;

        float y = nextSpawnY + gap;
        nextSpawnY = y;

        float x = PickGoodX(lastSpawnX);
        lastSpawnX = x;

        var go = Instantiate(platformPrefab, new Vector3(x, y, fixedZ), Quaternion.identity, transform);
        EnsureImmovable(go);
        spawned.Add(go);
    }

    void Cleanup()
    {
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

    void EnsureImmovable(GameObject go)
    {
        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    float PickGoodX(float lastX)
    {
        const int tries = 8;
        float best = Random.Range(minX, maxX);
        float bestScore = -1f;

        for (int i = 0; i < tries; i++)
        {
            float c = Random.Range(minX, maxX);

            float dx = Mathf.Abs(c - lastX);
            float edge = Mathf.Min(Mathf.Abs(c - minX), Mathf.Abs(maxX - c));
            float score = 0f;

            score += Mathf.Clamp01(dx / Mathf.Max(0.0001f, minDeltaXFromLast));
            score += Mathf.Clamp01(edge / ((maxX - minX) * 0.5f)) * 0.35f;

            if (score > bestScore) { bestScore = score; best = c; }
            if (dx >= minDeltaXFromLast) return c;
        }
        return best;
    }

    void Sanitize()
    {
        if (maxGapY < minGapY) maxGapY = minGapY;
        if (minGapY < minGapEpsilon) minGapY = minGapEpsilon;
        if (maxGapY < minGapEpsilon) maxGapY = minGapEpsilon;

        if (bandMaxAbove < bandMinAbove) bandMaxAbove = bandMinAbove + 1f;
        if (bandMinAbove < 0f) bandMinAbove = 0f;

        if (maxX < minX) { float t = minX; minX = maxX; maxX = t; }
        if (maxSpawnsPerFrame < 1) maxSpawnsPerFrame = 1;
        if (initialFillGuard < 50) initialFillGuard = 50;
    }
}