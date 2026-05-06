// PlatformSpawner.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PLATFORM FIELD GENERATOR (Vertical Runner)
/// 
/// Design goals:
/// - Infinite generation while player climbs (spawn driven by maxY reached).
/// - "Natural field" distribution: platforms feel scattered, not a tight chain.
/// - Density controlled in world-units (platforms per 10 meters), not "chance per spawn".
/// - Anti-clustering: enforce minimum separation in X/Y to avoid clumps.
/// - Performance: bounded instantiation per frame, bounded alive count, O(1) cleanup.
///
/// Authoring notes (technical game design):
/// - Tune density first (platformsPer10m), then gap bounds, then separation.
/// - If jumps feel impossible, increase bounceVelocity OR reduce maxGapY.
/// - If it feels too busy, reduce platformsPer10m or increase minSeparation.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlatformSpawner : MonoBehaviour
{
    public static PlatformSpawner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject platformPrefab;

    [Header("Spawn Horizon")]
    [Tooltip("Always keep content generated this far above the highest player Y reached.")]
    [SerializeField, Min(10f)] private float spawnAhead = 60f;

    [Header("Field Density (Designer)")]
    [Tooltip("Average number of platforms spawned per 10m of vertical progress.\n"
           + "Recommended: 6..10 for normal, 4..6 for sparse, 10..14 for hectic.")]
    [SerializeField, Range(2f, 20f)] private float platformsPer10m = 7.5f;

    [Header("Vertical Spacing (Hard Bounds)")]
    [Tooltip("Minimum vertical distance between any two spawned platforms (meters).")]
    [SerializeField, Min(0.5f)] private float minGapY = 1.8f;
    [Tooltip("Maximum vertical distance between consecutive candidate steps (meters).")]
    [SerializeField, Min(0.6f)] private float maxGapY = 3.2f;

    [Header("Horizontal Range")]
    [SerializeField] private float minX = -6f;
    [SerializeField] private float maxX = 6f;
    [SerializeField] private float fixedZ = 0f;

    [Header("Natural Distribution (Shape)")]
    [Tooltip("Low frequency drift to prevent 'random TV static'.\n"
           + "0.03..0.08 recommended. Higher = more waviness.")]
    [SerializeField, Min(0.001f)] private float driftScale = 0.05f;

    [Tooltip("How much the Perlin drift biases X vs pure randomness.\n"
           + "0 = pure random field. 0.3..0.6 = subtle guidance.")]
    [SerializeField, Range(0f, 1f)] private float driftBias = 0.45f;

    [Header("Anti-Clustering (Separation)")]
    [Tooltip("Minimum horizontal separation when platforms are close in Y.")]
    [SerializeField, Min(0f)] private float minSeparationX = 1.2f;

    [Tooltip("If two platforms are within this Y window, enforce minSeparationX.")]
    [SerializeField, Min(0.1f)] private float separationYWindow = 1.4f;

    [Header("Performance")]
    [Tooltip("Hard cap on alive platforms in scene (oldest are destroyed).")]
    [SerializeField, Min(30)] private int maxAlive = 140;

    [Tooltip("Platforms below (maxY - this) are eligible for cleanup.")]
    [SerializeField, Min(10f)] private float despawnBelowMaxY = 70f;

    [Tooltip("Worst-case instantiations per frame (prevents spikes).")]
    [SerializeField, Min(1)] private int maxSpawnsPerFrame = 18;

    [Tooltip("Max attempts to find a valid (non-clustered) position per spawn.")]
    [SerializeField, Range(1, 16)] private int maxPlacementAttempts = 8;

    // --- Runtime state ---
    private float _maxY;               // highest Y reached (monotonic)
    private float _cursorY;            // generation cursor
    private float _nextDensityY;       // next Y at which we should emit a platform (density scheduler)
    private float _densityStep;        // average meters per platform (derived)
    private float _noiseSeed;
    private bool _stopped;

    // Alive platform queue (oldest first). O(1) cleanup + cap.
    private readonly Queue<Transform> _alive = new();

    // Recent placements for separation checks (small sliding window).
    private readonly List<Vector2> _recent = new(64); // (x,y)
    private int _recentMax = 48;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>()?.transform;
        if (player == null || platformPrefab == null) return;

        Sanitize();

        _noiseSeed = Random.value * 1000f;

        _maxY = player.position.y;
        _cursorY = _maxY;
        _densityStep = 10f / Mathf.Max(0.1f, platformsPer10m); // meters per platform
        _nextDensityY = _cursorY + minGapY;                    // first spawn slightly above start

        // Prewarm to horizon so the first seconds are stable.
        FillTo(_maxY + spawnAhead);
    }

    private void Update()
    {
        if (_stopped) return;
        if (GameManager.Instance != null && GameManager.Instance.HasEnded) return;
        if (player == null || platformPrefab == null) return;

        // Drive generation by max progress (never decreases when player falls).
        float py = player.position.y;
        if (py > _maxY) _maxY = py;

        float targetTop = _maxY + spawnAhead;

        int spawned = 0;
        while (_cursorY < targetTop && spawned < maxSpawnsPerFrame)
        {
            StepCursor();
            spawned++;
        }

        Cleanup();
    }

    public void StopSpawning() => _stopped = true;

    // ---------------------------- Generation ----------------------------

    private void FillTo(float topY)
    {
        int guard = 5000;
        while (_cursorY < topY && guard-- > 0)
            StepCursor();
    }

    /// <summary>
    /// Advances the generation cursor and emits platforms according to density schedule.
    /// This gives a "field" instead of "one every step".
    /// </summary>
    private void StepCursor()
    {
        // Move forward with bounded step.
        _cursorY += Random.Range(minGapY, maxGapY);

        // Emit platforms when passing the density mark.
        // Multiple emits can happen if gaps are large.
        while (_cursorY >= _nextDensityY)
        {
            TrySpawnAtY(_nextDensityY);
            _nextDensityY += _densityStep;
        }
    }

    private void TrySpawnAtY(float y)
    {
        // We attempt multiple candidate X values to satisfy separation constraints.
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            float x = SampleNaturalX(y);

            if (IsValidPlacement(x, y))
            {
                PlacePlatform(x, y);
                RememberPlacement(x, y);
                return;
            }
        }

        // Fallback: place anyway with relaxed rules (prevents starvation).
        float fx = SampleNaturalX(y);
        PlacePlatform(fx, y);
        RememberPlacement(fx, y);
    }

    /// <summary>
    /// Blends uniform random with low-frequency Perlin drift for "natural" spread.
    /// driftBias controls how guided vs noisy the distribution is.
    /// </summary>
    private float SampleNaturalX(float y)
    {
        float uniform = Random.Range(minX, maxX);

        float n = Mathf.PerlinNoise(_noiseSeed, y * driftScale); // 0..1
        float drift = Mathf.Lerp(minX, maxX, n);

        float x = Mathf.Lerp(uniform, drift, driftBias);
        return Mathf.Clamp(x, minX, maxX);
    }

    private bool IsValidPlacement(float x, float y)
    {
        // Reject placements that would create local clumps.
        // Only checks a small recent window (fast).
        for (int i = 0; i < _recent.Count; i++)
        {
            Vector2 p = _recent[i];
            float dy = Mathf.Abs(y - p.y);

            if (dy <= separationYWindow)
            {
                float dx = Mathf.Abs(x - p.x);
                if (dx < minSeparationX)
                    return false;
            }
        }
        return true;
    }

    private void PlacePlatform(float x, float y)
    {
        Transform t = Instantiate(platformPrefab, new Vector3(x, y, fixedZ), Quaternion.identity).transform;
        _alive.Enqueue(t);

        // Hard cap: destroy oldest first.
        while (_alive.Count > maxAlive)
        {
            Transform old = _alive.Dequeue();
            if (old != null) Destroy(old.gameObject);
        }
    }

    private void RememberPlacement(float x, float y)
    {
        _recent.Add(new Vector2(x, y));

        // Trim old samples so checks stay O(1) small.
        if (_recent.Count > _recentMax)
            _recent.RemoveRange(0, _recent.Count - _recentMax);
    }

    // ---------------------------- Cleanup ----------------------------

    private void Cleanup()
    {
        float killY = _maxY - despawnBelowMaxY;

        // Oldest-first: pop while below threshold.
        while (_alive.Count > 0)
        {
            Transform t = _alive.Peek();
            if (t == null) { _alive.Dequeue(); continue; }
            if (t.position.y >= killY) break;

            _alive.Dequeue();
            Destroy(t.gameObject);
        }
    }

    // ---------------------------- Validation ----------------------------

    private void Sanitize()
    {
        if (maxX < minX) (minX, maxX) = (maxX, minX);
        if (maxGapY < minGapY) maxGapY = minGapY + 0.5f;

        // Keep designer values within sane runtime constraints.
        platformsPer10m = Mathf.Clamp(platformsPer10m, 2f, 20f);
        maxPlacementAttempts = Mathf.Clamp(maxPlacementAttempts, 1, 16);
        maxSpawnsPerFrame = Mathf.Clamp(maxSpawnsPerFrame, 1, 60);

        // Recent sample window sized for separation checks.
        _recentMax = Mathf.Clamp(maxAlive / 3, 32, 96);
    }
}