public enum LandingGrade { Smooth, Good, Firm, Hard, Crash }

public struct TouchdownReport
{
    public float SinkRate; //m/s
    public float GroundSpeed; //m/s
    public float PeakG; //peak gear load/weight
    public LandingGrade Grade;
}

public static class Landing
{
    public static LandingGrade GradeFor(float sinkRate) => sinkRate switch
    {
        < 1f    => LandingGrade.Smooth,
        < 2f    => LandingGrade.Good,
        < 3f    => LandingGrade.Firm,
        < 4.5f  => LandingGrade.Hard,
        _       => LandingGrade.Crash
    };
}