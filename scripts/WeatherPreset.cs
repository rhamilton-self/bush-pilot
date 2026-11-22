public class WeatherPreset
{
    public string Name;
    public bool Dynamic;             // true = follow the game clock
    public bool RelativeToRunway;    // FromDeg is an offset from the runway heading
    public float FromDeg;
    public float Speed;
    public float Turbulence = 1f;

    public static readonly WeatherPreset[] All =
    {
        new WeatherPreset { Name = "Clock-driven", Dynamic = true },
        new WeatherPreset { Name = "Calm", Speed = 0f, Turbulence = 0f },
        new WeatherPreset { Name = "Headwind 8", RelativeToRunway = true, FromDeg = 0f,   Speed = 8f,  Turbulence = 0.5f },
        new WeatherPreset { Name = "Crosswind from right 8", RelativeToRunway = true, FromDeg = 90f,  Speed = 8f,  Turbulence = 0.7f },
        new WeatherPreset { Name = "Crosswind from left 12, gusty", RelativeToRunway = true, FromDeg = -90f, Speed = 12f, Turbulence = 1.5f },
        new WeatherPreset { Name = "Tailwind 6", RelativeToRunway = true, FromDeg = 180f, Speed = 6f,  Turbulence = 0.5f },
        new WeatherPreset { Name = "Gale crosswind 18", RelativeToRunway = true, FromDeg = 90f,  Speed = 18f, Turbulence = 2.5f }
    };
}