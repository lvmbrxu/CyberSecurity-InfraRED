using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


public class ClickToMove : MonoBehaviour
{
    [SerializeField] private InputAction mouseClickAction;
    private Camera mainCamera;
    private Coroutine coroutine;
    [SerializeField] private float playerSpeed;
    private Vector3 targetPosition;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        mouseClickAction.Enable();
        mouseClickAction.performed += Move;
        
    }

    private void OnDisable()
    {
        mouseClickAction.performed -= Move;
        mouseClickAction.Disable();
    }

    private void Move(InputAction.CallbackContext context)
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray: ray, hitInfo: out RaycastHit hit) && hit.collider)
        {
            if (coroutine != null) StopCoroutine(coroutine);
            coroutine = StartCoroutine(PlayerMoveTowards(hit.point));
            targetPosition = hit.point;
        }
    }

    private IEnumerator PlayerMoveTowards(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.1f);
        Vector3 destination = Vector3.MoveTowards(transform.position, target, playerSpeed * Time.deltaTime);
        transform.position = destination;
        yield return null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPosition, 1);
    }
}
