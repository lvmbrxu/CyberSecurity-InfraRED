// InfoSpawner.cs
using UnityEngine;

/// <summary>
/// Spawns Security +/- collectibles along the climb.
/// - Spawns ahead of player based on Y.
/// - Caps active count.
/// - Stops when GameManager ends.
/// </summary>
[DisallowMultipleComponent]
public sealed class InfoSpawner : MonoBehaviour
{
    public static InfoSpawner Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private Transform player;

    [Header("Prefabs")]
    [SerializeField] private InfoCollectible securityPlusPrefab;
    [SerializeField] private InfoCollectible securityMinusPrefab;

    [Header("Spawn Rules")]
    [SerializeField, Min(1)] private int maxActive = 20;
    [SerializeField, Min(0.5f)] private float spawnStepY = 8f;
    [SerializeField, Range(0f, 1f)] private float minusChance = 0.25f;

    [Header("Horizontal Range")]
    [SerializeField] private float xRange = 3f;

    private float _nextSpawnY;
    private int _activeCount;
    private bool _stopped;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (player == null) player = Object.FindFirstObjectByType<DoodleJumpPlayer3D_CC>()?.transform;

        _nextSpawnY = player != null ? player.position.y + spawnStepY : spawnStepY;
        _activeCount = 0;
        _stopped = false;
    }

    private void Update()
    {
        if (_stopped) return;
        if (GameManager.Instance != null && GameManager.Instance.HasEnded) return;
        if (player == null) return;

        float py = player.position.y;

        while (_activeCount < maxActive && py + (spawnStepY * 3f) >= _nextSpawnY)
        {
            SpawnAt(_nextSpawnY);
            _nextSpawnY += spawnStepY;
        }
    }

    public void StopSpawning() => _stopped = true;

    private void SpawnAt(float y)
    {
        var prefab = (Random.value < minusChance) ? securityMinusPrefab : securityPlusPrefab;
        if (prefab == null) return;

        float x = Random.Range(-xRange, xRange);
        Vector3 pos = new(x, y, 0f);

        var inst = Instantiate(prefab, pos, Quaternion.identity);
        _activeCount++;

        var hook = inst.gameObject.AddComponent<DestroyHook>();
        hook.Init(this);
    }

    private sealed class DestroyHook : MonoBehaviour
    {
        private InfoSpawner _owner;
        public void Init(InfoSpawner owner) => _owner = owner;

        private void OnDestroy()
        {
            if (_owner != null) _owner._activeCount = Mathf.Max(0, _owner._activeCount - 1);
        }
    }
}