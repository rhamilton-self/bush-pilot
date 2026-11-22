public class GameClock
{
    public const double DaySeconds = 86400.0;
    public const double RealSecondsPerDay = 5400.0;   // 90 real minutes

    public double Time = 8.0 * 3600.0;                // game seconds, starting at 08:00 on day 1

    public double Scale => DaySeconds / RealSecondsPerDay;   // 16x
    public int Day => (int)(Time / DaySeconds) + 1;
    public double HourOfDay => (Time % DaySeconds) / 3600.0;

    public void Advance(double realDelta) => Time += realDelta * Scale;

    public double RealSeconds => Time / Scale;

    public string Text
    {
        get
        {
            int hh = (int)HourOfDay;
            int mm = (int)((HourOfDay - hh) * 60.0);
            return $"Day {Day}  {hh:00}:{mm:00}";
        }
    }
}