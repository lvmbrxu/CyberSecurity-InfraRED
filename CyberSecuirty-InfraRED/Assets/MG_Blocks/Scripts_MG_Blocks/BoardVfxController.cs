using UnityEngine;

public sealed class BoardVfxController : MonoBehaviour
{
    [Header("Camera")]
    public Camera cam;

    [Header("Depth")]
    public float zOffsetTowardCamera = -0.05f;

    [Header("Sweep")]
    public float sweepLifetime = 0.22f;
    public float sweepThicknessCells = 0.65f;
    public float sweepAlpha = 0.75f;

    [Header("Pop")]
    public float popLifetime = 0.35f;
    public int popBurstCount = 18;
    public float popStartSpeed = 2.0f;
    public float popStartSizeCells = 0.18f;

    Material sweepMat;
    Mesh quadMesh;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        BuildResources();
    }

    void BuildResources()
    {
        quadMesh = new Mesh();
        quadMesh.name = "VFX_Quad";
        quadMesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3( 0.5f, -0.5f, 0),
            new Vector3( 0.5f,  0.5f, 0),
            new Vector3(-0.5f,  0.5f, 0),
        };
        quadMesh.uv = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(1,0),
            new Vector2(1,1),
            new Vector2(0,1),
        };
        quadMesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        quadMesh.RecalculateNormals();

        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (!s) s = Shader.Find("Unlit/Color");

        sweepMat = new Material(s);
        sweepMat.name = "VFX_Sweep_Unlit";

        // set color property for both URP and Built-in
        if (sweepMat.HasProperty("_BaseColor")) sweepMat.SetColor("_BaseColor", Color.white);
        if (sweepMat.HasProperty("_Color")) sweepMat.SetColor("_Color", Color.white);

        // URP transparent flag if available
        if (sweepMat.HasProperty("_Surface"))
            sweepMat.SetFloat("_Surface", 1f);
    }

    Vector3 DepthOffset()
    {
        if (!cam) return new Vector3(0, 0, zOffsetTowardCamera);
        return cam.transform.forward * zOffsetTowardCamera;
    }

    // ---------- API expected by your TetrisGameManager errors ----------

    public void SpawnRowSweep(Vector3 centerWorld, float cellSize, int boardWidthCells)
    {
        float width = boardWidthCells * cellSize;
        float height = cellSize * sweepThicknessCells;
        SpawnSweepQuad(centerWorld, width, height);
    }

    public void SpawnColSweep(Vector3 centerWorld, float cellSize, int boardHeightCells)
    {
        float width = cellSize * sweepThicknessCells;
        float height = boardHeightCells * cellSize;
        SpawnSweepQuad(centerWorld, width, height);
    }

    public void SpawnCellPop(Vector3 cellCenterWorld, float cellSize, int extraBurst = 0, float sizeScale = 1f)
    {
        var go = new GameObject("VFX_CellPop");
        go.transform.position = cellCenterWorld + DepthOffset();

        var ps = go.AddComponent<ParticleSystem>();

        // ✅ Make sure it is not playing while we configure it
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false; // ✅ critical
        main.loop = false;
        main.duration = popLifetime;
        main.startLifetime = popLifetime * 0.8f;
        main.startSpeed = popStartSpeed;
        main.startSize = cellSize * popStartSizeCells * sizeScale;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 512;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Clamp(popBurstCount + extraBurst, 1, 200))
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = cellSize * 0.10f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        var rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.material = sweepMat;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        
        ps.Play();

        Destroy(go, popLifetime + 0.25f);
    }

    // ---------- Internals ----------

    void SpawnSweepQuad(Vector3 centerWorld, float widthWorld, float heightWorld)
    {
        var go = new GameObject("VFX_Sweep");
        go.transform.position = centerWorld + DepthOffset();

        if (cam)
            go.transform.rotation = cam.transform.rotation;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = quadMesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = sweepMat;

        go.transform.localScale = new Vector3(widthWorld, heightWorld, 1f);

        var fade = go.AddComponent<VfxFadeOut>();
        fade.Init(sweepMat, sweepLifetime, sweepAlpha);

        Destroy(go, sweepLifetime + 0.1f);
    }
}