// BlockBlastShapeLibrary.cs
using UnityEngine;

/// <summary>
/// Central place to define which shapes can spawn.
/// Shapes are defined as grid offsets relative to an anchor cell (0,0).
/// </summary>
public static class BlockBlastShapeLibrary
{
    // Add/remove shapes here. Keep them normalized (min x/y == 0) for simplicity.
    public static readonly Vector2Int[][] Shapes =
    {
        // 1x1
        new []{ new Vector2Int(0,0) },

        // 2x2
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },

        // 3 line
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },

        // 4 line
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0) },

        // L (3)
        new []{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1) },

        // L (4)
        new []{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,2) },

        // T (4)
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1) },

        // S (4)
        new []{ new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1) },

        // Z (4)
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) },

        // 3x3 full
        new []{
            new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(2,0),
            new Vector2Int(0,1),new Vector2Int(1,1),new Vector2Int(2,1),
            new Vector2Int(0,2),new Vector2Int(1,2),new Vector2Int(2,2),
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