using UnityEngine;

public sealed class SimpleLaptopInteractor : MonoBehaviour
{
    [Header("Laptop")]
    [SerializeField] private GameObject laptopCanvas;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private MonoBehaviour playerMovement;

    private bool playerInRange;
    private bool laptopOpen;

    private void Awake()
    {
        SetLaptop(false);
    }

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Input.GetKeyDown(interactKey))
            SetLaptop(!laptopOpen);
    }

    private void SetLaptop(bool open)
    {
        laptopOpen = open;

        if (laptopCanvas != null)
            laptopCanvas.SetActive(open);

        if (playerMovement != null)
            playerMovement.enabled = !open;

        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = false;
        SetLaptop(false);
    }
}