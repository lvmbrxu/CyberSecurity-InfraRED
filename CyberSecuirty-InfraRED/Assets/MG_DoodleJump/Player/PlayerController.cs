// PlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// DoodleJump-style 3D CharacterController.
/// - New Input System (embedded InputAction, zero GC per-frame).
/// - Bounce logic via OnControllerColliderHit (CharacterController correct).
/// - One-way platforms supported (ignore while moving up).
/// - Tracks last safe landing point for fall recovery.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float maxX = 6f;
    [SerializeField] private bool wrapHorizontal = true;

    [Header("Gravity / Bounce")]
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float extraFallGravity = 10f;
    [SerializeField] private float bounceVelocity = 12f;

    [Header("Platforms")]
    [Tooltip("Set to ONLY Platform layer.")]
    [SerializeField] private LayerMask platformMask;
    [SerializeField] private bool oneWayPlatforms = true;
    [SerializeField, Range(0f, 1f)] private float minTopNormal = 0.5f;

    [Header("Fall Recovery")]
    [Tooltip("How high above last safe point to respawn.")]
    [SerializeField] private float respawnYOffset = 2.5f;
    [Tooltip("Up velocity given on respawn to re-enter gameplay.")]
    [SerializeField] private float respawnUpVelocity = 10f;

    private CharacterController _cc;
    private float _vy;

    // Input (no PlayerInput component required)
    private InputAction _moveAction;
    private float _moveX;

    // One-way platform collision toggle needs a single layer index
    private int _platformLayer = -1;

    // Last safe landing point (updated on bounce)
    private Vector3 _lastSafePos;

    public float VerticalVelocity => _vy;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _lastSafePos = transform.position;

        _moveAction = new InputAction("Move", InputActionType.Value);
        _moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/d")
            .With("Positive", "<Keyboard>/rightArrow");
        _moveAction.AddBinding("<Gamepad>/leftStick/x");

        _moveAction.performed += ctx => _moveX = ctx.ReadValue<float>();
        _moveAction.canceled += _ => _moveX = 0f;

        _platformLayer = LayerMaskToSingleLayer(platformMask);
    }

    private void OnEnable() => _moveAction.Enable();
    private void OnDisable() => _moveAction.Disable();

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasEnded) return;

        // One-way: ignore platform collisions while moving upward.
        if (oneWayPlatforms && _platformLayer >= 0)
            Physics.IgnoreLayerCollision(gameObject.layer, _platformLayer, _vy > 0f);

        // Gravity.
        float g = gravity + (_vy < 0f ? extraFallGravity : 0f);
        _vy -= g * Time.deltaTime;

        // Move CC.
        Vector3 motion = new(_moveX * moveSpeed, _vy, 0f);
        _cc.Move(motion * Time.deltaTime);

        // Keep slight downward force when grounded so CC stays grounded.
        if (_cc.isGrounded && _vy < 0f) _vy = -2f;

        // Horizontal wrap (your original behavior).
        if (wrapHorizontal)
        {
            Vector3 p = transform.position;
            if (p.x > maxX) p.x = -maxX;
            else if (p.x < -maxX) p.x = maxX;
            transform.position = p;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Bounce only when falling AND landing on top of a platform layer collider.
        if (_vy > 0f) return;
        if (hit.normal.y < minTopNormal) return;
        if (((1 << hit.gameObject.layer) & platformMask.value) == 0) return;

        _vy = bounceVelocity;

        // Record last safe point at landing (used for fall recover).
        _lastSafePos = transform.position;

        // Optional platform behavior.
        var plat = hit.collider.GetComponent<Platform>();
        if (plat != null) plat.OnPlayerBounced();
    }

    /// <summary>
    /// Called by GameManager after fall penalty (if still alive).
    /// Teleport is intentional recovery, but gameplay continues via upward velocity.
    /// </summary>
    public void RecoverFromFall()
    {
        Vector3 pos = _lastSafePos;
        pos.y += respawnYOffset;

        _vy = respawnUpVelocity;

        _cc.enabled = false;
        transform.position = pos;
        _cc.enabled = true;
    }

    private static int LayerMaskToSingleLayer(LayerMask mask)
    {
        int v = mask.value;
        if (v == 0) return -1;
        if ((v & (v - 1)) != 0) return -1; // must be exactly one layer
        int layer = 0;
        while ((v >>= 1) != 0) layer++;
        return layer;
    }
}