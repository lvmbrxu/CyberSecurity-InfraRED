using UnityEngine;
using UnityEngine.SceneManagement;

public class OTEScript : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private GameObject lightOnn;
    [SerializeField] private GameObject lightOff;
    [SerializeField] private GameObject lightOnn2;
    [SerializeField] private GameObject lightOff2;
    [SerializeField] private GameObject lightObjectOnn;
    [SerializeField] private GameObject lightObjectOff;

    [SerializeField] private GameObject nextGameGL;
    [SerializeField] private GameObject nextGameGL2;
    [SerializeField] private GameObject nextGameRL;
    [SerializeField] private GameObject nextGameRL2;
    [SerializeField] private GameObject nextGameObjectOnn;
    [SerializeField] private GameObject nextGameObjectOff;

    [SerializeField] private GameObject invisWall;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Entered");
        SceneManager.LoadScene(sceneToLoad);
        lightOnn.SetActive(false);
        lightOff.SetActive(true);
        lightOnn2.SetActive(false);
        lightOff2.SetActive(true);
        lightObjectOnn.SetActive(false);
        lightObjectOff.SetActive(true);
        
        nextGameGL.SetActive(true);
        nextGameGL2.SetActive(true);
        nextGameRL.SetActive(false);
        nextGameRL2.SetActive(false);
        nextGameObjectOnn.SetActive(true);
        nextGameObjectOff.SetActive(false);
        
        invisWall.SetActive(false);
    }
}
