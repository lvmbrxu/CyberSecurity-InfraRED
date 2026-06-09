using UnityEngine;

public class IdleFidgetTimer : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "IdleFidget";

    [Header("Timing")]
    [Tooltip("Average time between fidgets.")]
    [SerializeField] private float baseInterval = 10f;

    [Tooltip("Random extra +/- seconds added to baseInterval.")]
    [SerializeField] private float randomJitter = 2f;

    [Header("Only fidget when not moving")]
    [SerializeField] private bool onlyWhenIdle = true;
    [SerializeField] private float movementThreshold = 0.05f;

    private Vector3 lastPos;
    private float timer;
    private float nextFireTime;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        lastPos = transform.position;
        ScheduleNext();
    }

    private void Update()
    {
        if (animator == null) return;

        timer += Time.deltaTime;

        if (timer < nextFireTime) return;

        if (onlyWhenIdle && IsMoving())
        {
            // If moving, delay a bit and try again soon
            timer = 0f;
            nextFireTime = 1.0f;
            lastPos = transform.position;
            return;
        }

        animator.SetTrigger(triggerName);

        timer = 0f;
        ScheduleNext();
        lastPos = transform.position;
    }

    private void ScheduleNext()
    {
        nextFireTime = baseInterval + Random.Range(-randomJitter, randomJitter);
        if (nextFireTime < 1f) nextFireTime = 1f;
    }

    private bool IsMoving()
    {
        float dist = (transform.position - lastPos).magnitude;
        lastPos = transform.position;
        return dist > movementThreshold;
    }
}