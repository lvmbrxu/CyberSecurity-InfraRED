// PlayerController3D_NewInput.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D_NewInput : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 8f;
    public float maxX = 6f;
    public bool wrapHorizontal = true;

    [Header("Jump")]
    public float bounceVelocity = 12f;
    public float extraFallGravity = 20f;

    [Header("Ground Probe")]
    public LayerMask platformMask;
    public float probeRadius = 0.35f;
    public float probeDistance = 0.9f;

    Rigidbody rb;

    InputAction moveAction;
    float moveX;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Keyboard A/D + Left/Right, Gamepad left stick X
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/d")
            .With("Positive", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick/x");

        moveAction.performed += ctx => moveX = ctx.ReadValue<float>();
        moveAction.canceled += _ => moveX = 0f;
    }

    void OnEnable() => moveAction.Enable();
    void OnDisable() => moveAction.Disable();

    void FixedUpdate()
    {
        // Horizontal move (X axis)
        Vector3 v = rb.linearVelocity;
        v.x = moveX * moveSpeed;
        rb.linearVelocity = v;

        // Faster fall (optional, makes it feel snappier)
        if (rb.linearVelocity.y < 0f && extraFallGravity > 0f)
            rb.AddForce(Vector3.down * extraFallGravity, ForceMode.Acceleration);

        if (wrapHorizontal)
        {
            Vector3 p = transform.position;
            if (p.x > maxX) p.x = -maxX;
            else if (p.x < -maxX) p.x = maxX;
            transform.position = p;
        }

        TryBounce();
    }

    void TryBounce()
    {
        if (rb.linearVelocity.y > 0f) return; // only bounce while falling

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.SphereCast(origin, probeRadius, Vector3.down, out RaycastHit hit, probeDistance, platformMask, QueryTriggerInteraction.Ignore))
        {
            // Only accept if we are above the top surface-ish
            if (transform.position.y < hit.point.y - 0.05f) return;

            // Apply bounce
            Vector3 v = rb.linearVelocity;
            v.y = bounceVelocity;
            rb.linearVelocity = v;

            // Breakable platform hook
            var plat = hit.collider.GetComponent<Platform3D>();
            if (plat) plat.OnPlayerBounced();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawWireSphere(origin + Vector3.down * probeDistance, probeRadius);
    }
}