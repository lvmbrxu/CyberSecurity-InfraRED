using UnityEngine;

public sealed class CameraShake : MonoBehaviour
{
    [Header("Defaults")]
    public float baseDuration = 0.08f;
    public float baseStrength = 0.12f;

    Vector3 startLocalPos;
    float timeLeft;
    float strength;

    void Awake()
    {
        startLocalPos = transform.localPosition;
    }

    public void Kick(float strengthMultiplier = 1f, float durationMultiplier = 1f)
    {
        strength = baseStrength * Mathf.Max(0f, strengthMultiplier);
        timeLeft = baseDuration * Mathf.Max(0f, durationMultiplier);
    }

    void LateUpdate()
    {
        if (timeLeft <= 0f)
        {
            transform.localPosition = startLocalPos;
            return;
        }

        timeLeft -= Time.deltaTime;

        // Small random offset
        Vector2 r = Random.insideUnitCircle * strength;
        transform.localPosition = startLocalPos + new Vector3(r.x, r.y, 0f);

        if (timeLeft <= 0f)
            transform.localPosition = startLocalPos;
    }
}