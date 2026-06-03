using UnityEngine;

/// <summary>
/// Standard "Block Blast" style pool (no weird blocks).
/// All shapes fit within a 3x3 bounding box after rotation/normalization.
/// Rotation is applied in GetRandom.
/// </summary>
public static class BlockBlastShapeLibrary
{
    public static readonly Vector2Int[][] Shapes =
    {
        // ----------------
        // Small helpers
        // ----------------
        new []{ new Vector2Int(0,0) }, // 1

        new []{ new Vector2Int(0,0), new Vector2Int(1,0) }, // 2-line

        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) }, // 3-line

        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1) }, // 3-corner (L3)

        // ----------------
        // 4 blocks (3x3-friendly tetrominoes)
        // ----------------
        new []{ // 2x2
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1)
        },

        new []{ // T4
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(1,1)
        },

        new []{ // S4
            new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1), new Vector2Int(1,1)
        },

        new []{ // Z4
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(1,1), new Vector2Int(2,1)
        },

        new []{ // L4 (3 tall + foot)
            new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2),
            new Vector2Int(1,0)
        },

        // ----------------
        // 5 blocks (common in block-blast style pools)
        // ----------------
        new []{ // Plus (5)
            new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1),
            new Vector2Int(1,2)
        },

        new []{ // Big T (5)
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(1,1), new Vector2Int(1,2)
        },

        new []{ // P (5) : 2x2 + one on top-left
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1),
            new Vector2Int(0,2)
        },

        new []{ // Stair (5)
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(1,1), new Vector2Int(2,1),
            new Vector2Int(2,2)
        },

        // ----------------
        // 6 blocks (rectangles show up a lot in these games)
        // ----------------
        new []{ // 2x3 rectangle
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1),
            new Vector2Int(0,2), new Vector2Int(1,2)
        },

        new []{ // 3x2 rectangle
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1)
        },

        // ----------------
        // 3x3 max pieces (signature of the genre)
        // ----------------
        new []{ // 3x3 full (9)
            new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(2,0),
            new Vector2Int(0,1),new Vector2Int(1,1),new Vector2Int(2,1),
            new Vector2Int(0,2),new Vector2Int(1,2),new Vector2Int(2,2),
        },

        new []{ // 3x3 minus center (8)
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1),                      new Vector2Int(2,1),
            new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2),
        },

        new []{ // 3x3 minus one corner (8)
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1),
            new Vector2Int(0,2), new Vector2Int(1,2) // missing (2,2)
        },
    };

    public static Vector2Int[] GetRandom(System.Random rng, bool allowRotation = true)
    {
        var baseShape = Shapes[rng.Next(0, Shapes.Length)];
        if (!allowRotation) return baseShape;

        int rot = rng.Next(0, 4);
        return Normalize(Rotate(baseShape, rot));
    }

    static Vector2Int[] Rotate(Vector2Int[] shape, int quarterTurnsCW)
    {
        var dst = new Vector2Int[shape.Length];
        for (int i = 0; i < shape.Length; i++)
        {
            var p = shape[i];
            for (int r = 0; r < (quarterTurnsCW & 3); r++)
                p = new Vector2Int(p.y, -p.x); // 90° CW
            dst[i] = p;
        }
        return dst;
    }

    static Vector2Int[] Normalize(Vector2Int[] shape)
    {
        int minX = int.MaxValue, minY = int.MaxValue;
        for (int i = 0; i < shape.Length; i++)
        {
            minX = Mathf.Min(minX, shape[i].x);
            minY = Mathf.Min(minY, shape[i].y);
        }

        var norm = new Vector2Int[shape.Length];
        for (int i = 0; i < shape.Length; i++)
            norm[i] = new Vector2Int(shape[i].x - minX, shape[i].y - minY);

        return norm;
    }
}