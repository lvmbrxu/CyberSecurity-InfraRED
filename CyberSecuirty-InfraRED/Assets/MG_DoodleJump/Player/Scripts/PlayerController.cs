// PlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class DoodleJumpPlayer3D_CC : MonoBehaviour
{
    public enum HorizontalMode { Clamp, Wrap }

    [Header("Move")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float xLimit = 6f;
    [SerializeField] private HorizontalMode horizontalMode = HorizontalMode.Clamp;

    [Header("Gravity / Bounce")]
    [SerializeField] private float gravity = 30f;
    [SerializeField] private float extraFallGravity = 10f;
    [SerializeField] private float bounceVelocity = 12f;

    [Header("Platforms")]
    [SerializeField] private LayerMask platformMask;
    [SerializeField] private bool oneWayPlatforms = true;

    [Header("Landing Filter (Anti-stuck)")]
    [SerializeField, Min(0f)] private float topSurfaceEpsilon = 0.06f;
    [SerializeField, Range(0f, 1f)] private float minTopNormal = 0.6f;

    [Header("Fall Recovery")]
    [SerializeField] private float respawnYOffset = 2.5f;
    [SerializeField] private float respawnUpVelocity = 10f;

    private CharacterController _cc;
    private float _vy;

    private InputAction _moveAction;
    private float _moveX;

    private int _platformLayer = -1;
    private Vector3 _lastSafePos;

    public float XLimit => xLimit;

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

        if (oneWayPlatforms && _platformLayer >= 0)
            Physics.IgnoreLayerCollision(gameObject.layer, _platformLayer, _vy > 0f);

        float g = gravity + (_vy < 0f ? extraFallGravity : 0f);
        _vy -= g * Time.deltaTime;

        Vector3 motion = new(_moveX * moveSpeed, _vy, 0f);
        _cc.Move(motion * Time.deltaTime);

        if (_cc.isGrounded && _vy < 0f) _vy = -2f;

        ApplyHorizontalBounds();
    }

    private void ApplyHorizontalBounds()
    {
        Vector3 p = transform.position;

        switch (horizontalMode)
        {
            case HorizontalMode.Clamp:
                p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
                transform.position = p;
                break;

            case HorizontalMode.Wrap:
                if (p.x > xLimit) p.x = -xLimit;
                else if (p.x < -xLimit) p.x = xLimit;
                transform.position = p;
                break;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (((1 << hit.gameObject.layer) & platformMask.value) == 0) return;
        if (_vy > 0f) return;
        if (hit.normal.y < minTopNormal) return;

        Bounds b = hit.collider.bounds;
        float contactToTop = b.max.y - hit.point.y;
        if (contactToTop > topSurfaceEpsilon) return;

        _vy = bounceVelocity;
        _lastSafePos = transform.position;

        var plat = hit.collider.GetComponent<Platform3D>();
        if (plat != null) plat.OnPlayerBounced();
    }

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
        if ((v & (v - 1)) != 0) return -1;
        int layer = 0;
        while ((v >>= 1) != 0) layer++;
        return layer;
    }
}