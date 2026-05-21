using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a 3-piece hand that is guaranteed to be placeable in some order
/// on the current board occupancy.
/// </summary>
public static class SolvableHandGenerator
{
    private const int MaxAttempts = 250;

    public struct HandPiece
    {
        public Vector2Int[] shape;
        public int rotation; // debug only
    }

    public static bool TryGenerateHand(
        int boardW, int boardH,
        Func<int, int, bool> isOccupied,     // (x,y) => true if filled
        System.Random rng,
        bool allowRotation,
        out HandPiece[] hand)
    {
        hand = null;

        // Snapshot occupancy into mutable grid
        bool[,] occ = new bool[boardW, boardH];
        int empty = 0;
        for (int y = 0; y < boardH; y++)
        for (int x = 0; x < boardW; x++)
        {
            occ[x, y] = isOccupied(x, y);
            if (!occ[x, y]) empty++;
        }

        if (empty == 0) return false;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var cand = new HandPiece[3];

            for (int i = 0; i < 3; i++)
            {
                int rot = allowRotation ? rng.Next(0, 4) : 0;
                var baseShape = BlockBlastShapeLibrary.Shapes[rng.Next(0, BlockBlastShapeLibrary.Shapes.Length)];
                var shape = allowRotation ? RotateAndNormalize(baseShape, rot) : baseShape;

                cand[i] = new HandPiece { shape = shape, rotation = rot };
            }

            if (ExistsAnyOrderPlacement((bool[,])occ.Clone(), cand))
            {
                hand = cand;
                return true;
            }
        }

        return false;
    }

    private static Vector2Int[] RotateAndNormalize(Vector2Int[] baseShape, int rot)
    {
        var dst = new Vector2Int[baseShape.Length];
        for (int i = 0; i < baseShape.Length; i++)
        {
            var p = baseShape[i];
            for (int r = 0; r < (rot & 3); r++)
                p = new Vector2Int(p.y, -p.x); // 90 CW
            dst[i] = p;
        }

        int minX = int.MaxValue, minY = int.MaxValue;
        for (int i = 0; i < dst.Length; i++)
        {
            minX = Mathf.Min(minX, dst[i].x);
            minY = Mathf.Min(minY, dst[i].y);
        }

        var norm = new Vector2Int[dst.Length];
        for (int i = 0; i < dst.Length; i++)
            norm[i] = new Vector2Int(dst[i].x - minX, dst[i].y - minY);

        return norm;
    }

    private static bool ExistsAnyOrderPlacement(bool[,] occOriginal, HandPiece[] hand)
    {
        int[] idx = { 0, 1, 2 };
        foreach (var perm in Perm3(idx))
        {
            if (CanPlaceAll((bool[,])occOriginal.Clone(),
                    hand[perm[0]].shape,
                    hand[perm[1]].shape,
                    hand[perm[2]].shape))
                return true;
        }
        return false;
    }

    private static IEnumerable<int[]> Perm3(int[] a)
    {
        yield return new[] { a[0], a[1], a[2] };
        yield return new[] { a[0], a[2], a[1] };
        yield return new[] { a[1], a[0], a[2] };
        yield return new[] { a[1], a[2], a[0] };
        yield return new[] { a[2], a[0], a[1] };
        yield return new[] { a[2], a[1], a[0] };
    }

    private static bool CanPlaceAll(bool[,] occ, Vector2Int[] a, Vector2Int[] b, Vector2Int[] c)
        => TryPlaceOne(occ, a) && TryPlaceOne(occ, b) && TryPlaceOne(occ, c);

    private static bool TryPlaceOne(bool[,] occ, Vector2Int[] shape)
    {
        int w = occ.GetLength(0);
        int h = occ.GetLength(1);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (CanPlaceAt(occ, x, y, shape))
            {
                ApplyAt(occ, x, y, shape, true);
                return true;
            }
        }

        return false;
    }

    private static bool CanPlaceAt(bool[,] occ, int ax, int ay, Vector2Int[] shape)
    {
        int w = occ.GetLength(0);
        int h = occ.GetLength(1);

        for (int i = 0; i < shape.Length; i++)
        {
            int x = ax + shape[i].x;
            int y = ay + shape[i].y;
            if (x < 0 || x >= w || y < 0 || y >= h) return false;
            if (occ[x, y]) return false;
        }

        return true;
    }

    private static void ApplyAt(bool[,] occ, int ax, int ay, Vector2Int[] shape, bool v)
    {
        for (int i = 0; i < shape.Length; i++)
            occ[ax + shape[i].x, ay + shape[i].y] = v;
    }
}