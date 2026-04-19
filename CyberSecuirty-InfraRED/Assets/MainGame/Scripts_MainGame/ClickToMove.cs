using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;


public class ClickToMove : MonoBehaviour
{
    private NavMeshAgent agent;
    
    public float moveSpeed;
    private bool canMove = true;

    private NPCInteract targetNPC;
    public float interactionDistance;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
    }

    void Update()
    {
        if (!canMove) return;
        HandleClick();
        CheckInteractionDistance();
    }

    void HandleClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                NPCInteract npc = hit.collider.gameObject.GetComponent<NPCInteract>();
                if (npc != null)
                {
                    targetNPC = npc;
                    agent.SetDestination(npc.transform.position);
                    return;
                }
                
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, Mathf.Infinity, 1))
                {
                    targetNPC = null;
                    agent.SetDestination(navMeshHit.position);
                }
                else Debug.Log("clicked point is not a walkable area");
            }
        }
    }

    void CheckInteractionDistance()
    {
        if (targetNPC == null) return;
        if (!agent.pathPending && agent.remainingDistance <= interactionDistance)
        {
            agent.isStopped = true;
            agent.ResetPath();
            
            targetNPC.Interact();
            targetNPC = null;
        }
    }
    public void StopMovement()
    {
        canMove = false;
        agent.isStopped = true;
        agent.ResetPath();
    }
    public void ResumeMovement()
    {
        canMove = true;
        agent.isStopped = false;
    }
}
