using UnityEngine;

public class DoodlePlatformMaterialApplier : MonoBehaviour
{
    public GameStateSO gameState;

    [Header("Where all platforms live")]
    public Transform platformsRoot;

    [Header("Materials")]
    public Material normalMaterial;
    public Material glitchedMaterial;

    private void Start()
    {
        Apply();
    }

    public void Apply()
    {
        if (platformsRoot == null) return;

        var renderers = platformsRoot.GetComponentsInChildren<Renderer>(true);
        var mat = (gameState.platformGlitchMode == PlatformGlitchMode.Glitched)
            ? glitchedMaterial
            : normalMaterial;

        foreach (var r in renderers)
            r.sharedMaterial = mat; 
    }
}