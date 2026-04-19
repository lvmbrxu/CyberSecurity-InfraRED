// FinishPlatform.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishPlatform : MonoBehaviour
{
    void Reset() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (!GameManager.I) return;
        if (other.transform == GameManager.I.player)
            GameManager.I.Win();
    }
}