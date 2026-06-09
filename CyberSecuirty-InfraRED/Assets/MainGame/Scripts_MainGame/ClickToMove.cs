using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class ClickToMove : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private bool canMove = true;

    [Tooltip("How close we need to be to the target point before stopping.")]
    [SerializeField] private float arriveDistance = 0.15f;

    [Header("NPC Interaction")]
    [SerializeField] private float interactionDistance = 0.2f;

    [Header("FX")]
    [SerializeField] private ParticleSystem walkTrail;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";

    [Header("Destination Arrow (only on walkable NavMesh)")]
    [SerializeField] private GameObject destinationArrowPrefab;
    [SerializeField] private float arrowYOffset = 0.05f;

    [Header("Click Filtering")]
    [Tooltip("Set this to only the Roads layer.")]
    [SerializeField] private LayerMask roadLayerMask;

    [Tooltip("Max raycast distance for clicks.")]
    [SerializeField] private float maxClickDistance = 500f;

    [Tooltip("How far from the click we allow snapping to NavMesh. Smaller = stricter.")]
    [SerializeField] private float navmeshSnapDistance = 1.0f;

    private NavMeshAgent agent;

    private NPCInteract targetNPC;
    private bool hasTargetPoint;

    private GameObject arrowInstance;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        // smoother feel
        agent.acceleration = 25f;
        agent.angularSpeed = 720f;
        agent.autoBraking = true;
        agent.updateRotation = true;

        agent.stoppingDistance = arriveDistance;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (!canMove)
        {
            UpdateAnimationAndTrail(0f);
            return;
        }

        HandleClick();
        UpdateArrivalAndInteract();
        UpdateAnimationAndTrail(agent.velocity.magnitude);
    }

    private void HandleClick()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        // 1) NPC raycast: allow NPC clicks regardless of Roads layer
        if (Physics.Raycast(ray, out RaycastHit npcHit, maxClickDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            NPCInteract npc = npcHit.collider.GetComponentInParent<NPCInteract>();
            if (npc != null)
            {
                targetNPC = npc;

                Vector3 desired = npc.GetInteractPointWorld();
                if (TryGetNavmeshPoint(desired, out Vector3 navPoint))
                    SetMoveTarget(navPoint);
                else
                    ClearArrow();

                return;
            }
        }

        // 2) Ground raycast: ONLY Roads layer
        if (!Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, roadLayerMask, QueryTriggerInteraction.Ignore))
        {
            // clicked non-road => no arrow, no movement
            ClearArrow();
            return;
        }

        // 3) Only move + arrow if we can sample NavMesh (Option A)
        if (TryGetNavmeshPoint(hit.point, out Vector3 navMeshPoint))
        {
            targetNPC = null;
            SetMoveTarget(navMeshPoint);
        }
        else
        {
            ClearArrow();
        }
    }

    private void SetMoveTarget(Vector3 point)
    {
        hasTargetPoint = true;

        agent.isStopped = false;
        agent.stoppingDistance = arriveDistance;
        agent.SetDestination(point);

        SpawnOrMoveArrow(point);
    }

    private void UpdateArrivalAndInteract()
    {
        if (!hasTargetPoint) return;
        if (agent.pathPending) return;

        if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveDistance))
        {
            agent.isStopped = true;
            agent.ResetPath();
            hasTargetPoint = false;

            ClearArrow();

            if (targetNPC != null)
            {
                float d = Vector3.Distance(transform.position, targetNPC.GetInteractPointWorld());
                if (d <= interactionDistance + 0.25f)
                    targetNPC.Interact();

                targetNPC = null;
            }
        }
    }

    private void UpdateAnimationAndTrail(float speed)
    {
        // animator
        if (animator != null && !string.IsNullOrEmpty(speedParam))
        {
            float normalized = agent.speed > 0.01f ? Mathf.Clamp01(speed / agent.speed) : 0f;
            animator.SetFloat(speedParam, normalized);
        }

        // trail
        if (walkTrail == null) return;

        if (speed < 0.1f)
        {
            if (walkTrail.isPlaying) walkTrail.Stop();
            return;
        }

        if (!walkTrail.isPlaying) walkTrail.Play();
    }

    private void SpawnOrMoveArrow(Vector3 point)
    {
        if (destinationArrowPrefab == null) return;

        Vector3 pos = new Vector3(point.x, point.y + arrowYOffset, point.z);

        // only ONE arrow
        if (arrowInstance == null)
            arrowInstance = Instantiate(destinationArrowPrefab, pos, Quaternion.identity);
        else
            arrowInstance.transform.position = pos;
    }

    private void ClearArrow()
    {
        if (arrowInstance != null)
        {
            Destroy(arrowInstance);
            arrowInstance = null;
        }
    }

    private bool TryGetNavmeshPoint(Vector3 worldPoint, out Vector3 navPoint)
    {
        if (NavMesh.SamplePosition(worldPoint, out NavMeshHit hit, navmeshSnapDistance, NavMesh.AllAreas))
        {
            navPoint = hit.position;
            return true;
        }

        navPoint = default;
        return false;
    }

    public void StopMovement()
    {
        canMove = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        hasTargetPoint = false;
        targetNPC = null;

        ClearArrow();
        UpdateAnimationAndTrail(0f);
    }

    public void ResumeMovement()
    {
        canMove = true;
        if (agent != null) agent.isStopped = false;
    }
}