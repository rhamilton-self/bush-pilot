using Godot;

public partial class DebugHud : CanvasLayer
{
    public Aircraft Aircraft;
    Label _label;
    public GameClock Clock;

    public override void _Ready()
    {
        _label = new Label { Position = new Vector2(16, 16) };
        _label.AddThemeFontSizeOverride("font_size", 18);
        AddChild(_label);
    }
    
    public override void _Process(double delta)
    {
        var c = Aircraft.Controls;
        string gear = "";
        foreach (var w in Aircraft.Wheels)
            gear += $"{w.Load,7:0}";

        Vector3 wind = Aircraft.WindNow;
        float windSpeed = wind.Length();
        float windFrom = windSpeed > 0.01f
            ? Mathf.PosMod(Mathf.RadToDeg(Mathf.Atan2(-wind.X, wind.Z)), 360f) : 0f;

        Vector3 fwd = -Aircraft.GlobalBasis.Z;
        float heading = Mathf.PosMod(Mathf.RadToDeg(Mathf.Atan2(fwd.X, -fwd.Z)), 360f);
        Vector3 gv = Aircraft.LinearVelocity;
        float track = Mathf.PosMod(Mathf.RadToDeg(Mathf.Atan2(gv.X, -gv.Z)), 360f);
        float drift = Mathf.Abs(new Vector2(gv.X, gv.Z).Length()) > 5f
            ? Mathf.Wrap(track - heading, -180f, 180f) : 0f;
        float headwind = -wind.Dot(fwd);
        float crosswind = -wind.Dot(Aircraft.GlobalBasis.X);

        _label.Text = 
            $"Time      {Clock.Text}\n" +
            // $"Pitch    {c.Pitch,6:0.00}\n" +
            // $"Roll     {c.Roll,6:0.00}\n" +
            // $"Yaw      {c.Yaw,6:0.00}\n" +
            $"Throttle {c.Throttle * 100,5:0}%\n" +
            $"Flaps    {c.Flaps}  ({Aircraft.FlapPosition * 100,3:0}%)\n" +            
            $"Brake    {c.Brake,6:0.00}\n" +
            $"Gear (N) {gear}\n" +
            $"Gnd spd  {Aircraft.LinearVelocity.Length(),6:0.0} m/s\n" +
            // $"On ground {(Aircraft.OnGround ? "yes" : "no")}\n" +
            $"Landing  {(Aircraft.LastTouchdown is TouchdownReport t ? $"{t.Grade}  {t.SinkRate:0.0} m/s ({t.SinkRate * 196.85f:0} fpm)  {t.PeakG:0.0} g" : "-")}\n" +
            // $"Ground eff {Aircraft.GroundEffect,5:0.00}\n" +
            // $"Tail spd   {Aircraft.TailAirspeed() + 0,5:0.0} m/s\n" +
            $"Altitude {Aircraft.GlobalPosition.Y,6:0} m\n" +
            $"Airspeed {Aircraft.Air.Airspeed,6:0.0} m/s\n" +
            // $"Thrust   {Aircraft.Thrust,6:0} N\n" +
            $"Fwd spd  {-Aircraft.Air.LocalVelocity.Z,6:0.0} m/s\n" +
            $"AoA      {Mathf.RadToDeg(Aircraft.Air.AoA),6:0.0} deg\n" +
            // $"V/S      {Aircraft.LinearVelocity.Y,6:0.0} m/s\n" +
            // $"Lift     {Aircraft.Lift,6:0} N\n" +
            // $"Drag     {Aircraft.Drag,6:0} N\n" +
            // $"CL       {Aircraft.CL,6:0.00}\n" +
            $"Heading   {heading,5:0} deg\n" +
            $"Weather   {Aircraft.Wind.PresetName}\n" +
            $"Wind      {windFrom,4:0} deg at {windSpeed,4:0.0} m/s\n" +
            $"Head/Xwnd {headwind,5:0.0} / {crosswind,5:0.0} m/s\n" +
            $"Gust H/V  {new Vector2(Aircraft.GustNow.X, Aircraft.GustNow.Z).Length(),4:0.0} / {Aircraft.GustNow.Y,5:0.0} m/s\n" +
            $"Drift     {drift,5:0} deg  (track {track,3:0})\n" +
            // $"Pitch att {Mathf.RadToDeg(Mathf.Asin(-Aircraft.GlobalBasis.Z.Y)),5:0} deg\n" +
            // $"Bank      {Mathf.RadToDeg(Mathf.Asin(-Aircraft.GlobalBasis.X.Y)),5:0} deg\n" +
            $"Stall     {Aircraft.StallFactor,5:0.00}   {(Aircraft.StallWarning ? "<< STALL WARNING >>" : "")}\n" +
            $"FPS      {Engine.GetFramesPerSecond()}";
    }
}