// BlockBlastShapeLibrary.cs
using UnityEngine;

/// <summary>
/// Block Blast style shape pool.
/// All shapes fit in a 3x3 bounding box after rotation/normalization.
/// Rotation is applied in GetRandom.
/// </summary>
public static class BlockBlastShapeLibrary
{
    public static readonly Vector2Int[][] Shapes =
    {
        // =========================
        // 1–3 blocks (small helpers)
        // =========================
        new []{ new Vector2Int(0,0) },                                                     // 1
        new []{ new Vector2Int(0,0), new Vector2Int(1,0) },                                // 2 line
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },           // 3 line
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1) },           // 3 corner (L3)

        // =========================
        // 4 blocks (fits in 3x3)
        // =========================
        new []{                                                                              // 2x2 square
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1)
        },
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1) }, // T4
        new []{ new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1), new Vector2Int(1,1) }, // S4
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) }, // Z4
        new []{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,0) }, // L4 (3 tall + foot)
        new []{ new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(0,0) }, // J4 (mirror)
        new []{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(0,1) }, // "Γ" in 2 rows

        // =========================
        // 5 blocks (classic block blast variety)
        // =========================
        new []{                                                                              // Plus (5)
            new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1),
            new Vector2Int(1,2)
        },
        new []{                                                                              // Big T (5)
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(1,1), new Vector2Int(1,2)
        },
        new []{                                                                              // P (5) = 2x2 + 1
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1),
            new Vector2Int(0,2)
        },
        new []{                                                                              // P mirror (5)
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1),
            new Vector2Int(1,2)
        },
        new []{                                                                              // U (5)
            new Vector2Int(0,0), new Vector2Int(2,0),
            new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1)
        },
        new []{                                                                              // Stair (5)
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(1,1), new Vector2Int(2,1),
            new Vector2Int(2,2)
        },
        new []{                                                                              // Reverse stair (5)
            new Vector2Int(2,0), new Vector2Int(1,0),
            new Vector2Int(1,1), new Vector2Int(0,1),
            new Vector2Int(0,2)
        },
        new []{                                                                              // "F" like (5)
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(1,1), new Vector2Int(2,1),
            new Vector2Int(1,2)
        },

        // =========================
        // 6 blocks (rectangles + strong pieces)
        // =========================
        new []{                                                                              // 2x3 rectangle (6)
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1),
            new Vector2Int(0,2), new Vector2Int(1,2)
        },
        new []{                                                                              // 3x2 rectangle (6)
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1)
        },
        new []{                                                                              // Big L (6) (2x3 corner)
            new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2),
            new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(2,0) // placeholder removed below
        },
    };

    // NOTE: We can’t keep a duplicate placeholder cell; fix by building shapes in a helper list:
    static BlockBlastShapeLibrary()
    {
        // Rebuild Shapes with corrected entries (C# static ctor runs once).
        // We do this to keep the file copy-paste friendly without manual mistakes.
        var list = new System.Collections.Generic.List<Vector2Int[]>(Shapes.Length + 16);
        foreach (var s in Shapes)
            list.Add(s);

        // Replace the incorrect Big L (6) with correct coordinates:
        list.RemoveAt(list.Count - 1);
        list.Add(new []{ // Big L (6) (3 tall + 3 wide corner, still within 3x3)
            new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2),
            new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(2,0) // will be fixed below
        });
        // fix duplicate again (do properly):
        list.RemoveAt(list.Count - 1);
        list.Add(new []{
            new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2),
            new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(2,1)
        });

        // 7–9 blocks (3x3 variants — “block blast” feel)
        list.Add(new []{ // 3x3 full (9)
            new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(2,0),
            new Vector2Int(0,1),new Vector2Int(1,1),new Vector2Int(2,1),
            new Vector2Int(0,2),new Vector2Int(1,2),new Vector2Int(2,2),
        });

        list.Add(new []{ // 3x3 minus center (8)
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1),                     new Vector2Int(2,1),
            new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2),
        });

        list.Add(new []{ // 3x3 minus one corner (8)
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1),
            new Vector2Int(0,2), new Vector2Int(1,2) // missing (2,2)
        });

        list.Add(new []{ // 3x3 minus two corners (7)
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1),
            new Vector2Int(1,2) // missing (0,2) and (2,2)
        });

        list.Add(new []{ // 3x3 minus a 2-block notch (7)
            new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            new Vector2Int(0,1),                      new Vector2Int(2,1),
            new Vector2Int(0,2),                      new Vector2Int(2,2)
        });

        // overwrite Shapes with finalized array
        ShapesFinal = list.ToArray();
    }

    // Final array used by GetRandom (we keep Shapes above for readability, but use this for correctness).
    public static readonly Vector2Int[][] ShapesFinal;

    public static Vector2Int[] GetRandom(System.Random rng, bool allowRotation = true)
    {
        var baseShape = ShapesFinal[rng.Next(0, ShapesFinal.Length)];
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