// GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Security-driven game loop.
/// - Security increases with upward progress (max Y).
/// - Pickups add/sub security.
/// - Falling below screen removes fixed security and recovers (no death).
/// - Only when security hits 0% => Die.
/// - At 100% => Win and stop spawners.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private PlayerController player;
    [SerializeField] private FollowCamera followCam;

    [Header("UI")]
    [SerializeField] private Slider securitySlider;     // Min=0 Max=1
    [SerializeField] private TMP_Text securityPercentText;

    [Header("Panels")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject winPanel;

    [Header("Security")]
    [SerializeField, Range(0f, 1f)] private float startSecurity01 = 0.25f;
    [SerializeField, Range(0.01f, 1f)] private float fallPenalty01 = 0.10f;
    [Tooltip("World units of upward progress required to reach 100% via distance alone.")]
    [SerializeField, Min(1f)] private float unitsToFullSecurity = 120f;

    [Header("Fall check")]
    [SerializeField, Min(0f)] private float killBelowScreen = 2.5f;

    private float _security01;
    private float _runStartY;
    private float _maxY;
    private bool _ended;

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
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (followCam == null) followCam = Camera.main != null ? Camera.main.GetComponent<FollowCamera>() : null;

        if (deathPanel) deathPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);

        _runStartY = player != null ? player.transform.position.y : 0f;
        _maxY = _runStartY;

        SetSecurity(startSecurity01, force: true);
    }

    private void Update()
    {
        if (_ended) return;
        if (player == null || followCam == null) return;

        // Distance-based security: uses highest Y only (no farming via falling).
        float y = player.transform.position.y;
        if (y > _maxY)
        {
            _maxY = y;

            float dist = _maxY - _runStartY;
            float dist01 = Mathf.Clamp01(dist / unitsToFullSecurity);

            // Only push up to distance value (pickups can still exceed it).
            if (dist01 > _security01)
                SetSecurity(dist01);
        }

        // Fall check: penalty + recover; death only at 0%.
        float bottomY = followCam.BottomY;
        if (player.transform.position.y < bottomY - killBelowScreen)
        {
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

        player.RecoverFromFall();
    }

    private void SetSecurity(float value01, bool force = false)
    {
        float v = Mathf.Clamp01(value01);
        if (!force && Mathf.Approximately(v, _security01)) return;

        _security01 = v;

        if (securitySlider) securitySlider.value = _security01;
        if (securityPercentText) securityPercentText.text = Mathf.RoundToInt(_security01 * 100f) + "%";

        if (_security01 >= 1f)
            Win();
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