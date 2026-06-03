using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlatformSpawner : MonoBehaviour
{
    public static PlatformSpawner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DoodleJumpPlayer3D_CC player;
    [SerializeField] private CharacterController playerController;

    [Header("Platform Prefab")]
    [SerializeField] private GameObject platformPrefab;

    [Header("Width (defaults to player XLimit)")]
    [SerializeField, Min(0f)] private float overrideXLimit = 0f;
    [SerializeField] private float fixedZ = 0f;

    [Header("Streaming")]
    [SerializeField, Min(20f)] private float spawnAhead = 90f;
    [SerializeField, Min(20f)] private float despawnBelowMaxY = 100f;
    [SerializeField, Min(50)] private int maxAlive = 240;
    [SerializeField, Min(1)] private int maxStepsPerFrame = 24;

    [Header("Cadence")]
    [SerializeField, Min(0.5f)] private float avgGapY = 3.2f;
    [SerializeField, Min(0f)] private float gapJitterY = 0.55f;

    [Header("Separation")]
    [SerializeField, Min(0.25f)] private float minSpacingX = 2.4f;
    [SerializeField, Min(0.5f)] private float minSpacingY = 2.8f;
    [SerializeField, Min(0f)] private float capsulePadding = 0.25f;

    [Header("Placement Search")]
    [SerializeField, Range(1, 32)] private int attemptsPerBeat = 14;
    [SerializeField, Range(0f, 1f)] private float stableSearchBias = 0.55f;

    [Header("Natural Motion")]
    [SerializeField, Min(0.001f)] private float driftScale = 0.045f;
    [SerializeField, Range(0f, 1f)] private float driftBias = 0.45f;
    [SerializeField, Min(0f)] private float maxStepX = 2.4f;

    [Header("Width / Lanes")]
    [SerializeField, Min(0)] private int extraPlatformsPerBeat = 1;
    [SerializeField, Min(0f)] private float laneMinXSpacing = 3.4f;
    [SerializeField, Min(0f)] private float laneOffsetY = 0.8f;

    [Header("IDs (Phase 2)")]
    [SerializeField] private GameObject idPrefab;

    [Tooltip("Base chance per MAIN platform during Phase2.")]
    [SerializeField, Range(0f, 1f)] private float idChancePerMainPlatform = 0.35f;

    [Tooltip("Extra IDs spawned beyond the required amount (makes the hunt feel active).")]
    [SerializeField, Min(0)] private int extraIdsToSpawn = 4;

    [Tooltip("Force an ID after this many MAIN platforms without spawning one.")]
    [SerializeField, Min(1)] private int forceIdAfterNoSpawnBeats = 3;

    [Tooltip("Min vertical spacing between IDs.")]
    [SerializeField, Min(0f)] private float minIdSeparationY = 10f;

    [Tooltip("Height above platform top for the ID pickup.")]
    [SerializeField, Min(0f)] private float idWorldYOffset = 1.0f;

    [Header("Orientation")]
    [SerializeField] private Vector3 platformEuler = new(0f, 90f, 0f);

    [Header("Start Pad")]
    [SerializeField] private bool spawnStartPlatform = true;
    [SerializeField, Min(0f)] private float startYOffset = 1.2f;
    [SerializeField, Min(0f)] private float startXJitter = 0.8f;

    private bool stopped;

    private float maxY;
    private float nextY;
    private float lastX;
    private float noiseSeed;

    private float xLimit;
    private float MinX => -xLimit;
    private float MaxX => xLimit;

    private readonly Queue<Transform> alive = new();
    private readonly List<Vector2> recent = new(96);
    private int recentMax = 72;

    private Quaternion rot;

    // Phase flags
    private bool phase2VisualsOnly;
    private bool phase2;

    // IDs runtime
    private int idsToSpawn;
    private int idsSpawned;
    private float lastIdY = float.NegativeInfinity;
    private int beatsSinceLastId; // pity timer

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<DoodleJumpPlayer3D_CC>();
        if (player != null && playerController == null) playerController = player.GetComponent<CharacterController>();
        if (player == null || platformPrefab == null) return;

        xLimit = (overrideXLimit > 0f) ? overrideXLimit : player.XLimit;

        if (laneMinXSpacing < minSpacingX) laneMinXSpacing = minSpacingX;

        if (playerController != null)
        {
            float capsuleMin = (playerController.radius * 2f) + capsulePadding;
            if (minSpacingX < capsuleMin) minSpacingX = capsuleMin;
            if (laneMinXSpacing < minSpacingX) laneMinXSpacing = minSpacingX;
        }

        if (avgGapY < minSpacingY) avgGapY = minSpacingY;
        gapJitterY = Mathf.Clamp(gapJitterY, 0f, avgGapY * 0.5f);

        rot = Quaternion.Euler(platformEuler);
        noiseSeed = Random.value * 1000f;

        maxY = player.transform.position.y;
        lastX = Mathf.Clamp(player.transform.position.x, MinX, MaxX);

        phase2 = false;
        phase2VisualsOnly = false;

        idsToSpawn = 0;
        idsSpawned = 0;
        beatsSinceLastId = 0;

        if (spawnStartPlatform)
        {
            float sx = Mathf.Clamp(lastX + Random.Range(-startXJitter, startXJitter), MinX, MaxX);
            float sy = maxY + startYOffset;
            SpawnPlatform(sx, sy, force: true, isMain: true);
        }

        nextY = maxY + Mathf.Max(minSpacingY, startYOffset + 1.2f);
        FillTo(maxY + spawnAhead);
    }

    private void Update()
    {
        if (stopped) return;
        if (GameManager.Instance != null && GameManager.Instance.HasEnded) return;

        float py = player.transform.position.y;
        if (py > maxY) maxY = py;

        float targetTop = maxY + spawnAhead;

        int steps = 0;
        while (nextY < targetTop && steps < maxStepsPerFrame)
        {
            SpawnBeat(nextY);
            nextY += NextGapY();
            steps++;
        }

        Cleanup();
    }

    public void StopSpawning() => stopped = true;

    public void PreviewPhase2Visuals()
    {
        phase2VisualsOnly = true;
        SwapAllPlatformsToPhase2Global();
    }

    public void EnterPhase2(int idsRequired)
    {
        phase2 = true;
        phase2VisualsOnly = true;

        // Spawn MORE than required so the world feels active.
        idsToSpawn = Mathf.Max(0, idsRequired) + Mathf.Max(0, extraIdsToSpawn);
        idsSpawned = 0;
        beatsSinceLastId = 0;
        lastIdY = float.NegativeInfinity;

        SwapAllPlatformsToPhase2Global();
    }

    public void SwapAllPlatformsToPhase2Global()
    {
#if UNITY_2022_2_OR_NEWER
        var platforms = FindObjectsByType<Platform3D>(FindObjectsSortMode.None);
#else
        var platforms = FindObjectsOfType<Platform3D>();
#endif
        for (int i = 0; i < platforms.Length; i++)
            platforms[i].SetVariant(Platform3D.VisualVariant.Phase2Special);
    }

    private void FillTo(float topY)
    {
        int guard = 8000;
        while (nextY < topY && guard-- > 0)
        {
            SpawnBeat(nextY);
            nextY += NextGapY();
        }
    }

    private float NextGapY()
    {
        float g = avgGapY + Random.Range(-gapJitterY, gapJitterY);
        return Mathf.Max(minSpacingY, g);
    }

    private void SpawnBeat(float y)
    {
        if (!TryFindValidX(y, out float mainX))
            return;

        SpawnPlatform(mainX, y, force: false, isMain: true);
        lastX = mainX;

        float lastPlacedX = mainX;
        for (int i = 0; i < extraPlatformsPerBeat; i++)
        {
            if (!TryFindValidLaneX(y, mainX, lastPlacedX, out float laneX))
                break;

            float laneY = y + Random.Range(-laneOffsetY, laneOffsetY);
            SpawnPlatform(laneX, laneY, force: false, isMain: false);
            lastPlacedX = laneX;
        }
    }

    private void SpawnPlatform(float x, float y, bool force, bool isMain)
    {
        if (!force && !IsValid(x, y)) return;

        Transform platform = Instantiate(platformPrefab, new Vector3(x, y, fixedZ), rot).transform;

        if (phase2VisualsOnly || phase2)
        {
            var plat = platform.GetComponent<Platform3D>();
            if (plat != null) plat.SetVariant(Platform3D.VisualVariant.Phase2Special);
        }

        if (phase2 && isMain)
            TrySpawnIdOnPlatform(platform, y);

        alive.Enqueue(platform);
        Remember(x, y);

        while (alive.Count > maxAlive)
        {
            Transform old = alive.Dequeue();
            if (old != null) Destroy(old.gameObject);
        }
    }

    private void TrySpawnIdOnPlatform(Transform platformRoot, float platformY)
    {
        if (idPrefab == null) return;
        if (idsSpawned >= idsToSpawn) return;

        // Space them out vertically.
        if (platformY < lastIdY + minIdSeparationY)
        {
            beatsSinceLastId++;
            return;
        }

        // Pity timer: force if we went too long without one.
        bool force = beatsSinceLastId >= forceIdAfterNoSpawnBeats;
        bool roll = Random.value <= idChancePerMainPlatform;

        if (!force && !roll)
        {
            beatsSinceLastId++;
            return;
        }

        var col = platformRoot.GetComponentInChildren<Collider>();
        Vector3 worldPos = (col != null)
            ? new Vector3(col.bounds.center.x, col.bounds.max.y + idWorldYOffset, col.bounds.center.z)
            : platformRoot.position + Vector3.up * idWorldYOffset;

        Instantiate(idPrefab, worldPos, Quaternion.identity, platformRoot);

        idsSpawned++;
        lastIdY = platformY;
        beatsSinceLastId = 0;
    }

    private bool TryFindValidX(float y, out float x)
    {
        float baseX = SampleMainX(y);

        for (int i = 0; i < attemptsPerBeat; i++)
        {
            float t = (attemptsPerBeat <= 1) ? 1f : (i / (attemptsPerBeat - 1f));
            float near = Mathf.Lerp(baseX, Random.Range(MinX, MaxX), t);
            float candidate = Mathf.Lerp(Random.Range(MinX, MaxX), near, stableSearchBias);

            candidate = Mathf.Clamp(candidate, MinX, MaxX);

            if (IsValid(candidate, y))
            {
                x = candidate;
                return true;
            }
        }

        x = 0f;
        return false;
    }

    private bool TryFindValidLaneX(float y, float mainX, float lastLaneX, out float x)
    {
        const int laneAttempts = 10;

        for (int i = 0; i < laneAttempts; i++)
        {
            float candidate = Random.Range(MinX, MaxX);
            if (Mathf.Abs(candidate - mainX) < laneMinXSpacing) continue;
            if (Mathf.Abs(candidate - lastLaneX) < laneMinXSpacing) continue;

            if (IsValid(candidate, y))
            {
                x = candidate;
                return true;
            }
        }

        x = 0f;
        return false;
    }

    private float SampleMainX(float y)
    {
        float n = Mathf.PerlinNoise(noiseSeed, y * driftScale);
        float drift = Mathf.Lerp(MinX, MaxX, n);
        float uniform = Random.Range(MinX, MaxX);

        float desired = Mathf.Lerp(uniform, drift, driftBias);
        float delta = Mathf.Clamp(desired - lastX, -maxStepX, maxStepX);
        return Mathf.Clamp(lastX + delta, MinX, MaxX);
    }

    private bool IsValid(float x, float y)
    {
        for (int i = 0; i < recent.Count; i++)
        {
            Vector2 p = recent[i];
            float dy = Mathf.Abs(y - p.y);
            if (dy > minSpacingY) continue;

            float dx = Mathf.Abs(x - p.x);
            if (dx < minSpacingX) return false;
        }
        return true;
    }

    private void Remember(float x, float y)
    {
        recent.Add(new Vector2(x, y));
        if (recent.Count > recentMax)
            recent.RemoveRange(0, recent.Count - recentMax);
    }

    private void Cleanup()
    {
        float killY = maxY - despawnBelowMaxY;

        while (alive.Count > 0)
        {
            Transform t = alive.Peek();
            if (t == null) { alive.Dequeue(); continue; }
            if (t.position.y >= killY) break;

            alive.Dequeue();
            Destroy(t.gameObject);
        }
    }
}