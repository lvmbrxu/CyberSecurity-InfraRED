// PieceView.cs
// Renders ONE draggable piece (world space).
// Snaps to integer anchor while dragging, drops by integer anchor.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PieceView : MonoBehaviour
{
    public Vector2Int[] Shape { get; private set; }
    public Material PieceMaterial { get; private set; }

    TetrisGameManager game;
    readonly List<Transform> blocks = new();

    bool dragging;
    Vector2Int currentAnchor;
    bool hasAnchor;

    Vector3 pivotLocalOffset;
    Vector3 spawnWorld;

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
                snapped.z = game.GridZ;
                transform.position = snapped;
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            dragging = false;

            if (hasAnchor && game.TryPlacePieceAtAnchor(this, currentAnchor))
                return;

            // failed -> return
            transform.position = spawnWorld;
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