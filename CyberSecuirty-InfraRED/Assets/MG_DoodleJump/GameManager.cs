using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GamePhase { Phase1_SecurityRun, Phase2_IdHunt, Ended }

    [Header("Refs")]
    [SerializeField] private DoodleJumpPlayer3D_CC player;
    [SerializeField] private FollowCameraY followCam;
    [SerializeField] private PlatformSpawner platformSpawner;
    [SerializeField] private PhaseTransition transition;

    [Header("Camera Background (optional)")]
    [SerializeField] private CameraBackgroundSwap cameraBackgroundSwap;

    [Header("UI - Phase 1 (Security)")]
    [SerializeField] private CanvasGroup securityUi;
    [SerializeField] private Slider securitySlider;
    [SerializeField] private TMP_Text securityPercentText;

    [Header("UI - Phase 2 (IDs)")]
    [SerializeField] private CanvasGroup idUi;
    [SerializeField] private TMP_Text idCountText;

    [Header("Panels")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject winPanel;

    [Header("Security (Phase 1)")]
    [SerializeField, Range(0f, 1f)] private float startSecurity01 = 0.10f;
    [SerializeField, Range(0.01f, 1f)] private float fallPenalty01 = 0.10f;
    [SerializeField, Min(1f)] private float unitsToFullSecurity = 900f;

    [Header("Fall Rules")]
    [SerializeField, Min(0f)] private float fallBelowScreen = 2.5f;
    [SerializeField, Min(0f)] private float fallUnlockMargin = 1.5f;

    [Header("IDs (Phase 2)")]
    [SerializeField, Min(1)] private int idsRequired = 3;

    [Header("Phase 2 optional consequence")]
    [Tooltip("If enabled, phase 2 will force moving platforms ON (in addition to NPC choice).")]
    [SerializeField] private bool forceMovingPlatformsInPhase2 = false;

    public GamePhase Phase { get; private set; } = GamePhase.Phase1_SecurityRun;
    public bool HasEnded => Phase == GamePhase.Ended;

    public float Security01 => security01;
    public int IdsCollected => idsCollected;
    public int IdsRequired => idsRequired;

    private float security01;
    private float runStartY;
    private float maxY;
    private bool fallLock;

    private int idsCollected;

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
        if (platformSpawner == null) platformSpawner = FindFirstObjectByType<PlatformSpawner>();

        if (deathPanel) deathPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);

        runStartY = player != null ? player.transform.position.y : 0f;
        maxY = runStartY;
        fallLock = false;

        idsCollected = 0;

        ApplyPresentationForPhase(GamePhase.Phase1_SecurityRun);
        cameraBackgroundSwap?.SetPhase1();

        SetSecurity(startSecurity01, force: true);
        UpdateIdUI();
    }

    private void Update()
    {
        if (HasEnded) return;
        if (player == null || followCam == null) return;

        float y = player.transform.position.y;
        HandleFall(y);

        if (Phase == GamePhase.Phase1_SecurityRun)
        {
            if (y > maxY)
            {
                maxY = y;
                float dist01 = Mathf.Clamp01((maxY - runStartY) / unitsToFullSecurity);
                if (dist01 > security01) SetSecurity(dist01);
            }

            if (security01 >= 1f)
                BeginPhase2();
        }
    }

    // ---------------- Security ----------------

    private void SetSecurity(float value01, bool force = false)
    {
        float v = Mathf.Clamp01(value01);
        if (!force && Mathf.Approximately(v, security01)) return;

        security01 = v;

        if (securitySlider) securitySlider.value = security01;

        if (securityPercentText)
        {
            int pct = Mathf.RoundToInt(security01 * 100f);
            securityPercentText.text = $"Secure yourself : {pct}%";
        }
    }

    // ---------------- Fall handling ----------------

    private void HandleFall(float playerY)
    {
        float fallLine = followCam.BottomY - fallBelowScreen;

        if (fallLock && playerY > (fallLine + fallUnlockMargin))
            fallLock = false;

        if (!fallLock && playerY < fallLine)
        {
            fallLock = true;
            ApplyFallPenaltyAndRecover();
        }
    }

    private void ApplyFallPenaltyAndRecover()
    {
        SetSecurity(security01 - fallPenalty01);

        if (security01 <= 0f)
        {
            Die();
            return;
        }

        player.RecoverFromFall();
    }

    // ---------------- Phase 2 ----------------

    private void BeginPhase2()
    {
        if (Phase != GamePhase.Phase1_SecurityRun) return;
        if (transition != null && transition.IsBusy) return;

        System.Action swap = () =>
        {
            Phase = GamePhase.Phase2_IdHunt;
            ApplyPresentationForPhase(GamePhase.Phase2_IdHunt);

            cameraBackgroundSwap?.SetPhase2();

            platformSpawner?.SwapAllPlatformsToPhase2Global();
            platformSpawner?.EnterPhase2(idsRequired);

            idsCollected = 0;
            UpdateIdUI();
        };

        if (transition != null)
            StartCoroutine(transition.FadeSwap(swap));
        else
            swap.Invoke();
    }

    public void OnIdCollected()
    {
        if (HasEnded) return;
        if (Phase != GamePhase.Phase2_IdHunt) return;

        idsCollected = Mathf.Clamp(idsCollected + 1, 0, idsRequired);
        UpdateIdUI();

        if (idsCollected >= idsRequired)
            Win();
    }

    private void UpdateIdUI()
    {
        if (idCountText != null)
            idCountText.text = $"Collect all IDs: {idsCollected}/{idsRequired}";
    }

    // ---------------- Presentation ----------------

    private void ApplyPresentationForPhase(GamePhase phase)
    {
        SetCanvasGroup(securityUi, phase == GamePhase.Phase1_SecurityRun);
        SetCanvasGroup(idUi, phase == GamePhase.Phase2_IdHunt);
    }

    private static void SetCanvasGroup(CanvasGroup cg, bool on)
    {
        if (cg == null) return;
        cg.alpha = on ? 1f : 0f;
        cg.interactable = on;
        cg.blocksRaycasts = on;
    }

    // ---------------- End states ----------------

    public void Die()
    {
        if (HasEnded) return;
        Phase = GamePhase.Ended;

        Time.timeScale = 0f;
        if (deathPanel) deathPanel.SetActive(true);

        platformSpawner?.StopSpawning();
        InfoSpawner.Instance?.StopSpawning();
    }

    public void Win()
    {
        if (HasEnded) return;
        Phase = GamePhase.Ended;

        Time.timeScale = 0f;
        if (winPanel) winPanel.SetActive(true);

        platformSpawner?.StopSpawning();
        InfoSpawner.Instance?.StopSpawning();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}