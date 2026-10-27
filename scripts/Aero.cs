using Godot;

public static class Aero
{
    public const float AirDensity = 1.225f; //kg/m^3 at sea level

    public static float DynamicPressure(float airspeed)
        => 0.5f * AirDensity * airspeed * airspeed;

    public static float LiftCoefficient(float aoa, float cl0, float slope, float clMax)
    {
        float cl = cl0 + slope * aoa;
        return Mathf.Clamp(cl, -clMax, clMax);
    }

    public static float DragCoefficient(float cl, float cd0, float k)
        => cd0 + k * cl * cl;
}