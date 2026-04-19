using UnityEngine;
using UnityEngine.SceneManagement;

public class OTEScript : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private GameObject lightOnn;
    [SerializeField] private GameObject lightOff;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered");
        SceneManager.LoadScene(sceneToLoad);
        lightOnn.SetActive(false);
        lightOff.SetActive(true);
    }
}
