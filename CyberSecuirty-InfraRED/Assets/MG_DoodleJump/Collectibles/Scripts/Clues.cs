// ClueBob.cs (bob + Y rotation)
using UnityEngine;

/// <summary>
/// Clue indicator motion.
/// - Local-space vertical bob.
/// - Constant yaw rotation around local Y.
/// </summary>
[DisallowMultipleComponent]
public sealed class ClueBob : MonoBehaviour
{
    [Header("Bob")]
    [SerializeField, Min(0f)] private float amplitude = 0.35f;
    [SerializeField, Min(0.01f)] private float frequency = 1.25f;

    [Header("Rotate")]
    [Tooltip("Degrees per second around local Y.")]
    [SerializeField] private float yawDegreesPerSecond = 90f;

    private Vector3 _baseLocalPos;

    private void Awake()
    {
        _baseLocalPos = transform.localPosition;
    }

    private void Update()
    {
        float y = Mathf.Sin(Time.time * frequency * (Mathf.PI * 2f)) * amplitude;
        transform.localPosition = new Vector3(_baseLocalPos.x, _baseLocalPos.y + y, _baseLocalPos.z);

        if (yawDegreesPerSecond != 0f)
            transform.Rotate(0f, yawDegreesPerSecond * Time.deltaTime, 0f, Space.Self);
    }
}