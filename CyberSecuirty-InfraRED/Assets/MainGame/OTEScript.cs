using UnityEngine;
using UnityEngine.SceneManagement;

public class OTEScript : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered");
        SceneManager.LoadScene(sceneToLoad);
    }
}
