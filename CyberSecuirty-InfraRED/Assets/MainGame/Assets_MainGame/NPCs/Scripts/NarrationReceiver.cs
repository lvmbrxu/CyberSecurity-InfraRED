using UnityEngine;
using UnityEngine.Playables;

public class NarrationEventReceiver : MonoBehaviour
{
    [Header("Optional: Timeline for camera moments")]
    public PlayableDirector director;  // assign if using Timeline

    [Header("Optional: Animator for camera animations")]
    public Animator cameraAnimator;    // assign if using Animator

    // Called by DialogueUI when a narration line has triggerTimeline=true
    public void OnMarker(string marker)
    {
        // Super simple "string switch" approach.
        // You can add more cases as you add more markers.
        switch (marker)
        {
            case "CAM_SHAKE":
                if (cameraAnimator != null) cameraAnimator.SetTrigger("Shake");
                break;

            case "CAM_ZOOM":
                if (cameraAnimator != null) cameraAnimator.SetTrigger("Zoom");
                break;

            case "PLAY_TIMELINE":
                if (director != null) director.Play();
                break;
        }
    }
}