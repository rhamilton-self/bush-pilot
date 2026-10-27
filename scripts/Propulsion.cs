using Godot;

public static class Propulsion
{
    public static float Thrust(float throttle, float forwardSpeed,
        float power, float efficiency, float staticThrust)
    {
        float v = Mathf.Max(forwardSpeed, 1f);
        float fromPower = throttle * power * efficiency / v;
        return Mathf.Min(fromPower, throttle * staticThrust);
    }
}