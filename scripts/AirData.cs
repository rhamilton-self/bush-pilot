using System.Threading;
using Godot;

public struct AirData
{
    public Vector3 LocalVelocity;
    public float Airspeed;
    public float AoA;
    public float Sideslip;
    public Vector3 Relative;

    public static AirData Compute(Basis basis, Vector3 velocity, Vector3 wind)
    {
        Vector3 relative = velocity - wind;
        Vector3 local = basis.Inverse() * relative;

        return new AirData
        {
            LocalVelocity = local,
            Airspeed = local.Length(),
            AoA = Mathf.Atan2(-local.Y, -local.Z),
            Sideslip = Mathf.Atan2(local.X, -local.Z),
            Relative = relative
        };
    }
}