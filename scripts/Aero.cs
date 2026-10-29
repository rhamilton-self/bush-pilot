using System;
using Godot;

public static class Aero
{
    public const float AirDensity = 1.225f; //kg/m^3 at sea level

    public static float DynamicPressure(float airspeed)
        => 0.5f * AirDensity * airspeed * airspeed;

    // public static float LiftCoefficient(float aoa, float cl0, float slope, float clMax)
    // {
    //     float cl = cl0 + slope * aoa;
    //     return Mathf.Clamp(cl, -clMax, clMax);
    // }

    // public static float DragCoefficient(float cl, float cd0, float k)
    //     => cd0 + k * cl * cl;

    public static float StallFactor(float aoa, float stallAoa, float blendWidth)
    {
        float x = Mathf.Clamp((Mathf.Abs(aoa) - stallAoa) / blendWidth, 0f, 1f);
        return x * x * (3f - 2f * x);
    }

    public static float LiftCoefficient(float aoa, float cl0, float slope,
        float stallAoa, float blendWidth)
    {
        float clPeak = cl0 + slope * stallAoa;
        float attached = Mathf.Clamp(cl0 + slope * aoa, -clPeak, clPeak);
        float flatPlate = 2f * Mathf.Sin(aoa) * Mathf.Cos(aoa);
        float s = StallFactor(aoa, stallAoa, blendWidth);
        return Mathf.Lerp(attached, flatPlate * 0.7f, s);
    }

    public static float DragCoefficient(float cl, float cd0, float k, float aoa,
                                        float stallFactor, float stallDrag)
    {
        float sinA = Mathf.Sin(aoa);
        return cd0 + k * cl * cl + stallFactor * stallDrag * sinA * sinA;
    }
}