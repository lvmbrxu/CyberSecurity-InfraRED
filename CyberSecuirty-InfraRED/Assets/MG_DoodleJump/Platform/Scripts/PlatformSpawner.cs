// PlatformSpawner.cs
// - Spawns platforms naturally throughout the whole run.
// - At Phase2: swaps ALL platforms (existing + future) to Phase2Special visuals.
// - Spawns ID pickups on main platforms randomly until required count is spawned.
// - IDs are placed above platform collider top (safe offset), with min vertical separation.
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlatformSpawner : MonoBehaviour
{
    public static PlatformSpawner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DoodleJumpPlayer3D_CC player;
    [SerializeField] private CharacterController playerController;

    [Header("Platform Prefab (single)")]
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

    [Header("Phase 2 IDs")]
    [SerializeField] private GameObject idPrefab;
    [SerializeField, Range(0f, 1f)] private float idChancePerMainPlatform = 0.10f;
    [SerializeField, Min(0f)] private float minIdSeparationY = 16f;
    [SerializeField, Min(0f)] private float idWorldYOffset = 1.0f;

    [Header("Orientation")]
    [SerializeField] private Vector3 platformEuler = new(0f, 90f, 0f);

    [Header("Start Pad")]
    [SerializeField] private bool spawnStartPlatform = true;
    [SerializeField, Min(0f)] private float startYOffset = 1.2f;
    [SerializeField, Min(0f)] private float startXJitter = 0.8f;

    private bool _stopped;

    private float _maxY;
    private float _nextY;
    private float _lastX;
    private float _noiseSeed;

    private float _xLimit;
    private float MinX => -_xLimit;
    private float MaxX => _xLimit;

    private readonly Queue<Transform> _alive = new();
    private readonly List<Vector2> _recent = new(96);
    private int _recentMax = 72;

    private Quaternion _rot;

    // Phase2
    private bool _phase2;
    private int _idsToSpawn;
    private int _idsSpawned;
    private float _lastIdY = float.NegativeInfinity;

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

        _xLimit = (overrideXLimit > 0f) ? overrideXLimit : player.XLimit;

        if (laneMinXSpacing < minSpacingX) laneMinXSpacing = minSpacingX;

        if (playerController != null)
        {
            float capsuleMin = (playerController.radius * 2f) + capsulePadding;
            if (minSpacingX < capsuleMin) minSpacingX = capsuleMin;
            if (laneMinXSpacing < minSpacingX) laneMinXSpacing = minSpacingX;
        }

        if (avgGapY < minSpacingY) avgGapY = minSpacingY;
        gapJitterY = Mathf.Clamp(gapJitterY, 0f, avgGapY * 0.5f);

        _rot = Quaternion.Euler(platformEuler);
        _noiseSeed = Random.value * 1000f;

        _maxY = player.transform.position.y;
        _lastX = Mathf.Clamp(player.transform.position.x, MinX, MaxX);

        _phase2 = false;
        _idsToSpawn = 0;
        _idsSpawned = 0;
        _lastIdY = float.NegativeInfinity;

        if (spawnStartPlatform)
        {
            float sx = Mathf.Clamp(_lastX + Random.Range(-startXJitter, startXJitter), MinX, MaxX);
            float sy = _maxY + startYOffset;
            SpawnPlatform(sx, sy, force: true, isMain: true);
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

    public void EnterPhase2(int idsRequired)
    {
        _phase2 = true;
        _idsToSpawn = Mathf.Max(0, idsRequired);
        _idsSpawned = 0;
        _lastIdY = float.NegativeInfinity;

        // Swap existing platforms to phase2 visuals.
        foreach (var t in _alive)
        {
            if (t == null) continue;
            var p = t.GetComponent<Platform3D>();
            if (p != null) p.SetVariant(Platform3D.VisualVariant.Phase2Special);
        }
    }

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
        if (!TryFindValidX(y, out float mainX))
            return;

        SpawnPlatform(mainX, y, force: false, isMain: true);
        _lastX = mainX;

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

        Transform platform = Instantiate(platformPrefab, new Vector3(x, y, fixedZ), _rot).transform;

        if (_phase2)
        {
            var plat = platform.GetComponent<Platform3D>();
            if (plat != null) plat.SetVariant(Platform3D.VisualVariant.Phase2Special);

            TrySpawnIdOnPlatform(platform, y, isMain);
        }

        _alive.Enqueue(platform);
        Remember(x, y);

        while (_alive.Count > maxAlive)
        {
            Transform old = _alive.Dequeue();
            if (old != null) Destroy(old.gameObject);
        }
    }

    private void TrySpawnIdOnPlatform(Transform platformRoot, float platformY, bool isMain)
    {
        if (!isMain) return;
        if (idPrefab == null) return;
        if (_idsSpawned >= _idsToSpawn) return;

        if (platformY < _lastIdY + minIdSeparationY) return;

        if (Random.value > idChancePerMainPlatform) return;

        Vector3 worldPos;
        var col = platformRoot.GetComponentInChildren<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            worldPos = new Vector3(b.center.x, b.max.y + idWorldYOffset, b.center.z);
        }
        else
        {
            worldPos = platformRoot.position + Vector3.up * idWorldYOffset;
        }

        Instantiate(idPrefab, worldPos, Quaternion.identity, platformRoot);

        _idsSpawned++;
        _lastIdY = platformY;
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