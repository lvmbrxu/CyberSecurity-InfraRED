using System;
using System.Collections.Generic;
using UnityEngine;

public static class SolvableHandGenerator
{
    private const int MaxAttempts = 450;

    public struct HandPiece
    {
        public Vector2Int[] shape;
        public int rotation; // not used (kept for compatibility)
    }

    public static bool TryGenerateHand(
        int boardW, int boardH,
        Func<int, int, bool> isOccupied,
        System.Random rng,
        bool allowRotation,
        out HandPiece[] hand)
    {
        hand = null;

        bool[,] occ = new bool[boardW, boardH];
        int filled = 0;
        for (int y = 0; y < boardH; y++)
        for (int x = 0; x < boardW; x++)
        {
            occ[x, y] = isOccupied(x, y);
            if (occ[x, y]) filled++;
        }

        if (filled >= boardW * boardH) return false;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var cand = new HandPiece[3];

            // Block Blast feel: ensure at least one "small helper" piece
            int smallSlot = rng.Next(0, 3);

            for (int i = 0; i < 3; i++)
            {
                Vector2Int[] shape = (i == smallSlot)
                    ? GetSmallHelper(rng)                   // explicit small pool
                    : BlockBlastShapeLibrary.GetRandom(rng, allowRotation);

                cand[i] = new HandPiece { shape = shape, rotation = 0 };
            }

            if (ExistsAnyOrderPlacement_WithClears((bool[,])occ.Clone(), cand))
            {
                hand = cand;
                return true;
            }
        }

        return false;
    }

    // Small helpers: 1x1, 1x2 (both), 1x3 (both), 3-block L
    // These MUST exist in your BlockBlastShapeLibrary to match shapes.
    static Vector2Int[] GetSmallHelper(System.Random rng)
    {
        // Explicitly list the helper shapes in normalized forms.
        // (These match the shapes we included in the "no rotate" library.)
        Vector2Int[][] helpers =
        {
            new []{ new Vector2Int(0,0) }, // 1x1

            new []{ new Vector2Int(0,0), new Vector2Int(1,0) }, // 1x2 horizontal
            new []{ new Vector2Int(0,0), new Vector2Int(0,1) }, // 1x2 vertical

            new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) }, // 1x3 horizontal
            new []{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2) }, // 1x3 vertical

            new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1) }, // L3
        };

        return helpers[rng.Next(0, helpers.Length)];
    }

    // ---------- Lookahead solver with clears (depth 3) ----------
    private static bool ExistsAnyOrderPlacement_WithClears(bool[,] occOriginal, HandPiece[] hand)
    {
        int[] idx = { 0, 1, 2 };
        foreach (var perm in Perm3(idx))
        {
            var occ = (bool[,])occOriginal.Clone();
            if (TrySolveDepth3_WithClears(occ, hand[perm[0]].shape, hand[perm[1]].shape, hand[perm[2]].shape))
                return true;
        }
        return false;
    }

    private static bool TrySolveDepth3_WithClears(bool[,] occ, Vector2Int[] a, Vector2Int[] b, Vector2Int[] c)
    {
        return TryPlaceAndRecurse(occ, a, () =>
            TryPlaceAndRecurse(occ, b, () =>
                TryPlaceAndRecurse(occ, c, () => true)
            )
        );
    }

    private static bool TryPlaceAndRecurse(bool[,] occ, Vector2Int[] shape, Func<bool> next)
    {
        int w = occ.GetLength(0);
        int h = occ.GetLength(1);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (!CanPlaceAt(occ, x, y, shape))
                continue;

            var placed = new List<Vector2Int>(shape.Length);
            ApplyAt(occ, x, y, shape, true, placed);

            var cleared = new List<Vector2Int>(w + h);
            ApplyLineClears(occ, cleared);

            if (next())
                return true;

            UndoCells(occ, cleared);
            UndoCells(occ, placed);
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

    private static void ApplyAt(bool[,] occ, int ax, int ay, Vector2Int[] shape, bool v, List<Vector2Int> touched)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            int x = ax + shape[i].x;
            int y = ay + shape[i].y;
            occ[x, y] = v;
            touched.Add(new Vector2Int(x, y));
        }
    }

    private static void ApplyLineClears(bool[,] occ, List<Vector2Int> clearedCells)
    {
        int w = occ.GetLength(0);
        int h = occ.GetLength(1);

        for (int y = 0; y < h; y++)
        {
            bool full = true;
            for (int x = 0; x < w; x++)
                if (!occ[x, y]) { full = false; break; }

            if (full)
            {
                for (int x = 0; x < w; x++)
                {
                    if (occ[x, y])
                    {
                        occ[x, y] = false;
                        clearedCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        for (int x = 0; x < w; x++)
        {
            bool full = true;
            for (int y = 0; y < h; y++)
                if (!occ[x, y]) { full = false; break; }

            if (full)
            {
                for (int y = 0; y < h; y++)
                {
                    if (occ[x, y])
                    {
                        occ[x, y] = false;
                        clearedCells.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
    }

    private static void UndoCells(bool[,] occ, List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
            occ[cells[i].x, cells[i].y] = true;
    }
}