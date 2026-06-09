using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform interactPoint;

    // This returns the exact spot ClickToMove should go to.
    public Vector3 GetInteractPointWorld()
    {
        return interactPoint != null ? interactPoint.position : transform.position;
    }

    public void Interact()
    {
        // your existing interaction logic here
    }
}