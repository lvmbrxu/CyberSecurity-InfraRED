using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PieceView : MonoBehaviour
{
    public Vector2Int[] Shape { get; private set; }
    public Material PieceMaterial { get; private set; }

    [Header("X-Ray While Hovering Board")]
    [Range(0f, 1f)] public float hoverAlpha = 0.50f;
    [Range(0f, 1f)] public float invalidHoverAlpha = 0.75f;
    public Color invalidTint = new Color(1f, 0.2f, 0.2f, 1f); // red-ish
    public Color validTint = Color.white;                      // normal
    public float xrayZOffset = -0.01f;                         // slight offset to reduce z-fighting

    TetrisGameManager game;
    readonly List<Transform> blocks = new();
    readonly List<Renderer> renderers = new();

    bool dragging;
    Vector2Int currentAnchor;
    bool hasAnchor;

    Vector3 pivotLocalOffset;
    Vector3 spawnWorld;

    // MaterialPropertyBlock avoids instantiating materials (fast/no GC)
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
        colorId = Shader.PropertyToID("_Color");         // Built-in

        transform.position = spawnWorld;
        BuildVisual(blockPrefab);
    }

    void BuildVisual(GameObject blockPrefab)
    {
        if (!blockPrefab) return;

        // bounds for centering
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        for (int i = 0; i < Shape.Length; i++)
        {
            minX = Mathf.Min(minX, Shape[i].x);
            minY = Mathf.Min(minY, Shape[i].y);
            maxX = Mathf.Max(maxX, Shape[i].x);
            maxY = Mathf.Max(maxY, Shape[i].y);
        }
        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

        // pivot offset so (0,0) anchor maps correctly during snap
        Vector3 local00 = new Vector3((0f - center.x) * game.CellSize, (0f - center.y) * game.CellSize, 0f);
        pivotLocalOffset = -local00;

        for (int i = 0; i < Shape.Length; i++)
        {
            var go = Object.Instantiate(blockPrefab, transform);
            go.name = $"PieceBlock_{i}";

            go.transform.localScale = Vector3.one * game.CellSize;
            Vector3 local = new Vector3(Shape[i].x - center.x, Shape[i].y - center.y, 0f) * game.CellSize;
            go.transform.localPosition = local;

            if (!go.GetComponent<Collider>())
                go.AddComponent<BoxCollider>();

            ApplyMaterial(go, PieceMaterial);

            blocks.Add(go.transform);

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
                snapped.z = game.GridZ + xrayZOffset; // slight offset so it reads over the board
                transform.position = snapped;

                // Apply X-ray effect while hovering over board
                bool canPlace = game.CanPlacePublic(Shape, currentAnchor);
                ApplyXRay(canPlace);
            }
            else
            {
                // Not on board plane
                hasAnchor = false;
                ClearXRay();
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            dragging = false;

            // Try to place
            bool placed = hasAnchor && game.TryPlacePieceAtAnchor(this, currentAnchor);

            ClearXRay();

            if (placed)
                return;

            transform.position = spawnWorld;
        }
    }

    void ApplyXRay(bool canPlace)
    {
        xrayApplied = true;

        Color tint = canPlace ? validTint : invalidTint;
        float a = canPlace ? hoverAlpha : invalidHoverAlpha;

        for (int i = 0; i < renderers.Count; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            r.GetPropertyBlock(mpb);

            // Preserve original color if the material has one
            Color baseC = Color.white;
            var mat = r.sharedMaterial;
            if (mat)
            {
                if (mat.HasProperty(baseColorId)) baseC = mat.GetColor(baseColorId);
                else if (mat.HasProperty(colorId)) baseC = mat.GetColor(colorId);
            }

            Color outC = new Color(baseC.r * tint.r, baseC.g * tint.g, baseC.b * tint.b, a);

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
            mpb.Clear(); // remove overrides, returns to normal opaque material state
            r.SetPropertyBlock(mpb);
        }
    }

    bool TryPick(Camera cam)
    {
        Ray r = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(r, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        for (int i = 0; i < blocks.Count; i++)
        {
            if (hit.collider.transform == blocks[i] || hit.collider.transform.IsChildOf(blocks[i]))
                return true;
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