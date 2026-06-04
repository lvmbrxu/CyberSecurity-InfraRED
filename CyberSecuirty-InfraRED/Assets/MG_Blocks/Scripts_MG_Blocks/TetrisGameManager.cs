using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TetrisGameManager : MonoBehaviour
{
    [Header("Board Size")]
    [Min(1)] public int width = 8;
    [Min(1)] public int height = 8;

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

    [Header("Rules")]
    public bool allowRotation = false;

    [Header("Difficulty")]
    [Range(0f, 1f)] public float solvableHandChance = 0.72f;
    public bool scaleDifficultyOverTime = true;
    [Range(0f, 1f)] public float minSolvableChanceLate = 0.24f;

    [Header("Scoring")]
    public int pointsPerBlockPlaced = 6;
    public int pointsPerLineClear = 240;
    public int pointsPerCellCleared = 8;
    public int pointsPerClueCollected = 450;
    public float comboMultiplierStep = 0.70f;

    [Header("Score Fly Text (score updates only on arrival)")]
    public GameObject scoreFlyTextPrefab;
    public RectTransform scoreTargetRect;
    public Vector2Int scoreFlyFromCell = new Vector2Int(-1, -1);

    [Header("Feedback Text")]
    public GameObject feedbackTextPrefab;
    public RectTransform feedbackSpawnRect;

    [Header("Clues (Preview + Board)")]
    public bool enableClues = true;

    [Tooltip("Chance that a dealt piece will contain a clue.")]
    [Range(0f, 1f)] public float previewPieceHasClueChance = 0.35f;

    [Tooltip("Max number of clue blocks on a single preview piece.")]
    [Range(1, 3)] public int maxCluesPerPiece = 1;

    public int cluesTargetToWin = 6;

    [Tooltip("World fingerprint prefab (SpriteRenderer). Used both on preview blocks and placed blocks.")]
    public GameObject clueVisualPrefab;

    public Vector3 clueLocalOffset = new Vector3(0f, 0.55f, -0.02f);
    public Vector3 clueLocalScale = Vector3.one * 0.25f;

    [Header("VFX (Procedural)")]
    public BoardVfxController boardVfx;
    public int comboBigPopExtraBurst = 30;
    public float comboBigPopScale = 1.8f;

    [Header("Juice")]
    public CameraShake cameraShake;
    public Canvas uiCanvas;
    public GameObject clueFlyIconPrefab;

    [Header("Random Seed (0 = random)")]
    public int seed = 0;

    [Header("UI")]
    public UIScript ui;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupBlockSound;
    public AudioClip placeBlockSound;

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
    int pendingScore;
    int combo;
    int cluesFound;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        rng = (seed == 0) ? new System.Random() : new System.Random(seed);

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        if (!boardSpace && boardBottomLeft) boardSpace = boardBottomLeft.parent;
        if (!handSpace && handBottomLeft) handSpace = handBottomLeft.parent;

        if (boardVfx && !boardVfx.cam) boardVfx.cam = cam;

        occ = new bool[width, height];
        placedVisual = new GameObject[width, height];

        RecalculateBoardLayout();
        BuildGridVisuals();
        EnsureHandBackground();

        score = 0;
        pendingScore = 0;
        combo = 1;
        cluesFound = 0;

        ui?.SetScore(score);
        ui?.SetCombo(combo);
        ui?.SetClues(cluesFound, cluesTargetToWin);

        DealNewHandOrGameOver();
    }

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

    void RecalculateBoardLayout()
    {
        if (!boardBottomLeft || !boardTopRight || !boardSpace)
        {
            Debug.LogError("Board markers/space missing.");
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
            Debug.LogError("Board usable area invalid.");
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
        if (!handBackgroundPrefab || !handBottomLeft || !handTopRight || !handSpace) return;
        if (handBackgroundInstance) return;

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

    public Vector3 GridToWorld(Vector2Int g)
        => new Vector3(boardStartWorld.x + g.x * cellSize, boardStartWorld.y + g.y * cellSize, gridZ);

    public Vector2Int WorldToGrid(Vector3 w)
    {
        float lx = (w.x - boardStartWorld.x) / cellSize;
        float ly = (w.y - boardStartWorld.y) / cellSize;
        return new Vector2Int(Mathf.RoundToInt(lx), Mathf.RoundToInt(ly));
    }

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
            go.transform.localScale = Vector3.one * cellSize;
        }
    }

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
                hand = GenerateRandomHand();
        }
        else hand = GenerateRandomHand();

        Vector3[] spawnPoints = GetHandSpawnPointsWorld();
        for (int i = 0; i < 3; i++)
        {
            var root = new GameObject($"HandPiece_{i}");
            root.transform.position = spawnPoints[i];

            var pv = root.AddComponent<PieceView>();
            pv.Init(this, hand[i].shape, pieceBlockPrefab, PickMaterial(), spawnPoints[i]);

            // ✅ Generate preview clues and show them on the tray piece
            if (enableClues)
            {
                bool[] mask = GeneratePreviewClueMask(hand[i].shape.Length);
                pv.SetPreviewClues(mask, clueVisualPrefab, clueLocalOffset, clueLocalScale);
            }
            else
            {
                pv.SetPreviewClues(null, null, Vector3.zero, Vector3.one);
            }

            handPieces.Add(pv);
        }

        if (!AnyHandPiecePlaceable())
            GameOver_NoSpace();
    }

    bool[] GeneratePreviewClueMask(int len)
    {
        var mask = new bool[len];

        // Decide if this piece has any clue at all
        if (rng.NextDouble() > previewPieceHasClueChance)
            return mask;

        int clues = Mathf.Clamp(maxCluesPerPiece, 1, 3);
        clues = Mathf.Min(clues, len);

        // Pick random unique indices
        for (int k = 0; k < clues; k++)
        {
            int tries = 0;
            while (tries++ < 20)
            {
                int idx = rng.Next(0, len);
                if (!mask[idx]) { mask[idx] = true; break; }
            }
        }

        return mask;
    }

    Vector3[] GetHandSpawnPointsWorld()
    {
        Vector3[] result = new Vector3[3];

        if (!handBottomLeft || !handTopRight || !handSpace)
        {
            Debug.LogError("Hand markers/space missing.");
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
        {
            var shape = BlockBlastShapeLibrary.GetRandom(rng, allowRotation);
            h[i] = new SolvableHandGenerator.HandPiece { shape = shape, rotation = 0 };
        }
        return h;
    }

    float GetFilledRatio()
    {
        int filled = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            if (occ[x, y]) filled++;
        int total = width * height;
        return total == 0 ? 0f : (float)filled / total;
    }

    Material PickMaterial()
    {
        if (pieceMaterials == null || pieceMaterials.Length == 0) return null;
        return pieceMaterials[rng.Next(0, pieceMaterials.Length)];
    }

    public bool CanPlacePublic(Vector2Int[] shape, Vector2Int anchor)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            int x = anchor.x + shape[i].x;
            int y = anchor.y + shape[i].y;
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            if (occ[x, y]) return false;
        }
        return true;
    }

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
            if (CanPlacePublic(shape, new Vector2Int(x, y)))
                return true;

        return false;
    }

    Vector3 ScoreFlyOrigin()
    {
        if (scoreFlyFromCell.x >= 0 && scoreFlyFromCell.y >= 0 &&
            scoreFlyFromCell.x < width && scoreFlyFromCell.y < height)
            return GridToWorld(scoreFlyFromCell);

        return GridToWorld(new Vector2Int(width / 2, height / 2));
    }

    Vector3 ComputeShapeWorldCenter(Vector2Int anchor, Vector2Int[] shape)
    {
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < shape.Length; i++)
            sum += GridToWorld(new Vector2Int(anchor.x + shape[i].x, anchor.y + shape[i].y));
        return sum / Mathf.Max(1, shape.Length);
    }

    public bool TryPlacePieceAtAnchor(PieceView piece, Vector2Int anchor)
    {
        if (ended) return false;
        if (!piece) return false;

        var shape = piece.Shape;
        if (shape == null || shape.Length == 0) return false;
        if (!CanPlacePublic(shape, anchor)) return false;

        int placementGain = pointsPerBlockPlaced * shape.Length;
        SpawnScoreFly(ComputeShapeWorldCenter(anchor, shape), placementGain);

        CommitPlacement(shape, anchor, piece.PieceMaterial, piece.ClueMask);
        PlayPlaceSound();

        int clearedLines, clearedCells;
        int baseClearGain = ClearLines(out clearedLines, out clearedCells);

        if (clearedLines > 0)
        {
            combo = Mathf.Max(2, combo + 1);
            ui?.SetCombo(combo);

            float mult = 1f + (combo - 1) * comboMultiplierStep;
            int finalClearGain = Mathf.RoundToInt(baseClearGain * mult);

            SpawnScoreFly(ScoreFlyOrigin(), finalClearGain);

            if (boardVfx && clearedLines >= 2)
            {
                var center = GridToWorld(new Vector2Int(width / 2, height / 2));
                boardVfx.SpawnCellPop(center, cellSize, extraBurst: 30, sizeScale: 1.8f);
            }

            SpawnFeedbackForClear(clearedLines, combo);

            if (cameraShake)
            {
                float strengthMul = 1f + 0.35f * (clearedLines - 1) + 0.02f * clearedCells;
                float durationMul = 1f + 0.15f * (clearedLines - 1);
                cameraShake.Kick(strengthMul, durationMul);
            }
        }
        else
        {
            combo = 1;
            ui?.SetCombo(combo);
            SpawnFeedback("Nice!");
        }

        handPieces.Remove(piece);
        Destroy(piece.gameObject);

        if (handPieces.Count == 0)
            DealNewHandOrGameOver();
        else if (!AnyHandPiecePlaceable())
            GameOver_NoSpace();

        return true;
    }

    void SpawnFeedbackForClear(int clearedLines, int comboNow)
    {
        if (clearedLines >= 3) { SpawnFeedback("Amazing!"); return; }
        if (clearedLines == 2) { SpawnFeedback("Good job!"); return; }
        if (comboNow >= 3) SpawnFeedback($"Combo x{comboNow}!");
        else SpawnFeedback("Great!");
    }

    void SpawnFeedback(string msg)
    {
        if (!uiCanvas || !feedbackTextPrefab) return;

        var go = Instantiate(feedbackTextPrefab, uiCanvas.transform);
        var fly = go.GetComponent<FeedbackTextFly>();
        if (!fly) { Destroy(go); return; }

        var rt = go.GetComponent<RectTransform>();
        if (feedbackSpawnRect) rt.position = feedbackSpawnRect.position;
        else rt.anchoredPosition = Vector2.zero;

        fly.SetText(msg);
    }

    void SpawnScoreFly(Vector3 worldPos, int amount)
    {
        if (!uiCanvas || !scoreFlyTextPrefab || !scoreTargetRect) return;

        var go = Instantiate(scoreFlyTextPrefab, uiCanvas.transform);
        var fly = go.GetComponent<ScoreFlyToUI>();
        if (!fly) { Destroy(go); return; }

        pendingScore += amount;

        fly.canvas = uiCanvas;
        fly.uiTarget = scoreTargetRect;
        fly.worldCamera = cam ? cam : Camera.main;

        fly.OnArrive = () =>
        {
            pendingScore -= amount;
            score += amount;
            ui?.SetScore(score);
        };

        fly.Init(worldPos, $"+{amount}");
    }

    void CommitPlacement(Vector2Int[] shape, Vector2Int anchor, Material mat, bool[] clueMask)
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

            var data = go.GetComponent<PlacedCellData>();
            if (!data) data = go.AddComponent<PlacedCellData>();

            bool hasClueHere = enableClues && clueMask != null && i < clueMask.Length && clueMask[i];

            if (hasClueHere && clueVisualPrefab)
            {
                data.hasClue = true;

                var clueGO = Instantiate(clueVisualPrefab, go.transform, false);
                clueGO.name = "ClueVisual";
                clueGO.transform.localPosition = clueLocalOffset * cellSize;
                clueGO.transform.localRotation = Quaternion.identity;
                clueGO.transform.localScale = clueLocalScale;

                data.clueVisual = clueGO.transform;
            }
            else
            {
                data.hasClue = false;
                data.clueVisual = null;
            }
        }
    }

    int ClearLines(out int clearedLines, out int clearedCells)
    {
        clearedLines = 0;
        clearedCells = 0;

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
            return 0;

        if (boardVfx)
        {
            for (int i = 0; i < fullRows.Count; i++)
            {
                int y = fullRows[i];
                Vector3 left = GridToWorld(new Vector2Int(0, y));
                Vector3 right = GridToWorld(new Vector2Int(width - 1, y));
                Vector3 center = (left + right) * 0.5f;
                boardVfx.SpawnRowSweep(center, cellSize, width);
            }

            for (int i = 0; i < fullCols.Count; i++)
            {
                int x = fullCols[i];
                Vector3 bottom = GridToWorld(new Vector2Int(x, 0));
                Vector3 top = GridToWorld(new Vector2Int(x, height - 1));
                Vector3 center = (bottom + top) * 0.5f;
                boardVfx.SpawnColSweep(center, cellSize, height);
            }
        }

        for (int i = 0; i < fullRows.Count; i++)
        {
            int y = fullRows[i];
            for (int x = 0; x < width; x++)
                ClearCell(x, y, ref clearedCells);
        }

        for (int i = 0; i < fullCols.Count; i++)
        {
            int x = fullCols[i];
            for (int y = 0; y < height; y++)
                ClearCell(x, y, ref clearedCells);
        }

        clearedLines = fullRows.Count + fullCols.Count;
        return clearedLines * pointsPerLineClear + clearedCells * pointsPerCellCleared;
    }

    void ClearCell(int x, int y, ref int clearedCells)
    {
        if (!occ[x, y]) return;

        if (boardVfx)
        {
            var pos = GridToWorld(new Vector2Int(x, y));
            boardVfx.SpawnCellPop(pos, cellSize);
        }

        occ[x, y] = false;
        clearedCells++;

        var go = placedVisual[x, y];
        if (go)
        {
            var data = go.GetComponent<PlacedCellData>();
            if (enableClues && data && data.hasClue)
                TrySpawnClueFly(GridToWorld(new Vector2Int(x, y)));

            Destroy(go);
            placedVisual[x, y] = null;
        }
    }

    void TrySpawnClueFly(Vector3 worldPos)
    {
        if (!ui || !uiCanvas || !clueFlyIconPrefab) return;
        if (!ui.cluesTargetRect) return;

        var go = Instantiate(clueFlyIconPrefab, uiCanvas.transform);
        var fly = go.GetComponent<ClueFlyToUI>();
        if (!fly) { Destroy(go); return; }

        fly.canvas = uiCanvas;
        fly.uiTarget = ui.cluesTargetRect;
        fly.worldCamera = cam ? cam : Camera.main;
        fly.OnArrive = OnClueArrived;
        fly.Init(worldPos);
    }

    void OnClueArrived()
    {
        if (!enableClues) return;

        cluesFound++;
        ui?.SetClues(cluesFound, cluesTargetToWin);

        SpawnScoreFly(ScoreFlyOrigin(), pointsPerClueCollected);

        if (cluesTargetToWin > 0 && cluesFound >= cluesTargetToWin)
            Win();
    }

    static void ApplyMaterial(GameObject go, Material mat)
    {
        if (!mat) return;
        var r = go.GetComponentInChildren<Renderer>();
        if (!r) return;
        r.sharedMaterial = mat;
    }

    void Win()
    {
        if (ended) return;
        ended = true;

        int final = score + pendingScore;
        if (ui) ui.ShowWin(final);
        else Time.timeScale = 0f;
    }

    void GameOver_NoSpace()
    {
        if (ended) return;
        ended = true;

        int final = score + pendingScore;
        if (ui) ui.ShowGameOver(final);
        else Time.timeScale = 0f;
    }
}