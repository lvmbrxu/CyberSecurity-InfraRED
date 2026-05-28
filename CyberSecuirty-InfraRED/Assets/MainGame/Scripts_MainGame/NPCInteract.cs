using System;
using UnityEngine;
using UnityEngine.AI;

public class NPCInteract : MonoBehaviour
{
    public Transform[] waypoints;
    public Transform player;

    public float detectionRange;
    public float loseRange;
    public float returnDelay;
    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private enum State {Patrol, Alert, Return}
    private State currentState = State.Patrol;
    private float lostTimer;
    
    public GameObject dialogueUI;
    private ClickToMove playerMovement;
    

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        GoToNextWaypoint();
        playerMovement = FindObjectOfType<ClickToMove>();
    }

    private void Update()
    {
        float distanceToPlayer = Vector3.Distance(player.position, transform.position);
        switch (currentState)
        {
            case State.Patrol:
                PatrolUpdate();

                if (distanceToPlayer <= detectionRange)
                {
                    currentState = State.Alert;
                    agent.isStopped = true;
                }
                break;
            
            case State.Alert:
                LookAtPlayer();
                if (distanceToPlayer > loseRange)
                {
                    currentState = State.Return;
                    lostTimer = 1;
                }
                break;
            
            case State.Return:
                lostTimer += Time.deltaTime;

                if (distanceToPlayer <= detectionRange)
                {
                    currentState = State.Alert;
                    agent.isStopped = true;
                    return;
                }

                if (lostTimer >= returnDelay)
                {
                    currentState = State.Patrol;
                    agent.isStopped = false;
                    GoToNextWaypoint();
                }
                break;
        }
    }

    void PatrolUpdate()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            GoToNextWaypoint();
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        agent.SetDestination(waypoints[currentWaypointIndex].position);
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    void LookAtPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    public void Interact()
    {
        playerMovement.StopMovement();
        dialogueUI.SetActive(true);
    }
}
