using UnityEngine;

public class PlatformMover : MonoBehaviour
{
    [Header("Move settings")]
    [SerializeField] private bool moving;
    [SerializeField] private float amplitude = 2.0f; // left/right distance
    [SerializeField] private float speed = 1.2f;     // movement speed
    [SerializeField] private bool randomPhase = true;

    private Vector3 startPos;
    private float phase;

    private void Awake()
    {
        startPos = transform.position;
        phase = randomPhase ? Random.Range(0f, 100f) : 0f;
    }

    private void OnEnable()
    {
        // good for pooled/reused platforms
        startPos = transform.position;
        if (randomPhase) phase = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (!moving) return;

        float xOffset = Mathf.Sin((Time.time + phase) * speed) * amplitude;
        transform.position = new Vector3(startPos.x + xOffset, transform.position.y, transform.position.z);
    }

    public void SetMoving(bool enable, float amp, float spd)
    {
        moving = enable;
        amplitude = amp;
        speed = spd;
        startPos = transform.position; // prevents “jump”
    }
}