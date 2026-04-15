// PlayerController3D_NoHeadBump.cs
// Prevents "head-bumping" by only allowing platforms to collide with the player when the player is falling.
// Requires: Player has Rigidbody + Collider. Platforms have Collider (non-trigger).
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerController3D_NoHeadBump : MonoBehaviour
{
    [Header("Assign your existing controller if you want, or just use this for the collision rule.")]
    public LayerMask platformMask;          // set to Platform layer
    public float bounceVelocity = 12f;
    public float extraFallGravity = 20f;

    Rigidbody rb;
    Collider playerCol;
    int platformLayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCol = GetComponent<Collider>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        platformLayer = LayerMaskToSingleLayer(platformMask);
        if (platformLayer >= 0)
            Physics.IgnoreLayerCollision(gameObject.layer, platformLayer, false);
    }

    void FixedUpdate()
    {
        // Only collide with platforms while falling (so you pass up through them)
        if (platformLayer >= 0)
            Physics.IgnoreLayerCollision(gameObject.layer, platformLayer, rb.linearVelocity.y > 0f);

        if (rb.linearVelocity.y < 0f && extraFallGravity > 0f)
            rb.AddForce(Vector3.down * extraFallGravity, ForceMode.Acceleration);
    }

    void OnCollisionEnter(Collision c)
    {
        // Bounce only when landing on a platform (falling + contact normal points up)
        if (((1 << c.gameObject.layer) & platformMask.value) == 0) return;
        if (rb.linearVelocity.y > 0f) return;

        // require a mostly-upward contact (we hit the top)
        bool topHit = false;
        for (int i = 0; i < c.contactCount; i++)
        {
            if (c.contacts[i].normal.y > 0.5f) { topHit = true; break; }
        }
        if (!topHit) return;

        var v = rb.linearVelocity;
        v.y = bounceVelocity;
        rb.linearVelocity = v;

        var plat = c.collider.GetComponent<Platform3D>();
        if (plat) plat.OnPlayerBounced();
    }

    int LayerMaskToSingleLayer(LayerMask mask)
    {
        int v = mask.value;
        if (v == 0) return -1;
        if ((v & (v - 1)) != 0) return -1; // more than one bit set
        int layer = 0;
        while ((v >>= 1) != 0) layer++;
        return layer;
    }
}