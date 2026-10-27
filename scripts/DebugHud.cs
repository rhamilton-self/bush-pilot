using Godot;

public partial class DebugHud : CanvasLayer
{
    public Aircraft Aircraft;
    Label _label;

    public override void _Ready()
    {
        _label = new Label { Position = new Vector2(16, 16) };
        _label.AddThemeFontSizeOverride("font_size", 18);
        AddChild(_label);
    }

    public override void _Process(double delta)
    {
        var c = Aircraft.Controls;
        _label.Text = 
            $"Pitch    {c.Pitch,6:0.00}\n" +
            $"Roll     {c.Roll,6:0.00}\n" +
            $"Yaw      {c.Yaw,6:0.00}\n" +
            $"Throttle {c.Throttle * 100,5:0}%\n" +
            $"Flaps    {c.Flaps}\n" +
            $"Brake    {c.Brake,6:0.00}\n" +
            $"Altitude {Aircraft.GlobalPosition.Y,6:0} m\n" +
            $"Airspeed {Aircraft.Air.Airspeed,6:0.0} m/s\n" +
            $"Thrust   {Aircraft.Thrust,6:0} N\n" +
            $"Fwd spd  {-Aircraft.Air.LocalVelocity.Z,6:0.0} m/s\n" +
            $"AoA      {Mathf.RadToDeg(Aircraft.Air.AoA),6:0.0} deg\n" +
            $"V/S      {Aircraft.LinearVelocity.Y,6:0.0} m/s\n" +
            $"Lift     {Aircraft.Lift,6:0} N\n" +
            $"Drag     {Aircraft.Drag,6:0} N\n" +
            $"CL       {Aircraft.CL,6:0.00}\n" +
            $"FPS      {Engine.GetFramesPerSecond()}";
    }
}