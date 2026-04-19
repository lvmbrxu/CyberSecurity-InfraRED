using UnityEngine;

public class UISound : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip clickClip;

    public void PlayClick()
    {
        source.PlayOneShot(clickClip);
    }
}