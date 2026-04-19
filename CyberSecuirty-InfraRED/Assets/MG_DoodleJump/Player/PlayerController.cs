// DoodleJumpPlayer3D_CC.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class DoodleJumpPlayer3D_CC : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;
    public float maxX = 6f;
    public bool wrapHorizontal = true;

    [Header("Gravity / Bounce")]
    public float gravity = 30f;
    public float extraFallGravity = 10f;
    public float bounceVelocity = 12f;

    [Header("Platforms")]
    public LayerMask platformMask;      // set to ONLY Platform layer
    public bool oneWayPlatforms = true; // collide only while falling
    public float minTopNormal = 0.5f;   // how "top" the hit must be

    CharacterController cc;
    float vy;

    InputAction moveAction;
    float moveX;

    int platformLayer = -1;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/d")
            .With("Positive", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick/x");

        moveAction.performed += ctx => moveX = ctx.ReadValue<float>();
        moveAction.canceled += _ => moveX = 0f;

        platformLayer = LayerMaskToSingleLayer(platformMask);
    }

    void OnEnable() => moveAction.Enable();
    void OnDisable() => moveAction.Disable();

    void Update()
    {
        // One-way: disable collisions while going up
        if (oneWayPlatforms && platformLayer >= 0)
            Physics.IgnoreLayerCollision(gameObject.layer, platformLayer, vy > 0f);

        // gravity
        float g = gravity + (vy < 0f ? extraFallGravity : 0f);
        vy -= g * Time.deltaTime;

        // horizontal + vertical move
        Vector3 motion = new Vector3(moveX * moveSpeed, vy, 0f);
        cc.Move(motion * Time.deltaTime);

        // keep small downward when grounded so CC stays grounded (prevents floating)
        if (cc.isGrounded && vy < 0f) vy = -2f;

        // wrap
        if (wrapHorizontal)
        {
            Vector3 p = transform.position;
            if (p.x > maxX) p.x = -maxX;
            else if (p.x < -maxX) p.x = maxX;
            transform.position = p;
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Bounce only when falling, hitting the TOP of a platform layer collider
        if (vy > 0f) return;
        if (hit.normal.y < minTopNormal) return;
        if (((1 << hit.gameObject.layer) & platformMask.value) == 0) return;

        vy = bounceVelocity;

        var plat = hit.collider.GetComponent<Platform3D>();
        if (plat) plat.OnPlayerBounced();
    }

    int LayerMaskToSingleLayer(LayerMask mask)
    {
        int v = mask.value;
        if (v == 0) return -1;
        if ((v & (v - 1)) != 0) return -1; // must be exactly one layer
        int layer = 0;
        while ((v >>= 1) != 0) layer++;
        return layer;
    }
}