// TetrisGameManager.cs

// NEW CHANGES
// NO scene loading. GameOver/Win just shows UI + freezes.
// Restart is removed; use the new SceneLoader button for restart if you want.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TetrisGameManager : MonoBehaviour
{
    [Header("Board")]
    [Min(1)] public int width = 10;
    [Min(1)] public int height = 10;

    [Header("Grid World (cell centers)")]
    public Vector3 gridOrigin = new Vector3(-4.5f, -4.5f, 0f);
    public float cellSize = 1f;
    public float gridZ = 0f;

    [Header("Camera")]
    public Camera cam;

    [Header("Prefabs")]
    public GameObject gridCellPrefab;
    public GameObject placedBlockPrefab;
    public GameObject pieceBlockPrefab;

    [Header("Colors")]
    public Color[] pieceColors;
    public Color clueColor = Color.black;

    [Header("Spawning")]
    public Vector3 pieceSpawnOffset = new Vector3(0f, 3.5f, 0f);
    public bool allowRotation = true;

    [Header("Clues (in pieces)")]
    public int cluesToCollect = 5;
    [Range(0f, 1f)] public float chancePieceContainsClue = 0.45f;
    public int maxClueBlocksPerPiece = 1;
    public int seed = 0;

    [Header("UI")]
    public UIScript ui;

    // Public access used by PieceView
    public float CellSize => cellSize;
    public float GridZ => gridZ;
    public Camera Camera => cam;
    public Vector3 CurrentSpawnWorld => currentSpawnWorld;

    bool[,] occ;
    bool[,] clueOcc;
    GameObject[,] placedVisual;

    PieceView currentPiece;
    Vector2Int[] currentShape;
    bool[] currentShapeIsClue;
    Vector3 currentSpawnWorld;

    System.Random rng;
    int cluesFound;
    bool ended;

    Color currentPieceColor;

    static readonly int ColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorIdAlt = Shader.PropertyToID("_Color");

    void Awake()
    {
        if (!cam) cam = Camera.main;
        rng = (seed == 0) ? new System.Random() : new System.Random(seed);

        occ = new bool[width, height];
        clueOcc = new bool[width, height];
        placedVisual = new GameObject[width, height];

        BuildGridVisuals();
        SpawnNextPieceOrGameOver();
    }

    public Vector3 GridToWorld(Vector2Int g)
        => new Vector3(gridOrigin.x + g.x * cellSize, gridOrigin.y + g.y * cellSize, gridZ);

    public Vector2Int WorldToGrid(Vector3 w)
    {
        float lx = (w.x - gridOrigin.x) / cellSize;
        float ly = (w.y - gridOrigin.y) / cellSize;
        return new Vector2Int(Mathf.FloorToInt(lx + 0.5f), Mathf.FloorToInt(ly + 0.5f));
    }

    bool Inside(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

    public bool TryGetMouseBoardPoint(out Vector3 worldPoint)
    {
        worldPoint = default;

        var mouse = Mouse.current;
        if (!cam || mouse == null) return false;

        Ray r = cam.ScreenPointToRay(mouse.position.ReadValue());
        Plane p = new Plane(Vector3.back, new Vector3(0f, 0f, gridZ));
        if (!p.Raycast(r, out float enter)) return false;

        worldPoint = r.GetPoint(enter);
        worldPoint.z = gridZ;
        return true;
    }

    void BuildGridVisuals()
    {
        if (!gridCellPrefab) return;

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var go = Instantiate(gridCellPrefab, GridToWorld(new Vector2Int(x, y)), Quaternion.identity, transform);
            go.name = $"Grid_{x}_{y}";
        }
    }

    void SpawnNextPieceOrGameOver()
    {
        if (ended) return;

        currentShape = BlockBlastShapeLibrary.GetRandom(rng, allowRotation);
        currentShapeIsClue = BuildClueMaskForShape(currentShape);

        Vector3 topCenter = GridToWorld(new Vector2Int(width / 2, height - 1));
        currentSpawnWorld = topCenter + pieceSpawnOffset;
        currentSpawnWorld.z = gridZ;

        if (!AnyPlacementExists_Bounded(currentShape))
        {
            GameOver_NoSpace();
            return;
        }

        if (currentPiece) Destroy(currentPiece.gameObject);

        currentPieceColor = PickPieceColor();

        var root = new GameObject("CurrentPiece");
        root.transform.position = currentSpawnWorld;
        root.transform.localScale = Vector3.one;

        currentPiece = root.AddComponent<PieceView>();
        currentPiece.Init(this, currentShape, currentShapeIsClue, pieceBlockPrefab, currentPieceColor, clueColor);
    }

    Color PickPieceColor()
    {
        if (pieceColors == null || pieceColors.Length == 0) return Color.white;
        return pieceColors[rng.Next(0, pieceColors.Length)];
    }

    bool[] BuildClueMaskForShape(Vector2Int[] shape)
    {
        var mask = new bool[shape.Length];
        if (rng.NextDouble() > chancePieceContainsClue) return mask;

        int n = Mathf.Clamp(maxClueBlocksPerPiece, 1, shape.Length);
        int count = rng.Next(1, n + 1);

        var indices = new List<int>(shape.Length);
        for (int i = 0; i < shape.Length; i++) indices.Add(i);

        for (int i = 0; i < count; i++)
        {
            int j = rng.Next(i, indices.Count);
            (indices[i], indices[j]) = (indices[j], indices[i]);
            mask[indices[i]] = true;
        }

        return mask;
    }

    bool AnyPlacementExists_Bounded(Vector2Int[] shape)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        for (int i = 0; i < shape.Length; i++)
        {
            var p = shape[i];
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        int startX = -minX;
        int endX = (width - 1) - maxX;
        int startY = -minY;
        int endY = (height - 1) - maxY;

        if (startX > endX || startY > endY) return false;

        for (int y = startY; y <= endY; y++)
        for (int x = startX; x <= endX; x++)
            if (CanPlace(shape, new Vector2Int(x, y)))
                return true;

        return false;
    }

    bool CanPlace(Vector2Int[] shape, Vector2Int anchor)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            int x = anchor.x + shape[i].x;
            int y = anchor.y + shape[i].y;
            if (!Inside(x, y)) return false;
            if (occ[x, y]) return false;
        }
        return true;
    }

    public bool TryPlaceCurrentPieceAtAnchor(Vector2Int anchor)
    {
        if (ended) return false;
        if (!currentPiece) return false;

        if (!CanPlace(currentShape, anchor))
            return false;

        CommitPlacement(currentShape, currentShapeIsClue, anchor);
        ClearLinesAndAwardClues();

        if (!ended && cluesFound >= cluesToCollect)
        {
            ended = true;
            Debug.Log("introduce password");
            if (ui) ui.ShowWin();
            else Time.timeScale = 0f;
            return true;
        }

        SpawnNextPieceOrGameOver();
        return true;
    }

    void CommitPlacement(Vector2Int[] shape, bool[] isClue, Vector2Int anchor)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            int x = anchor.x + shape[i].x;
            int y = anchor.y + shape[i].y;

            occ[x, y] = true;
            clueOcc[x, y] = isClue[i];

            if (placedBlockPrefab)
            {
                var pos = GridToWorld(new Vector2Int(x, y));
                pos.z = gridZ;

                var go = Instantiate(placedBlockPrefab, pos, Quaternion.identity, transform);
                go.name = $"Placed_{x}_{y}";
                placedVisual[x, y] = go;

                ApplyColor(go, isClue[i] ? clueColor : currentPieceColor);
            }
        }
    }

    void ClearLinesAndAwardClues()
    {
        var fullRows = new List<int>();
        var fullCols = new List<int>();

        for (int y = 0; y < height; y++)
        {
            bool full = true;
            for (int x = 0; x < width; x++)
                if (!occ[x, y]) { full = false; break; }
            if (full) fullRows.Add(y);
        }

        for (int x = 0; x < width; x++)
        {
            bool full = true;
            for (int y = 0; y < height; y++)
                if (!occ[x, y]) { full = false; break; }
            if (full) fullCols.Add(x);
        }

        if (fullRows.Count == 0 && fullCols.Count == 0)
            return;

        for (int i = 0; i < fullRows.Count; i++)
        {
            int y = fullRows[i];
            for (int x = 0; x < width; x++)
                ClearCell(x, y);
        }

        for (int i = 0; i < fullCols.Count; i++)
        {
            int x = fullCols[i];
            for (int y = 0; y < height; y++)
                ClearCell(x, y);
        }
    }

    void ClearCell(int x, int y)
    {
        if (!occ[x, y]) return;

        if (clueOcc[x, y])
        {
            clueOcc[x, y] = false;
            cluesFound++;
            Debug.Log(cluesFound.ToString()); // 1..5
        }

        occ[x, y] = false;

        if (placedVisual[x, y])
        {
            Destroy(placedVisual[x, y]);
            placedVisual[x, y] = null;
        }
    }

    void ApplyColor(GameObject go, Color c)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (!r) return;

        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);

        if (r.sharedMaterial && r.sharedMaterial.HasProperty(ColorId))
            mpb.SetColor(ColorId, c);
        else
            mpb.SetColor(ColorIdAlt, c);

        r.SetPropertyBlock(mpb);
    }

    void GameOver_NoSpace()
    {
        if (ended) return;
        ended = true;

        if (ui) ui.ShowGameOver();
        else Time.timeScale = 0f;
    }

    // Call this from your Continue button if you want gameplay to resume
    public void ContinueGameplay()
    {
        ended = false;
        Time.timeScale = 1f;
        // If you want to keep playing after win:
        // SpawnNextPieceOrGameOver();
    }
}