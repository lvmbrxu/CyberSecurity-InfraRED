// GameManager.cs (add clue tracking + UI hooks)
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Security-driven loop.
/// - Security (0..1) increases with maxY progress + pickups.
/// - Falling applies penalty ONCE per fall, then respawns if Security > 0.
/// - Death only at 0, Win at 1.
/// </summary>

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private DoodleJumpPlayer3D_CC player;
    [SerializeField] private FollowCameraY followCam;

    [Header("UI - Security")]
    [SerializeField] private Slider securitySlider;
    [SerializeField] private TMP_Text securityPercentText;

    [Header("UI - Clues")]
    [Tooltip("Example: 'Clues: 0/3'")]
    [SerializeField] private TMP_Text clueCountText;

    [Header("Panels")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject winPanel;

    [Header("Security")]
    [SerializeField, Range(0f, 1f)] private float startSecurity01 = 0.25f;
    [SerializeField, Range(0.01f, 1f)] private float fallPenalty01 = 0.10f;
    [SerializeField, Min(1f)] private float unitsToFullSecurity = 900f;

    [Header("Fall Rules")]
    [SerializeField, Min(0f)] private float fallBelowScreen = 2.5f;
    [SerializeField, Min(0f)] private float fallUnlockMargin = 1.5f;

    [Header("Clues")]
    [Tooltip("Total clues possible in the run (matches max special platforms).")]
    [SerializeField, Min(0)] private int totalClues = 3;

    private float _security01;
    private float _runStartY;
    private float _maxY;
    private bool _ended;
    private bool _fallLock;

    private int _cluesCollected;

    public bool HasEnded => _ended;
    public float Security01 => _security01;
    public int CluesCollected => _cluesCollected;
    public int TotalClues => totalClues;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<DoodleJumpPlayer3D_CC>();
        if (followCam == null) followCam = Camera.main != null ? Camera.main.GetComponent<FollowCameraY>() : null;

        if (deathPanel) deathPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);

        _runStartY = player != null ? player.transform.position.y : 0f;
        _maxY = _runStartY;
        _fallLock = false;

        _cluesCollected = 0;
        UpdateClueUI();

        SetSecurity(startSecurity01, force: true);
    }

    private void Update()
    {
        if (_ended) return;
        if (player == null || followCam == null) return;

        float y = player.transform.position.y;

        // Distance-based security uses maxY only.
        if (y > _maxY)
        {
            _maxY = y;
            float dist01 = Mathf.Clamp01((_maxY - _runStartY) / unitsToFullSecurity);
            if (dist01 > _security01) SetSecurity(dist01);
        }

        // Debounced fall.
        float fallLine = followCam.BottomY - fallBelowScreen;

        if (_fallLock && y > (fallLine + fallUnlockMargin))
            _fallLock = false;

        if (!_fallLock && y < fallLine)
        {
            _fallLock = true;
            ApplyFallPenaltyAndRecover();
        }
    }

    // --- Security API ---
    public void AddSecurityDelta01(float delta01)
    {
        if (_ended) return;
        SetSecurity(_security01 + delta01);
    }

    private void ApplyFallPenaltyAndRecover()
    {
        if (_ended) return;

        SetSecurity(_security01 - fallPenalty01);

        if (_security01 <= 0f)
        {
            Die();
            return;
        }

        player.RecoverFromFall();
    }

    private void SetSecurity(float value01, bool force = false)
    {
        float v = Mathf.Clamp01(value01);
        if (!force && Mathf.Approximately(v, _security01)) return;

        _security01 = v;

        if (securitySlider) securitySlider.value = _security01;
        if (securityPercentText) securityPercentText.text = Mathf.RoundToInt(_security01 * 100f) + "%";

        if (_security01 >= 1f) Win();
    }

    // --- Clue API ---
    public void OnClueCollected()
    {
        if (_ended) return;

        _cluesCollected = Mathf.Clamp(_cluesCollected + 1, 0, totalClues);
        UpdateClueUI();
    }

    private void UpdateClueUI()
    {
        if (clueCountText != null)
            clueCountText.text = $"Clues: {_cluesCollected}/{totalClues}";
    }

    // --- Flow ---
    public void Die()
    {
        if (_ended) return;
        _ended = true;

        Time.timeScale = 0f;
        if (deathPanel) deathPanel.SetActive(true);

        PlatformSpawner.Instance?.StopSpawning();
        InfoSpawner.Instance?.StopSpawning();
    }

    public void Win()
    {
        if (_ended) return;
        _ended = true;

        Time.timeScale = 0f;
        if (winPanel) winPanel.SetActive(true);

        PlatformSpawner.Instance?.StopSpawning();
        InfoSpawner.Instance?.StopSpawning();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}