// PlatformSpawner.cs
// - Natural platform field spawns for the entire run (0% -> 100%).
// - Special platforms unlock at Security >= 75%, are rare, max 3.
// - Clues spawn ONLY on special platforms (always attaches to special).
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlatformSpawner : MonoBehaviour
{
    public static PlatformSpawner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DoodleJumpPlayer3D_CC player;
    [SerializeField] private CharacterController playerController;

    [Header("Platform Prefabs")]
    [SerializeField] private GameObject normalPlatformPrefab;
    [SerializeField] private GameObject specialPlatformPrefab;

    [Header("Clue Prefab (SPECIAL ONLY)")]
    [SerializeField] private GameObject cluePrefab;
    [Tooltip("Extra height above platform collider top (world units).")]
    [SerializeField, Min(0f)] private float clueWorldYOffset = 1.0f;

    [Header("Width (defaults to player XLimit)")]
    [Tooltip("If 0, uses player.XLimit.")]
    [SerializeField, Min(0f)] private float overrideXLimit = 0f;
    [SerializeField] private float fixedZ = 0f;

    [Header("Streaming")]
    [SerializeField, Min(20f)] private float spawnAhead = 90f;
    [SerializeField, Min(20f)] private float despawnBelowMaxY = 100f;
    [SerializeField, Min(50)] private int maxAlive = 240;
    [SerializeField, Min(1)] private int maxStepsPerFrame = 24;

    [Header("Cadence (Game Feel)")]
    [SerializeField, Min(0.5f)] private float avgGapY = 3.2f;
    [SerializeField, Min(0f)] private float gapJitterY = 0.55f;

    [Header("Separation (Anti-Clump / Anti-Wedge)")]
    [SerializeField, Min(0.25f)] private float minSpacingX = 2.4f;
    [SerializeField, Min(0.5f)] private float minSpacingY = 2.8f;
    [SerializeField, Min(0f)] private float capsulePadding = 0.25f;

    [Header("Placement Search (Throughput)")]
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

    [Header("Special Platforms (Unlock Late, Rare)")]
    [SerializeField, Range(0f, 1f)] private float specialGateSecurity01 = 0.75f;
    [SerializeField, Range(0f, 1f)] private float specialChancePerBeat = 0.015f;
    [SerializeField, Min(0)] private int maxSpecialSpawnCount = 3;

    [Header("Orientation")]
    [SerializeField] private Vector3 platformEuler = new(0f, 90f, 0f);

    [Header("Start Pad")]
    [SerializeField] private bool spawnStartPlatform = true;
    [SerializeField, Min(0f)] private float startYOffset = 1.2f;
    [SerializeField, Min(0f)] private float startXJitter = 0.8f;

    [Header("Debug")]
    [SerializeField] private bool debugLogSpecialSpawns = false;

    // ---- runtime ----
    private bool _stopped;
    private float _maxY;
    private float _nextY;
    private float _lastX;
    private float _noiseSeed;

    private int _specialSpawned;

    private float _xLimit;
    private float MinX => -_xLimit;
    private float MaxX => _xLimit;

    private readonly Queue<Transform> _alive = new();
    private readonly List<Vector2> _recent = new(96);
    private int _recentMax = 72;

    private Quaternion _rot;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<DoodleJumpPlayer3D_CC>();
        if (player != null && playerController == null) playerController = player.GetComponent<CharacterController>();
        if (player == null || normalPlatformPrefab == null) return;

        _xLimit = (overrideXLimit > 0f) ? overrideXLimit : player.XLimit;

        if (laneMinXSpacing < minSpacingX) laneMinXSpacing = minSpacingX;

        // Capsule-aware spacing to prevent wedging/push-off between cubes.
        if (playerController != null)
        {
            float capsuleMin = (playerController.radius * 2f) + capsulePadding;
            if (minSpacingX < capsuleMin) minSpacingX = capsuleMin;
            if (laneMinXSpacing < minSpacingX) laneMinXSpacing = minSpacingX;
        }

        // Cadence cannot be tighter than the clump window.
        if (avgGapY < minSpacingY) avgGapY = minSpacingY;
        gapJitterY = Mathf.Clamp(gapJitterY, 0f, avgGapY * 0.5f);

        _rot = Quaternion.Euler(platformEuler);
        _noiseSeed = Random.value * 1000f;

        _maxY = player.transform.position.y;
        _lastX = Mathf.Clamp(player.transform.position.x, MinX, MaxX);

        if (spawnStartPlatform)
        {
            float sx = Mathf.Clamp(_lastX + Random.Range(-startXJitter, startXJitter), MinX, MaxX);
            float sy = _maxY + startYOffset;
            PlaceAndRemember(sx, sy, force: true, useSpecial: false);
        }

        _nextY = _maxY + Mathf.Max(minSpacingY, startYOffset + 1.2f);
        FillTo(_maxY + spawnAhead);
    }

    private void Update()
    {
        if (_stopped) return;
        if (GameManager.Instance != null && GameManager.Instance.HasEnded) return;

        float py = player.transform.position.y;
        if (py > _maxY) _maxY = py;

        float targetTop = _maxY + spawnAhead;

        int steps = 0;
        while (_nextY < targetTop && steps < maxStepsPerFrame)
        {
            SpawnBeat(_nextY);
            _nextY += NextGapY();
            steps++;
        }

        Cleanup();
    }

    public void StopSpawning() => _stopped = true;

    private void FillTo(float topY)
    {
        int guard = 8000;
        while (_nextY < topY && guard-- > 0)
        {
            SpawnBeat(_nextY);
            _nextY += NextGapY();
        }
    }

    private float NextGapY()
    {
        float g = avgGapY + Random.Range(-gapJitterY, gapJitterY);
        return Mathf.Max(minSpacingY, g);
    }

    private void SpawnBeat(float y)
    {
        // Main platform (required).
        if (!TryFindValidX(y, out float mainX))
            return;

        // SPECIAL is just a skin/variant of a platform.
        // Normal platforms continue throughout the entire run.
        bool isSpecial = ShouldSpawnSpecialThisBeat();
        PlaceAndRemember(mainX, y, force: false, useSpecial: isSpecial);
        _lastX = mainX;

        // Lane platforms (always normal).
        float lastPlacedX = mainX;
        for (int i = 0; i < extraPlatformsPerBeat; i++)
        {
            if (!TryFindValidLaneX(y, mainX, lastPlacedX, out float laneX))
                break;

            float laneY = y + Random.Range(-laneOffsetY, laneOffsetY);
            PlaceAndRemember(laneX, laneY, force: false, useSpecial: false);
            lastPlacedX = laneX;
        }
    }

    private bool ShouldSpawnSpecialThisBeat()
    {
        if (specialPlatformPrefab == null) return false;
        if (_specialSpawned >= maxSpecialSpawnCount) return false;

        var gm = GameManager.Instance;
        if (gm == null) return false;

        // Gate.
        if (gm.Security01 < specialGateSecurity01) return false;

        // Rare chance.
        if (Random.value < specialChancePerBeat)
        {
            _specialSpawned++;
            return true;
        }

        return false;
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
        float n = Mathf.PerlinNoise(_noiseSeed, y * driftScale);
        float drift = Mathf.Lerp(MinX, MaxX, n);
        float uniform = Random.Range(MinX, MaxX);

        float desired = Mathf.Lerp(uniform, drift, driftBias);

        float delta = Mathf.Clamp(desired - _lastX, -maxStepX, maxStepX);
        return Mathf.Clamp(_lastX + delta, MinX, MaxX);
    }

    private bool IsValid(float x, float y)
    {
        for (int i = 0; i < _recent.Count; i++)
        {
            Vector2 p = _recent[i];

            float dy = Mathf.Abs(y - p.y);
            if (dy > minSpacingY) continue;

            float dx = Mathf.Abs(x - p.x);
            if (dx < minSpacingX) return false;
        }
        return true;
    }

    private void PlaceAndRemember(float x, float y, bool force, bool useSpecial)
    {
        if (!force && !IsValid(x, y)) return;

        GameObject prefab = useSpecial ? specialPlatformPrefab : normalPlatformPrefab;
        if (prefab == null) prefab = normalPlatformPrefab;

        Transform platform = Instantiate(prefab, new Vector3(x, y, fixedZ), _rot).transform;

        // CLUE: ONLY on special platforms.
        if (useSpecial && cluePrefab != null)
        {
            PlaceClueOnTop(platform);

            if (debugLogSpecialSpawns)
                Debug.Log($"[PlatformSpawner] Special+Clue @ y={y:F2} (specialCount={_specialSpawned})", this);
        }

        _alive.Enqueue(platform);
        Remember(x, y);

        while (_alive.Count > maxAlive)
        {
            Transform old = _alive.Dequeue();
            if (old != null) Destroy(old.gameObject);
        }
    }

    private void PlaceClueOnTop(Transform platformRoot)
    {
        // Spawn clue as child first so renderer bounds exist.
        Transform clue = Instantiate(cluePrefab, platformRoot).transform;

        // Find platform top via collider bounds.
        var col = platformRoot.GetComponentInChildren<Collider>();
        if (col == null)
        {
            clue.localPosition = new Vector3(0f, 1.5f, 0f);
            clue.localRotation = Quaternion.identity;
            return;
        }

        Bounds pb = col.bounds;

        // Raise by clue half-height + designer offset so it doesn't clip.
        float clueHalfHeight = 0f;
        var r = clue.GetComponentInChildren<Renderer>();
        if (r != null) clueHalfHeight = r.bounds.extents.y;

        Vector3 worldPos = new Vector3(pb.center.x, pb.max.y + clueWorldYOffset + clueHalfHeight, pb.center.z);
        clue.position = worldPos;
        clue.localRotation = Quaternion.identity;
    }

    private void Remember(float x, float y)
    {
        _recent.Add(new Vector2(x, y));
        if (_recent.Count > _recentMax)
            _recent.RemoveRange(0, _recent.Count - _recentMax);
    }

    private void Cleanup()
    {
        float killY = _maxY - despawnBelowMaxY;

        while (_alive.Count > 0)
        {
            Transform t = _alive.Peek();
            if (t == null) { _alive.Dequeue(); continue; }
            if (t.position.y >= killY) break;

            _alive.Dequeue();
            Destroy(t.gameObject);
        }
    }
}