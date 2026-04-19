using System;
using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    public GameObject dialogueUI;
    private ClickToMove playerMovement;
    

    private void Start()
    {
        playerMovement = FindObjectOfType<ClickToMove>();
    }

    public void Interact()
    {
        playerMovement.StopMovement();
        dialogueUI.SetActive(true);
    }
}
