using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TetrisGameManager : MonoBehaviour
{
    [Header("Board")]
    [Min(1)] public int width = 10;
    [Min(1)] public int height = 10;

    [Header("Board Frame Markers (GREEN frame)")]
    public Transform boardBottomLeft;
    public Transform boardTopRight;
    public Transform boardSpace;
    [Min(0f)] public float boardPadding = 0.05f;

    [Header("Hand Frame Markers (BLUE frame)")]
    public Transform handBottomLeft;
    public Transform handTopRight;
    public Transform handSpace;
    [Min(0f)] public float handPadding = 0.05f;

    [Header("Hand Background (optional)")]
    public GameObject handBackgroundPrefab;
    public float handBackgroundLocalZ = 0.02f;

    [Header("Depth")]
    public float gridZ = 0f;

    [Header("Camera")]
    public Camera cam;

    [Header("Prefabs")]
    public GameObject gridCellPrefab;
    public GameObject placedBlockPrefab;
    public GameObject pieceBlockPrefab;

    [Header("Piece Materials (textures/colors)")]
    public Material[] pieceMaterials;

    [Header("Piece Rules")]
    public bool allowRotation = true;

    [Header("Difficulty")]
    [Range(0f, 1f)] public float solvableHandChance = 0.75f;
    public bool scaleDifficultyOverTime = true;
    [Range(0f, 1f)] public float minSolvableChanceLate = 0.35f;

    [Header("Scoring")]
    public int pointsPerBlockPlaced = 8;
    public int pointsPerLineClear = 180;
    public int pointsPerCellCleared = 6;
    public int pointsPerClueCollected = 350;
    public float comboMultiplierStep = 0.65f;

    [Header("Clues")]
    public bool enableClues = true;
    [Range(0f, 1f)] public float clueChancePerPlacedCell = 0.06f;
    public int cluesTargetToWin = 0; // if > 0 => win condition
    public GameObject clueVisualPrefab;
    public Vector3 clueLocalOffset = new Vector3(0f, 0.55f, 0f);
    public Vector3 clueLocalScale = Vector3.one * 0.25f;

    [Header("Clear VFX (optional)")]
    public ParticleSystem clearCellVfxPrefab;
    public GameObject clearRowVfxPrefab; // add VFX_SweepFade on prefab
    public GameObject clearColVfxPrefab; // add VFX_SweepFade on prefab

    [Header("Juice (3,4,5)")]
    public CameraShake cameraShake;       // CameraShake on main camera
    public Canvas uiCanvas;               // your main canvas
    public GameObject clueFlyIconPrefab;  // UI Image prefab with ClueFlyToUI

    [Header("Random Seed (0 = random)")]
    public int seed = 0;

    [Header("UI")]
    public UIScript ui;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupBlockSound;
    public AudioClip placeBlockSound;

    // Public access for PieceView
    public float CellSize => cellSize;
    public float GridZ => gridZ;
    public Camera Camera => cam;

    bool[,] occ;
    GameObject[,] placedVisual;

    readonly List<PieceView> handPieces = new();
    System.Random rng;
    bool ended;

    float cellSize;
    Vector3 boardStartWorld;
    Transform handBackgroundInstance;

    int score;
    int combo;       // 1 = no combo
    int cluesFound;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        rng = (seed == 0) ? new System.Random() : new System.Random(seed);

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        if (!boardSpace && boardBottomLeft) boardSpace = boardBottomLeft.parent;
        if (!handSpace && handBottomLeft) handSpace = handBottomLeft.parent;

        occ = new bool[width, height];
        placedVisual = new GameObject[width, height];

        RecalculateBoardLayout();
        BuildGridVisuals();
        EnsureHandBackground();

        score = 0;
        combo = 1;
        cluesFound = 0;

        ui?.SetScore(score);
        ui?.SetCombo(combo);
        ui?.SetClues(cluesFound, cluesTargetToWin);

        DealNewHandOrGameOver();
    }

    // ---------------- Audio ----------------
    public void PlayPickupSound()
    {
        if (audioSource && pickupBlockSound)
            audioSource.PlayOneShot(pickupBlockSound);
    }

    public void PlayPlaceSound()
    {
        if (audioSource && placeBlockSound)
            audioSource.PlayOneShot(placeBlockSound);
    }

    // ---------------- Layout ----------------
    void RecalculateBoardLayout()
    {
        if (!boardBottomLeft || !boardTopRight || !boardSpace)
        {
            Debug.LogError("Board markers/space missing. Assign BoardBottomLeft, BoardTopRight, and BoardSpace.");
            return;
        }

        Vector3 blL = boardSpace.InverseTransformPoint(boardBottomLeft.position);
        Vector3 trL = boardSpace.InverseTransformPoint(boardTopRight.position);

        float minX = Mathf.Min(blL.x, trL.x) + boardPadding;
        float maxX = Mathf.Max(blL.x, trL.x) - boardPadding;
        float minY = Mathf.Min(blL.y, trL.y) + boardPadding;
        float maxY = Mathf.Max(blL.y, trL.y) - boardPadding;

        float usableW = maxX - minX;
        float usableH = maxY - minY;
        if (usableW <= 0f || usableH <= 0f)
        {
            Debug.LogError("Board usable area invalid. Check markers/padding.");
            return;
        }

        cellSize = Mathf.Min(usableW / width, usableH / height);

        float usedW = cellSize * width;
        float usedH = cellSize * height;

        float offsetX = (usableW - usedW) * 0.5f;
        float offsetY = (usableH - usedH) * 0.5f;

        Vector3 cell00Local = new Vector3(
            minX + offsetX + cellSize * 0.5f,
            minY + offsetY + cellSize * 0.5f,
            0f
        );

        boardStartWorld = boardSpace.TransformPoint(cell00Local);
        boardStartWorld.z = gridZ;
    }

    void EnsureHandBackground()
    {
        if (!handBackgroundPrefab || !handBottomLeft || !handTopRight || !handSpace)
            return;

        if (handBackgroundInstance)
            return;

        var go = Instantiate(handBackgroundPrefab, handSpace);
        go.name = "HandBackground";
        handBackgroundInstance = go.transform;

        Vector3 blL = handSpace.InverseTransformPoint(handBottomLeft.position);
        Vector3 trL = handSpace.InverseTransformPoint(handTopRight.position);

        float minX = Mathf.Min(blL.x, trL.x) + handPadding;
        float maxX = Mathf.Max(blL.x, trL.x) - handPadding;
        float minY = Mathf.Min(blL.y, trL.y) + handPadding;
        float maxY = Mathf.Max(blL.y, trL.y) - handPadding;

        float w = Mathf.Max(0.0001f, maxX - minX);
        float h = Mathf.Max(0.0001f, maxY - minY);

        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, -handBackgroundLocalZ);
        handBackgroundInstance.localPosition = center;
        handBackgroundInstance.localRotation = Quaternion.identity;
        handBackgroundInstance.localScale = new Vector3(w, h, 1f);
    }

    // ---------------- Grid conversions ----------------
    public Vector3 GridToWorld(Vector2Int g)
    {
        return new Vector3(
            boardStartWorld.x + g.x * cellSize,
            boardStartWorld.y + g.y * cellSize,
            gridZ
        );
    }

    public Vector2Int WorldToGrid(Vector3 w)
    {
        float lx = (w.x - boardStartWorld.x) / cellSize;
        float ly = (w.y - boardStartWorld.y) / cellSize;
        return new Vector2Int(Mathf.RoundToInt(lx), Mathf.RoundToInt(ly));
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

    // ---------------- Visuals ----------------
    void BuildGridVisuals()
    {
        if (!gridCellPrefab) return;

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            var go = Instantiate(gridCellPrefab, GridToWorld(new Vector2Int(x, y)), Quaternion.identity, transform);
            go.name = $"Grid_{x}_{y}";
            go.transform.localScale = Vector3.one * cellSize;
        }
    }

    // ---------------- Hand generation ----------------
    void DealNewHandOrGameOver()
    {
        if (ended) return;

        for (int i = 0; i < handPieces.Count; i++)
            if (handPieces[i]) Destroy(handPieces[i].gameObject);
        handPieces.Clear();

        float chance = solvableHandChance;
        if (scaleDifficultyOverTime)
            chance = Mathf.Lerp(solvableHandChance, minSolvableChanceLate, GetFilledRatio());

        bool wantSolvable = rng.NextDouble() < chance;

        SolvableHandGenerator.HandPiece[] hand;
        if (wantSolvable)
        {
            if (!SolvableHandGenerator.TryGenerateHand(
                    width, height,
                    (x, y) => occ[x, y],
                    rng,
                    allowRotation,
                    out hand))
            {
                hand = GenerateRandomHand();
            }
        }
        else
        {
            hand = GenerateRandomHand();
        }

        Vector3[] spawnPoints = GetHandSpawnPointsWorld();
        for (int i = 0; i < 3; i++)
        {
            var root = new GameObject($"HandPiece_{i}");
            root.transform.position = spawnPoints[i];

            var pv = root.AddComponent<PieceView>();
            pv.Init(this, hand[i].shape, pieceBlockPrefab, PickMaterial(), spawnPoints[i]);
            handPieces.Add(pv);
        }

        if (!AnyHandPiecePlaceable())
            GameOver_NoSpace();
    }

    Vector3[] GetHandSpawnPointsWorld()
    {
        Vector3[] result = new Vector3[3];

        if (!handBottomLeft || !handTopRight || !handSpace)
        {
            Debug.LogError("Hand markers/space missing. Assign HandBottomLeft, HandTopRight, and HandSpace.");
            return result;
        }

        Vector3 blL = handSpace.InverseTransformPoint(handBottomLeft.position);
        Vector3 trL = handSpace.InverseTransformPoint(handTopRight.position);

        float minX = Mathf.Min(blL.x, trL.x) + handPadding;
        float maxX = Mathf.Max(blL.x, trL.x) - handPadding;
        float minY = Mathf.Min(blL.y, trL.y) + handPadding;
        float maxY = Mathf.Max(blL.y, trL.y) - handPadding;

        float y = (minY + maxY) * 0.5f;

        Vector3 p0L = new Vector3(Mathf.Lerp(minX, maxX, 0.2f), y, 0f);
        Vector3 p1L = new Vector3(Mathf.Lerp(minX, maxX, 0.5f), y, 0f);
        Vector3 p2L = new Vector3(Mathf.Lerp(minX, maxX, 0.8f), y, 0f);

        result[0] = handSpace.TransformPoint(p0L);
        result[1] = handSpace.TransformPoint(p1L);
        result[2] = handSpace.TransformPoint(p2L);

        result[0].z = gridZ;
        result[1].z = gridZ;
        result[2].z = gridZ;

        return result;
    }

    SolvableHandGenerator.HandPiece[] GenerateRandomHand()
    {
        var h = new SolvableHandGenerator.HandPiece[3];
        for (int i = 0; i < 3; i++)
            h[i] = new SolvableHandGenerator.HandPiece { shape = BlockBlastShapeLibrary.GetRandom(rng, allowRotation), rotation = 0 };
        return h;
    }

    float GetFilledRatio()
    {
        int filled = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            if (occ[x, y]) filled++;

        return (width * height) == 0 ? 0f : (float)filled / (width * height);
    }

    Material PickMaterial()
    {
        if (pieceMaterials == null || pieceMaterials.Length == 0) return null;
        return pieceMaterials[rng.Next(0, pieceMaterials.Length)];
    }

    // ---------------- Placement ----------------
    bool AnyHandPiecePlaceable()
    {
        for (int i = 0; i < handPieces.Count; i++)
        {
            var p = handPieces[i];
            if (!p) continue;
            if (AnyPlacementExists_Bounded(p.Shape))
                return true;
        }
        return false;
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

    public bool TryPlacePieceAtAnchor(PieceView piece, Vector2Int anchor)
    {
        if (ended) return false;
        if (!piece) return false;

        var shape = piece.Shape;
        if (shape == null || shape.Length == 0) return false;
        if (!CanPlace(shape, anchor)) return false;

        // placement drip
        score += pointsPerBlockPlaced * shape.Length;
        ui?.SetScore(score);

        CommitPlacement(shape, anchor, piece.PieceMaterial);
        PlayPlaceSound();

        int clearedLines, clearedCells, cluesCollected;
        ClearLines(out clearedLines, out clearedCells, out cluesCollected);

        if (clearedLines > 0)
        {
            combo = Mathf.Max(2, combo + 1);
            float mult = 1f + (combo - 1) * comboMultiplierStep;

            int clearScore = Mathf.RoundToInt((clearedLines * pointsPerLineClear + clearedCells * pointsPerCellCleared) * mult);
            score += clearScore;

            if (cluesCollected > 0)
                score += cluesCollected * pointsPerClueCollected;

            ui?.SetScore(score);
            ui?.SetCombo(combo);
            ui?.SetClues(cluesFound, cluesTargetToWin);

            if (cameraShake)
            {
                float strengthMul = 1f + 0.35f * (clearedLines - 1) + 0.015f * clearedCells;
                float durationMul = 1f + 0.15f * (clearedLines - 1);
                cameraShake.Kick(strengthMul, durationMul);
            }
        }
        else
        {
            combo = 1;
            ui?.SetCombo(combo);
        }

        handPieces.Remove(piece);
        Destroy(piece.gameObject);

        // ✅ WIN CONDITION
        if (enableClues && cluesTargetToWin > 0 && cluesFound >= cluesTargetToWin)
        {
            Win();
            return true;
        }

        if (handPieces.Count == 0)
            DealNewHandOrGameOver();
        else if (!AnyHandPiecePlaceable())
            GameOver_NoSpace();

        return true;
    }

    void CommitPlacement(Vector2Int[] shape, Vector2Int anchor, Material mat)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            int x = anchor.x + shape[i].x;
            int y = anchor.y + shape[i].y;

            occ[x, y] = true;

            if (!placedBlockPrefab) continue;

            var pos = GridToWorld(new Vector2Int(x, y));
            var go = Instantiate(placedBlockPrefab, pos, Quaternion.identity, transform);
            go.name = $"Placed_{x}_{y}";
            go.transform.localScale = Vector3.one * cellSize;
            placedVisual[x, y] = go;

            ApplyMaterial(go, mat);

            // clue overlay
            var data = go.GetComponent<PlacedCellData>();
            if (!data) data = go.AddComponent<PlacedCellData>();

            if (enableClues && clueVisualPrefab && rng.NextDouble() < clueChancePerPlacedCell)
            {
                data.hasClue = true;
                var clue = Instantiate(clueVisualPrefab, go.transform);
                clue.name = "ClueVisual";
                clue.transform.localPosition = clueLocalOffset * cellSize;
                clue.transform.localRotation = Quaternion.identity;
                clue.transform.localScale = clueLocalScale;
                data.clueVisual = clue.transform;
            }
            else
            {
                data.hasClue = false;
                data.clueVisual = null;
            }
        }
    }

    void ClearLines(out int clearedLines, out int clearedCells, out int cluesCollected)
    {
        clearedLines = 0;
        clearedCells = 0;
        cluesCollected = 0;

        var fullRows = new List<int>(4);
        var fullCols = new List<int>(4);

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

        for (int i = 0; i < fullRows.Count; i++) SpawnRowClearVfx(fullRows[i]);
        for (int i = 0; i < fullCols.Count; i++) SpawnColClearVfx(fullCols[i]);

        for (int i = 0; i < fullRows.Count; i++)
        {
            int y = fullRows[i];
            for (int x = 0; x < width; x++)
                ClearCell(x, y, ref clearedCells, ref cluesCollected);
        }

        for (int i = 0; i < fullCols.Count; i++)
        {
            int x = fullCols[i];
            for (int y = 0; y < height; y++)
                ClearCell(x, y, ref clearedCells, ref cluesCollected);
        }

        clearedLines = fullRows.Count + fullCols.Count;

        if (enableClues && cluesCollected > 0)
        {
            cluesFound += cluesCollected;
        }
    }

    void ClearCell(int x, int y, ref int clearedCells, ref int cluesCollected)
    {
        if (!occ[x, y]) return;

        SpawnCellClearVfx(new Vector2Int(x, y));

        occ[x, y] = false;
        clearedCells++;

        var go = placedVisual[x, y];
        if (go)
        {
            var data = go.GetComponent<PlacedCellData>();
            if (data && data.hasClue)
            {
                cluesCollected++;
                TrySpawnClueFly(GridToWorld(new Vector2Int(x, y)));
            }

            Destroy(go);
            placedVisual[x, y] = null;
        }
    }

    void SpawnCellClearVfx(Vector2Int cell)
    {
        if (!clearCellVfxPrefab) return;

        var pos = GridToWorld(cell);
        var fx = Instantiate(clearCellVfxPrefab, pos, Quaternion.identity);
        fx.transform.localScale = Vector3.one * cellSize;

        var main = fx.main;
        Destroy(fx.gameObject, main.duration + main.startLifetime.constantMax + 0.25f);
    }

    void SpawnRowClearVfx(int y)
    {
        if (!clearRowVfxPrefab) return;

        Vector3 left = GridToWorld(new Vector2Int(0, y));
        Vector3 right = GridToWorld(new Vector2Int(width - 1, y));
        Vector3 center = (left + right) * 0.5f;

        var go = Instantiate(clearRowVfxPrefab, center, Quaternion.identity);
        go.transform.localScale = new Vector3(width * cellSize, cellSize * 0.25f, 1f);
        Destroy(go, 2f);
    }

    void SpawnColClearVfx(int x)
    {
        if (!clearColVfxPrefab) return;

        Vector3 bottom = GridToWorld(new Vector2Int(x, 0));
        Vector3 top = GridToWorld(new Vector2Int(x, height - 1));
        Vector3 center = (bottom + top) * 0.5f;

        var go = Instantiate(clearColVfxPrefab, center, Quaternion.identity);
        go.transform.localScale = new Vector3(cellSize * 0.25f, height * cellSize, 1f);
        Destroy(go, 2f);
    }

    void TrySpawnClueFly(Vector3 worldPos)
    {
        if (!ui || !uiCanvas || !clueFlyIconPrefab) return;
        if (!ui.cluesTargetRect) return;

        var go = Instantiate(clueFlyIconPrefab, uiCanvas.transform);
        var fly = go.GetComponent<ClueFlyToUI>();
        if (!fly)
        {
            Destroy(go);
            return;
        }

        fly.canvas = uiCanvas;
        fly.uiTarget = ui.cluesTargetRect;
        fly.worldCamera = cam ? cam : Camera.main;
        fly.Init(worldPos);
    }

    static void ApplyMaterial(GameObject go, Material mat)
    {
        if (!mat) return;
        var r = go.GetComponentInChildren<Renderer>();
        if (!r) return;
        r.sharedMaterial = mat;
    }

    // ✅ WIN / LOSE
    void Win()
    {
        if (ended) return;
        ended = true;
        if (ui) ui.ShowWin(score);
        else Time.timeScale = 0f;
    }

    void GameOver_NoSpace()
    {
        if (ended) return;
        ended = true;

        if (ui) ui.ShowGameOver(score);
        else Time.timeScale = 0f;
    }
}