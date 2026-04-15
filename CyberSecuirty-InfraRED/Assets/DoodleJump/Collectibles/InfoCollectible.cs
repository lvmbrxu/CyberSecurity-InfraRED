// CircleCollectible.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InfoCollectible : MonoBehaviour
{
    public int id = 1;

    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (!GameManager.I) return;
        if (other.transform != GameManager.I.player) return;

        GameManager.I.SetDebug(id.ToString());
        Destroy(gameObject);
    }
}