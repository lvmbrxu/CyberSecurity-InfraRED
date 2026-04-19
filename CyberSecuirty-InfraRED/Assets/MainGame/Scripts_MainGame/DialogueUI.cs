using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    public GameObject panel;
    public ClickToMove playerMovement;

    public void CloseDialogue()
    {
        panel.SetActive(false);
        playerMovement.ResumeMovement();
    }

    public void Option1()
    {
        Debug.Log("Option 1");
        CloseDialogue();
    }

    public void Option2()
    {
        Debug.Log("Option 2");
        CloseDialogue();
    }
}
