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
    [Min(0f)] public float boardPadding = 0.05f;

    [Header("Hand Frame Markers (BLUE frame)")]
    public Transform handBottomLeft;
    public Transform handTopRight;
    [Min(0f)] public float handPadding = 0.05f;

    [Header("Depth")]
    public float gridZ = 0f;
    public float handZ = 0f;

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

    [Header("Random Seed (0 = random)")]
    public int seed = 0;

    [Header("UI")]
    public UIScript ui;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickupBlockSound;
    public AudioClip placeBlockSound;

    // Public access used by PieceView
    public float CellSize => cellSize;
    public float GridZ => gridZ;
    public Camera Camera => cam;

    bool[,] occ;
    GameObject[,] placedVisual;

    readonly List<PieceView> handPieces = new();
    System.Random rng;
    bool ended;

    // Computed from markers
    float cellSize;
    Vector3 boardStartCenter;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        rng = (seed == 0) ? new System.Random() : new System.Random(seed);

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        occ = new bool[width, height];
        placedVisual = new GameObject[width, height];

        RecalculateBoardLayout();
        BuildGridVisuals();
        DealNewHandOrGameOver();
    }

    void RecalculateBoardLayout()
    {
        if (!boardBottomLeft || !boardTopRight)
        {
            Debug.LogError("Board markers are missing. Assign BoardBottomLeft and BoardTopRight.");
            return;
        }

        Vector3 bl = boardBottomLeft.position;
        Vector3 tr = boardTopRight.position;

        float usableWidth = Mathf.Abs(tr.x - bl.x) - boardPadding * 2f;
        float usableHeight = Mathf.Abs(tr.y - bl.y) - boardPadding * 2f;

        if (usableWidth <= 0f || usableHeight <= 0f)
        {
            Debug.LogError("Board usable area is invalid. Check board markers/padding.");
            return;
        }

        // Keep cells square by using the limiting dimension
        float cellFromWidth = usableWidth / width;
        float cellFromHeight = usableHeight / height;
        cellSize = Mathf.Min(cellFromWidth, cellFromHeight);

        float usedWidth = cellSize * width;
        float usedHeight = cellSize * height;

        float extraX = usableWidth - usedWidth;
        float extraY = usableHeight - usedHeight;

        float minX = Mathf.Min(bl.x, tr.x) + boardPadding + extraX * 0.5f;
        float minY = Mathf.Min(bl.y, tr.y) + boardPadding + extraY * 0.5f;

        // center of board cell (0,0)
        boardStartCenter = new Vector3(
            minX + cellSize * 0.5f,
            minY + cellSize * 0.5f,
            gridZ
        );
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

    public Vector3 GridToWorld(Vector2Int g)
    {
        return new Vector3(
            boardStartCenter.x + g.x * cellSize,
            boardStartCenter.y + g.y * cellSize,
            gridZ
        );
    }

    public Vector2Int WorldToGrid(Vector3 w)
    {
        float lx = (w.x - boardStartCenter.x) / cellSize;
        float ly = (w.y - boardStartCenter.y) / cellSize;
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

    void BuildGridVisuals()
    {
        if (!gridCellPrefab) return;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var go = Instantiate(gridCellPrefab, GridToWorld(new Vector2Int(x, y)), Quaternion.identity, transform);
                go.name = $"Grid_{x}_{y}";
                go.transform.localScale = Vector3.one * cellSize;
            }
        }
    }

    void DealNewHandOrGameOver()
    {
        if (ended) return;

        for (int i = 0; i < handPieces.Count; i++)
        {
            if (handPieces[i]) Destroy(handPieces[i].gameObject);
        }
        handPieces.Clear();

        float chance = solvableHandChance;
        if (scaleDifficultyOverTime)
        {
            float filled01 = GetFilledRatio();
            chance = Mathf.Lerp(solvableHandChance, minSolvableChanceLate, filled01);
        }

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

        Vector3[] spawnPoints = GetHandSpawnPoints();

        for (int i = 0; i < 3; i++)
        {
            var root = new GameObject($"HandPiece_{i}");
            root.transform.position = spawnPoints[i];

            var pv = root.AddComponent<PieceView>();
            Material mat = PickMaterial();
            pv.Init(this, hand[i].shape, pieceBlockPrefab, mat, spawnPoints[i]);

            handPieces.Add(pv);
        }

        if (!AnyHandPiecePlaceable())
            GameOver_NoSpace();
    }

    Vector3[] GetHandSpawnPoints()
    {
        Vector3[] result = new Vector3[3];

        if (!handBottomLeft || !handTopRight)
        {
            Debug.LogError("Hand markers are missing. Assign HandBottomLeft and HandTopRight.");
            return result;
        }

        Vector3 bl = handBottomLeft.position;
        Vector3 tr = handTopRight.position;

        float minX = Mathf.Min(bl.x, tr.x) + handPadding;
        float maxX = Mathf.Max(bl.x, tr.x) - handPadding;
        float minY = Mathf.Min(bl.y, tr.y) + handPadding;
        float maxY = Mathf.Max(bl.y, tr.y) - handPadding;

        float y = (minY + maxY) * 0.5f;

        result[0] = new Vector3(Mathf.Lerp(minX, maxX, 0.2f), y, handZ);
        result[1] = new Vector3(Mathf.Lerp(minX, maxX, 0.5f), y, handZ);
        result[2] = new Vector3(Mathf.Lerp(minX, maxX, 0.8f), y, handZ);

        return result;
    }

    SolvableHandGenerator.HandPiece[] GenerateRandomHand()
    {
        var h = new SolvableHandGenerator.HandPiece[3];
        for (int i = 0; i < 3; i++)
        {
            var shape = BlockBlastShapeLibrary.GetRandom(rng, allowRotation);
            h[i] = new SolvableHandGenerator.HandPiece
            {
                shape = shape,
                rotation = 0
            };
        }
        return h;
    }

    float GetFilledRatio()
    {
        int filled = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (occ[x, y]) filled++;
            }
        }

        int total = width * height;
        return total == 0 ? 0f : (float)filled / total;
    }

    Material PickMaterial()
    {
        if (pieceMaterials == null || pieceMaterials.Length == 0) return null;
        return pieceMaterials[rng.Next(0, pieceMaterials.Length)];
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
        {
            for (int x = startX; x <= endX; x++)
            {
                if (CanPlace(shape, new Vector2Int(x, y)))
                    return true;
            }
        }

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

        CommitPlacement(shape, anchor, piece.PieceMaterial);
        PlayPlaceSound();

        ClearLines();

        handPieces.Remove(piece);
        Destroy(piece.gameObject);

        if (handPieces.Count == 0)
        {
            DealNewHandOrGameOver();
        }
        else
        {
            if (!AnyHandPiecePlaceable())
                GameOver_NoSpace();
        }

        return true;
    }

    void CommitPlacement(Vector2Int[] shape, Vector2Int anchor, Material mat)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            int x = anchor.x + shape[i].x;
            int y = anchor.y + shape[i].y;

            occ[x, y] = true;

            if (placedBlockPrefab)
            {
                var pos = GridToWorld(new Vector2Int(x, y));
                pos.z = gridZ;

                var go = Instantiate(placedBlockPrefab, pos, Quaternion.identity, transform);
                go.name = $"Placed_{x}_{y}";
                go.transform.localScale = Vector3.one * cellSize;
                placedVisual[x, y] = go;

                ApplyMaterial(go, mat);
            }
        }
    }

    void ClearLines()
    {
        var fullRows = new List<int>();
        var fullCols = new List<int>();

        for (int y = 0; y < height; y++)
        {
            bool full = true;
            for (int x = 0; x < width; x++)
            {
                if (!occ[x, y])
                {
                    full = false;
                    break;
                }
            }
            if (full) fullRows.Add(y);
        }

        for (int x = 0; x < width; x++)
        {
            bool full = true;
            for (int y = 0; y < height; y++)
            {
                if (!occ[x, y])
                {
                    full = false;
                    break;
                }
            }
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

        occ[x, y] = false;

        if (placedVisual[x, y])
        {
            Destroy(placedVisual[x, y]);
            placedVisual[x, y] = null;
        }
    }

    static void ApplyMaterial(GameObject go, Material mat)
    {
        if (!mat) return;

        var r = go.GetComponentInChildren<Renderer>();
        if (!r) return;

        r.sharedMaterial = mat;
    }

    void GameOver_NoSpace()
    {
        if (ended) return;
        ended = true;

        if (ui) ui.ShowGameOver();
        else Time.timeScale = 0f;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (boardBottomLeft && boardTopRight)
        {
            Gizmos.color = Color.green;
            DrawRect(boardBottomLeft.position, boardTopRight.position, gridZ);
        }

        if (handBottomLeft && handTopRight)
        {
            Gizmos.color = Color.cyan;
            DrawRect(handBottomLeft.position, handTopRight.position, handZ);
        }
    }

    void DrawRect(Vector3 a, Vector3 b, float z)
    {
        Vector3 bl = new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), z);
        Vector3 br = new Vector3(Mathf.Max(a.x, b.x), Mathf.Min(a.y, b.y), z);
        Vector3 tr = new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), z);
        Vector3 tl = new Vector3(Mathf.Min(a.x, b.x), Mathf.Max(a.y, b.y), z);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
#endif
}