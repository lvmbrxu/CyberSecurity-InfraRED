using UnityEngine;

public static class VfxSpawner
{
    public static void SpawnClearPop(ParticleSystem prefab, Vector3 pos, float sizeScale = 1f, int extraBurst = 0)
    {
        if (!prefab) return;

        var ps = Object.Instantiate(prefab, pos, Quaternion.identity);
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Scale the whole effect
        ps.transform.localScale = Vector3.one * sizeScale;

        // Optional: scale burst count
        if (extraBurst > 0)
        {
            var emission = ps.emission;
            if (emission.burstCount > 0)
            {
                var burst = emission.GetBurst(0);
                burst.count = new ParticleSystem.MinMaxCurve(burst.count.constant + extraBurst);
                emission.SetBurst(0, burst);
            }
        }

        ps.Play();

        float killTime = main.duration + main.startLifetime.constantMax + 0.2f;
        Object.Destroy(ps.gameObject, killTime);
    }
}