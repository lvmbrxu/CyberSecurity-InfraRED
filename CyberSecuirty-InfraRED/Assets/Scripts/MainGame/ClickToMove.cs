using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;


public class ClickToMove : MonoBehaviour
{
    private NavMeshAgent agent;
    
    public float moveSpeed;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        
        agent.speed = moveSpeed;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, Mathf.Infinity, 1))
                {
                    agent.SetDestination(navMeshHit.position);
                }
                else Debug.Log("clicked point is not a walkable area");
            }
        }
    }
}
