// GameManager.cs (debounced fall penalty)
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Security-driven loop.
/// - Security (0..1): distance + pickups.
/// - Fall: -security ONCE per fall, recover if > 0.
/// - Death only at 0.
/// - Win at 1 (spawners stop).
/// </summary>
[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private DoodleJumpPlayer3D_CC player;
    [SerializeField] private FollowCameraY followCam;

    [Header("UI")]
    [SerializeField] private Slider securitySlider; // Min=0 Max=1
    [SerializeField] private TMP_Text securityPercentText;

    [Header("Panels")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject winPanel;

    [Header("Security")]
    [SerializeField, Range(0f, 1f)] private float startSecurity01 = 0.25f;
    [SerializeField, Range(0.01f, 1f)] private float fallPenalty01 = 0.10f;
    [SerializeField, Min(1f)] private float unitsToFullSecurity = 450f;

    [Header("Fall Rules")]
    [Tooltip("How far below the camera bottom counts as a fall.")]
    [SerializeField, Min(0f)] private float fallBelowScreen = 2.5f;

    [Tooltip("How far ABOVE the fall line the player must return before another fall can be counted.")]
    [SerializeField, Min(0f)] private float fallUnlockMargin = 1.5f;

    private float _security01;
    private float _runStartY;
    private float _maxY;
    private bool _ended;

    // Debounce: true after a fall is processed; cleared once player is safely back above the threshold.
    private bool _fallLock;

    public bool HasEnded => _ended;
    public float Security01 => _security01;
    public float TargetEndY => _runStartY + unitsToFullSecurity;

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

        SetSecurity(startSecurity01, force: true);
    }

    private void Update()
    {
        if (_ended) return;
        if (player == null || followCam == null) return;

        // Distance-based security uses maxY only.
        float y = player.transform.position.y;
        if (y > _maxY)
        {
            _maxY = y;
            float dist01 = Mathf.Clamp01((_maxY - _runStartY) / unitsToFullSecurity);
            if (dist01 > _security01) SetSecurity(dist01);
        }

        // Fall evaluation.
        float bottomY = followCam.BottomY;
        float fallLine = bottomY - fallBelowScreen;

        // Unlock once the player is safely above the fall line again.
        if (_fallLock && y > (fallLine + fallUnlockMargin))
            _fallLock = false;

        // Process fall ONCE.
        if (!_fallLock && y < fallLine)
        {
            _fallLock = true;
            ApplyFallPenaltyAndRecover();
        }
    }

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

        // Recovery teleports the player back to last safe position (player script).
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

    public void Die()
    {
        if (_ended) return;
        _ended = true;

        Time.timeScale = 0f;
        if (deathPanel) deathPanel.SetActive(true);

        PlatformSpawner.Instance?.StopSpawning();
    }

    public void Win()
    {
        if (_ended) return;
        _ended = true;

        Time.timeScale = 0f;
        if (winPanel) winPanel.SetActive(true);

        PlatformSpawner.Instance?.StopSpawning();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}