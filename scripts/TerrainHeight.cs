using Godot;
using System.Collections.Generic;



public struct Pad
{
    public Vector2 Center; //world X, Z
    public float Yaw; //radians, same as node's rotation about Y
    public float HalfLength; //along the runway's local Z
    public float HalfWidth; //along the runway's local X
    public float Height; //elevation at the centre
    public float Slope; //rise per meter toward the forward (-Z) end
    public float Blend; //metres over which terrain blends back to natural
}

public class TerrainHeight
{
    public const float WaterLevel = -6f;

    readonly FastNoiseLite _broad;
    readonly FastNoiseLite _detail;
    readonly List<Pad> _pads = new();

    public IReadOnlyList<Pad> Pads => _pads;

    public TerrainHeight(int seed)
    {
        _broad = new FastNoiseLite
        {
            Seed = seed,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Frequency = 0.0004f,
            FractalOctaves = 3
        };
        _detail = new FastNoiseLite
        {
            Seed = seed + 1,
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Frequency = 0.004f,
            FractalOctaves = 2
        };
    }

    public void AddPad(Pad pad) => _pads.Add(pad);

    public float Natural(float x, float z)
    {
        float broad = (_broad.GetNoise2D(x,z) + 1f) * 0.5f;
        float shaped = broad * broad;
        float detail = _detail.GetNoise2D(x, z);

        return -30f + shaped * 450f + detail * (4f + 25f * shaped);
    }

    public float Height(float x, float z)
    {
        float h = Mathf.Lerp(Natural(x, z), -25f, EdgeFalloff(x, z));

        foreach (var p in _pads)
        {
            float dx = x - p.Center.X;
            float dz = z - p.Center.Y;
            float c = Mathf.Cos(p.Yaw);
            float s = Mathf.Sin(p.Yaw);

            float lx = dx * c - dz * s; //position in the runway's own axes
            float lz = dx * s + dz * c;

            float outX = Mathf.Max(Mathf.Abs(lx) - p.HalfWidth, 0f);
            float outZ = Mathf.Max(Mathf.Abs(lz) - p.HalfLength, 0f);
            float dist = Mathf.Sqrt(outX * outX + outZ * outZ);

            float padHeight = p.Height - p.Slope * Mathf.Clamp(lz, -p.HalfLength, p.HalfLength);
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(dist / p.Blend, 0f, 1f));

            h = Mathf.Lerp(padHeight, h, t);
        }

        return h;
    }

    static float EdgeFalloff(float x, float z)
    {
        float d = Mathf.Max(Mathf.Abs(x),  Mathf.Abs(z));
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp((d - 1500f) / 548f, 0f, 1f));
    }
}