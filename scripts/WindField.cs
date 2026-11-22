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

    public WindField(int seed)
    {
        _dirNoise = MakeNoise(seed);
        _speedNoise = MakeNoise(seed + 101);
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
        float nx = position.X / MetresPerFeature;
        float nz = position.Z / MetresPerFeature;
        float nt = (float)(timeSeconds / 3600.0) / HoursPerFeature;

        float fromDeg = PrevailingFromDeg + _dirNoise.GetNoise3D(nx, nt, nz) * DirectionSwingDeg;
        float speed = Mathf.Max(MeanSpeed + _speedNoise.GetNoise3D(nx, nt, nz) * SpeedVariation, 0f);

        float h = Mathf.DegToRad(fromDeg);
        return new Vector3(-Mathf.Sin(h), 0f, Mathf.Cos(h)) * speed;
    }
}