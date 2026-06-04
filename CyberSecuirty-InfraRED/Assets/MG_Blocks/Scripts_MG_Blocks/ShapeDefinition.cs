using UnityEngine;

public static class BlockBlastShapeLibrary
{
    // NOTE: Player does NOT rotate. We include orientations explicitly.
    public static readonly Vector2Int[][] Shapes =
    {
        // ---- Small helpers ----
        new []{ new Vector2Int(0,0) }, // 1x1

        // 1x2
        new []{ new Vector2Int(0,0), new Vector2Int(1,0) },
        new []{ new Vector2Int(0,0), new Vector2Int(0,1) },

        // 1x3
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },
        new []{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2) },

        // ---- I (4) ----
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0) },
        new []{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3) },

        // ---- O (2x2) ----
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },

        // ---- T (4) all orientations ----
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1) }, // up
        new []{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(1,0) }, // down
        new []{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,1) }, // right
        new []{ new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(0,1) }, // left

        // ---- S and Z (4) both orientations ----
        new []{ new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // S horizontal
        new []{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,2) }, // S vertical

        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) }, // Z horizontal
        new []{ new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(0,2) }, // Z vertical

        // ---- L and J (4) all orientations ----
        // L variants
        new []{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,0) },
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1) },
        new []{ new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2), new Vector2Int(2,1) },
        new []{ new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(0,2) },

        // J variants (mirror)
        new []{ new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(0,0) },
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(2,1) },
        new []{ new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(0,0) },
        new []{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(0,0) },

        // ---- Big pieces common in Block Blast ----
        // 1x5
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0) },
        new []{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(0,4) },

        // 2x3 rectangle
        new []{
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1),
            new Vector2Int(0,2), new Vector2Int(1,2)
        },
        new []{
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1)
        },

        // 3x3
        new []{
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1),
            new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2)
        }
    };

    public static Vector2Int[] GetRandom(System.Random rng, bool allowRotation = false)
    {
        // rotation is intentionally ignored to match Block Blast "no rotate" gameplay;
        // we include orientations explicitly in Shapes.
        return Shapes[rng.Next(0, Shapes.Length)];
    }
}