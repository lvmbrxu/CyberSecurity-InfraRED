using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlatformSpawner : MonoBehaviour
{
    public static PlatformSpawner Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    [Header("Refs")]
    [SerializeField] private DoodleJumpPlayer3D_CC player;
    [SerializeField] private CharacterController playerController;

    [Header("Prefabs")]
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private GameObject idPrefab;

    [Header("Streaming")]
    [SerializeField] private float spawnAhead = 60f;
    [SerializeField] private float despawnBelowMaxY = 45f;
    [SerializeField] private int maxAlive = 250;
    [SerializeField] private int maxStepsPerFrame = 24;

    [Header("Cadence")]
    [SerializeField] private float avgGapY = 7.9f;
    [SerializeField] private float gapJitterY = 0.6f;

    [Header("Separation")]
    [SerializeField] private float minSpacingX = 16.6f;
    [SerializeField] private float minSpacingY = 3.88f;
    [SerializeField] private float capsulePadding = 0.72f;

    [Header("Search")]
    [SerializeField] private int attemptsPerBeat = 12;
    [SerializeField] private float stableSearchBias = 0.55f;

    [Header("Natural Motion")]
    [SerializeField] private float driftScale = 2f;
    [SerializeField] private float driftBias = 0.255f;
    [SerializeField] private float maxStepX = 17f;

    [Header("Lanes")]
    [SerializeField] private int extraPlatformsPerBeat = 1;
    [SerializeField] private float laneMinXSpacing = 5.09f;
    [SerializeField] private float laneOffsetY = 0.8f;

    [Header("IDs (Phase2)")]
    [SerializeField] private float idChancePerMainPlatform = 0.353f;
    [SerializeField] private int extraIdsToSpawn = 27;
    [SerializeField] private int forceIdAfterNoSpawnBeats = 3;
    [SerializeField] private float minIdSeparationY = 16f;
    [SerializeField] private float idWorldYOffset = 1f;

    [Header("Orientation")]
    [SerializeField] private Vector3 platformEuler = Vector3.zero;

    private bool stopped;
    private bool phase2;

    private float maxY;
    private float nextY;
    private float lastX;
    private float noiseSeed;

    private readonly Queue<Transform> alive = new();
    private readonly List<Vector2> recent = new(96);

    private Quaternion rot;

    private int idsToSpawn;
    private int idsSpawned;
    private int beatsSinceLastId;
    private float lastIdY = float.NegativeInfinity;

    private void Awake()
    {
        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            Destroy(gameObject);
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<DoodleJumpPlayer3D_CC>();
        if (player != null && playerController == null) playerController = player.GetComponent<CharacterController>();

        rot = Quaternion.Euler(platformEuler);
        noiseSeed = Random.value * 1000f;

        maxY = player != null ? player.transform.position.y : 0f;
        lastX = player != null ? player.transform.position.x : 0f;

        stopped = false;
        phase2 = false;

        idsToSpawn = 0;
        idsSpawned = 0;
        beatsSinceLastId = 0;
        lastIdY = float.NegativeInfinity;

        nextY = maxY + Mathf.Max(minSpacingY, 2f);
        FillTo(maxY + spawnAhead);
    }

    private void Update()
    {
        if (stopped) return;
        if (GameManager.Instance != null && GameManager.Instance.HasEnded) return;
        if (player == null) return;

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

    public void EnterPhase2(int idsRequired)
    {
        phase2 = true;

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

    private float NextGapY()
    {
        float g = avgGapY + Random.Range(-gapJitterY, gapJitterY);
        return Mathf.Max(minSpacingY, g);
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

    private void SpawnBeat(float y)
    {
        if (!TryFindValidX(y, out float mainX))
            return;

        SpawnPlatform(mainX, y, isMain: true);
        lastX = mainX;

        float lastPlacedX = mainX;
        for (int i = 0; i < extraPlatformsPerBeat; i++)
        {
            if (!TryFindValidLaneX(y, mainX, lastPlacedX, out float laneX))
                break;

            float laneY = y + Random.Range(-laneOffsetY, laneOffsetY);
            SpawnPlatform(laneX, laneY, isMain: false);
            lastPlacedX = laneX;
        }
    }

    private void SpawnPlatform(float x, float y, bool isMain)
    {
        if (platformPrefab == null) return;
        if (!IsValid(x, y)) return;

        Transform platform = Instantiate(platformPrefab, new Vector3(x, y, 0f), rot).transform;

        if (phase2)
        {
            var plat = platform.GetComponent<Platform3D>();
            if (plat != null) plat.SetVariant(Platform3D.VisualVariant.Phase2Special);

            if (isMain) TrySpawnIdOnPlatform(platform, y);
        }

        alive.Enqueue(platform);
        recent.Add(new Vector2(x, y));

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

        if (platformY < lastIdY + minIdSeparationY)
        {
            beatsSinceLastId++;
            return;
        }

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
            float near = Mathf.Lerp(baseX, Random.Range(-maxStepX, maxStepX), t);
            float candidate = Mathf.Lerp(Random.Range(-maxStepX, maxStepX), near, stableSearchBias);

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
            float candidate = Random.Range(-maxStepX, maxStepX);

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
        float drift = Mathf.Lerp(-maxStepX, maxStepX, n);
        float uniform = Random.Range(-maxStepX, maxStepX);

        float desired = Mathf.Lerp(uniform, drift, driftBias);
        float delta = Mathf.Clamp(desired - lastX, -maxStepX, maxStepX);
        return lastX + delta;
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