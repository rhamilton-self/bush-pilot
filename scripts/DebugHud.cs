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
            $"FPS      {Engine.GetFramesPerSecond()}";
    }
}