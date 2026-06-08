using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    public enum GamePhase { Phase1_SecurityRun, Phase2_IdHunt, Ended }

    [Header("Refs")]
    [SerializeField] private DoodleJumpPlayer3D_CC player;
    [SerializeField] private FollowCameraY followCam;
    [SerializeField] private PlatformSpawner platformSpawner;
    [SerializeField] private PhaseTransition transition;

    [Header("Background (Quad child of camera)")]
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

    [Header("Security")]
    [SerializeField, Range(0f, 1f)] private float startSecurity01 = 0.10f;
    [SerializeField, Range(0.01f, 1f)] private float fallPenalty01 = 0.10f;
    [SerializeField, Min(1f)] private float unitsToFullSecurity = 300f;

    [Header("Fall Rules")]
    [SerializeField, Min(0f)] private float fallBelowScreen = 2.5f;
    [SerializeField, Min(0f)] private float fallUnlockMargin = 1.5f;

    [Header("IDs")]
    [SerializeField, Min(1)] private int idsRequired = 3;

    public GamePhase Phase { get; private set; } = GamePhase.Phase1_SecurityRun;
    public bool HasEnded => Phase == GamePhase.Ended;

    private float security01;
    private float runStartY;
    private float maxY;
    private bool fallLock;
    private int idsCollected;

    private void Awake()
    {
        // Kill if mistakenly persisted
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
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<DoodleJumpPlayer3D_CC>();
        if (followCam == null) followCam = Camera.main != null ? Camera.main.GetComponent<FollowCameraY>() : null;
        if (platformSpawner == null) platformSpawner = FindFirstObjectByType<PlatformSpawner>();

        if (deathPanel) deathPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);

        Phase = GamePhase.Phase1_SecurityRun;

        runStartY = player != null ? player.transform.position.y : 0f;
        maxY = runStartY;
        fallLock = false;

        idsCollected = 0;

        ApplyPhasePresentation(GamePhase.Phase1_SecurityRun);
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

    public void AddSecurityDelta01(float delta01)
    {
        if (HasEnded) return;
        SetSecurity(security01 + delta01);
    }

    private void SetSecurity(float value01, bool force = false)
    {
        float v = Mathf.Clamp01(value01);
        if (!force && Mathf.Approximately(v, security01)) return;

        security01 = v;

        if (securitySlider) securitySlider.value = security01;
        if (securityPercentText) securityPercentText.text = $"{Mathf.RoundToInt(security01 * 100f)}%";
    }

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

    private void BeginPhase2()
    {
        if (Phase != GamePhase.Phase1_SecurityRun) return;
        if (transition != null && transition.IsBusy) return;

        System.Action swap = () =>
        {
            Phase = GamePhase.Phase2_IdHunt;
            ApplyPhasePresentation(GamePhase.Phase2_IdHunt);

            cameraBackgroundSwap?.SetPhase2();

            platformSpawner?.SwapAllPlatformsToPhase2Global();
            platformSpawner?.EnterPhase2(idsRequired);

            idsCollected = 0;
            UpdateIdUI();
        };

        if (transition != null) StartCoroutine(transition.FadeSwap(swap));
        else swap.Invoke();
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

    private void ApplyPhasePresentation(GamePhase phase)
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
        Time.timeScale = 1f;
        if (Instance == this) Instance = null;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }
}