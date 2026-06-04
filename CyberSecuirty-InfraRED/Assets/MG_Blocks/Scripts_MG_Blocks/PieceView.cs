using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PieceView : MonoBehaviour
{
    public Vector2Int[] Shape { get; private set; }
    public Material PieceMaterial { get; private set; }
    public bool[] ClueMask { get; private set; } // if you use preview clues

    [Header("X-Ray Overlap Feedback")]
    [Range(0f, 1f)] public float validAlpha = 0.50f;
    [Range(0f, 1f)] public float invalidAlpha = 0.75f;
    public Color invalidTint = new Color(1f, 0.2f, 0.2f, 1f);
    public float hoverZOffset = -0.01f; // slight push so it doesn't z-fight

    TetrisGameManager game;

    readonly List<GameObject> blockGos = new();
    readonly List<Renderer> renderers = new();
    readonly List<Transform> blockClueVisuals = new();

    bool dragging;
    bool hasAnchor;
    Vector2Int currentAnchor;

    Vector3 pivotLocalOffset;
    Vector3 spawnWorld;

    MaterialPropertyBlock mpb;
    int baseColorId;
    int colorId;
    bool xrayApplied;

    public void Init(
        TetrisGameManager game,
        Vector2Int[] shape,
        GameObject blockPrefab,
        Material pieceMaterial,
        Vector3 spawnWorld)
    {
        this.game = game;
        Shape = shape;
        PieceMaterial = pieceMaterial;
        this.spawnWorld = spawnWorld;

        mpb = new MaterialPropertyBlock();
        baseColorId = Shader.PropertyToID("_BaseColor"); // URP
        colorId = Shader.PropertyToID("_Color");         // built-in

        transform.position = spawnWorld;
        BuildVisual(blockPrefab);
    }

    // Optional (only if you're doing preview clues)
    public void SetPreviewClues(bool[] clueMask, GameObject clueVisualPrefab, Vector3 localOffset, Vector3 localScale)
    {
        ClueMask = clueMask;

        for (int i = 0; i < blockClueVisuals.Count; i++)
            if (blockClueVisuals[i]) Destroy(blockClueVisuals[i].gameObject);
        blockClueVisuals.Clear();

        if (ClueMask == null || clueVisualPrefab == null) return;

        for (int i = 0; i < ClueMask.Length && i < blockGos.Count; i++)
        {
            if (!ClueMask[i]) { blockClueVisuals.Add(null); continue; }

            var clue = Instantiate(clueVisualPrefab, blockGos[i].transform, false);
            clue.name = "PreviewClue";
            clue.transform.localPosition = localOffset * game.CellSize;
            clue.transform.localRotation = Quaternion.identity;
            clue.transform.localScale = localScale;

            blockClueVisuals.Add(clue.transform);
        }
    }

    void BuildVisual(GameObject blockPrefab)
    {
        if (!blockPrefab) return;

        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        for (int i = 0; i < Shape.Length; i++)
        {
            minX = Mathf.Min(minX, Shape[i].x);
            minY = Mathf.Min(minY, Shape[i].y);
            maxX = Mathf.Max(maxX, Shape[i].x);
            maxY = Mathf.Max(maxY, Shape[i].y);
        }
        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

        Vector3 local00 = new Vector3((0f - center.x) * game.CellSize, (0f - center.y) * game.CellSize, 0f);
        pivotLocalOffset = -local00;

        blockGos.Clear();
        renderers.Clear();

        for (int i = 0; i < Shape.Length; i++)
        {
            var go = Instantiate(blockPrefab, transform);
            go.name = $"PieceBlock_{i}";

            go.transform.localScale = Vector3.one * game.CellSize;

            Vector3 local = new Vector3(Shape[i].x - center.x, Shape[i].y - center.y, 0f) * game.CellSize;
            go.transform.localPosition = local;

            if (!go.GetComponent<Collider>())
                go.AddComponent<BoxCollider>();

            ApplyMaterial(go, PieceMaterial);

            blockGos.Add(go);

            var r = go.GetComponentInChildren<Renderer>();
            if (r) renderers.Add(r);
        }
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || game == null || game.Camera == null) return;

        if (!dragging && mouse.leftButton.wasPressedThisFrame)
        {
            if (TryPick(game.Camera))
            {
                dragging = true;
                hasAnchor = false;
                game.PlayPickupSound();
            }
        }

        if (!dragging) return;

        if (mouse.leftButton.isPressed)
        {
            if (game.TryGetMouseBoardPoint(out var boardPoint))
            {
                currentAnchor = game.WorldToGrid(boardPoint);
                hasAnchor = true;

                Vector3 snapped = game.GridToWorld(currentAnchor) + pivotLocalOffset;
                snapped.z = game.GridZ + hoverZOffset;
                transform.position = snapped;

                bool canPlace = game.CanPlacePublic(Shape, currentAnchor);
                ApplyXRay(canPlace);
            }
            else
            {
                hasAnchor = false;
                ClearXRay();
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            dragging = false;

            bool placed = hasAnchor && game.TryPlacePieceAtAnchor(this, currentAnchor);

            ClearXRay();

            if (placed) return;

            transform.position = spawnWorld;
        }
    }

    void ApplyXRay(bool canPlace)
    {
        xrayApplied = true;

        float a = canPlace ? validAlpha : invalidAlpha;
        Color tint = canPlace ? Color.white : invalidTint;

        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            var mat = r.sharedMaterial;

            // Get base color from material if possible
            Color baseC = Color.white;
            if (mat)
            {
                if (mat.HasProperty(baseColorId)) baseC = mat.GetColor(baseColorId);
                else if (mat.HasProperty(colorId)) baseC = mat.GetColor(colorId);
            }

            // Multiply tint and set alpha
            Color outC = new Color(baseC.r * tint.r, baseC.g * tint.g, baseC.b * tint.b, a);

            r.GetPropertyBlock(mpb);

            if (mat && mat.HasProperty(baseColorId))
                mpb.SetColor(baseColorId, outC);
            else
                mpb.SetColor(colorId, outC);

            r.SetPropertyBlock(mpb);
        }
    }

    void ClearXRay()
    {
        if (!xrayApplied) return;
        xrayApplied = false;

        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (!r) continue;
            r.GetPropertyBlock(mpb);
            mpb.Clear();
            r.SetPropertyBlock(mpb);
        }
    }

    bool TryPick(Camera cam)
    {
        Ray r = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(r, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        // If we hit any collider on our piece blocks, we pick it.
        for (int i = 0; i < blockGos.Count; i++)
        {
            var col = blockGos[i].GetComponent<Collider>();
            if (col && hit.collider == col) return true;
        }
        return false;
    }

    static void ApplyMaterial(GameObject go, Material mat)
    {
        if (!mat) return;
        var r = go.GetComponentInChildren<Renderer>();
        if (!r) return;
        r.sharedMaterial = mat;
    }
}