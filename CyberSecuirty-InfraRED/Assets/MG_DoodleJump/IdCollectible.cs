// IdCollectible.cs
// Put on your ID prefab.
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class IdCollectible : MonoBehaviour
{
    [SerializeField] private AudioSource sfx;
    private bool _collected;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        if (!TryGetComponent<Rigidbody>(out var rb))
            rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        if (other.GetComponentInParent<DoodleJumpPlayer3D_CC>() == null) return;

        _collected = true;
        GameManager.Instance?.OnIdCollected();

        if (sfx != null)
        {
            sfx.Play();
            if (TryGetComponent<Collider>(out var c)) c.enabled = false;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            Destroy(gameObject, (sfx.clip != null) ? sfx.clip.length : 0f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}