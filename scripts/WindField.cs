using Godot;

public class WindField
{
    public float PrevailingFromDeg = 270f;
    public float MeanSpeed = 7f;
    public float SpeedVariation = 6f;
    public float DirectionSwingDeg = 70f;
    public float HoursPerFeature = 5f;
    public float MetresPerFeature = 40000f;

    readonly FastNoiseLite _dirNoise;
    readonly FastNoiseLite _speedNoise;

    public float Turbulence = 1f;          // 0 = calm air, 1 = normal, 2+ = rough
    public float MetresPerGust = 70f;      // gust size
    public float SecondsPerGust = 4f;      // how quickly a gust changes while you hover in it

    readonly FastNoiseLite _gustX;
    readonly FastNoiseLite _gustY;
    readonly FastNoiseLite _gustZ;

    public bool UseFixed;
    public float FixedSpeed;
    public float FixedFromDeg;
    public string PresetName = "Clock-driven";

    public WindField(int seed)
    {
        _dirNoise = MakeNoise(seed);
        _speedNoise = MakeNoise(seed + 101);

        _gustX = MakeNoise(seed + 201);
        _gustY = MakeNoise(seed + 202);
        _gustZ = MakeNoise(seed + 203);
    }

    static FastNoiseLite MakeNoise(int seed) => new FastNoiseLite
    {
        Seed = seed,
        NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
        Frequency = 1f,
        FractalOctaves = 2
    };

    public Vector3 At(Vector3 position, double timeSeconds)
    {
        if (UseFixed)
        {
            float fh = Mathf.DegToRad(FixedFromDeg);
            return new Vector3(-Mathf.Sin(fh), 0f, Mathf.Cos(fh)) * FixedSpeed;
        }

        float nx = position.X / MetresPerFeature;
        float nz = position.Z / MetresPerFeature;
        float nt = (float)(timeSeconds / 3600.0) / HoursPerFeature;

        float fromDeg = PrevailingFromDeg + _dirNoise.GetNoise3D(nx, nt, nz) * DirectionSwingDeg;
        float speed = Mathf.Max(MeanSpeed + _speedNoise.GetNoise3D(nx, nt, nz) * SpeedVariation, 0f);

        float h = Mathf.DegToRad(fromDeg);
        return new Vector3(-Mathf.Sin(h), 0f, Mathf.Cos(h)) * speed;
    }

    public Vector3 Gust(Vector3 position, double realSeconds, float meanSpeed)
    {
        float nx = position.X / MetresPerGust;
        float nz = position.Z / MetresPerGust;
        float nt = (float)realSeconds / SecondsPerGust;

        float horizontal = Turbulence * (0.8f + 0.3f * meanSpeed);

        return new Vector3(
            _gustX.GetNoise3D(nx, nt, nz) * horizontal,
            _gustY.GetNoise3D(nx, nt, nz) * horizontal * 0.5f,
            _gustZ.GetNoise3D(nx, nt, nz) * horizontal);
    }
}