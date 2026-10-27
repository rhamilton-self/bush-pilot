using Godot;

public partial class AircraftInput : Node
{
    [Export] public float Response = 4f;
    [Export] public float ThrottleRate = 0.4f;

    public float Pitch { get; private set; }
    public float Roll { get; private set; }
    public float Yaw { get; private set; }
    public float Throttle { get; private set; }
    public float Brake { get; private set; }
    public int Flaps { get; private set; }

    public const int MaxFlaps = 3;


    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        float k = 1f - Mathf.Exp(-Response * dt);

        Pitch = Mathf.Lerp(Pitch, Input.GetAxis("pitch_down", "pitch_up"), k);
        Roll = Mathf.Lerp(Roll, Input.GetAxis("roll_left", "roll_right"), k);
        Yaw = Mathf.Lerp(Yaw, Input.GetAxis("yaw_left", "yaw_right"), k);

        Throttle = Mathf.Clamp(
            Throttle + Input.GetAxis("throttle_down", "throttle_up") * ThrottleRate * dt,
            0f, 1f);

        Brake = Input.GetActionStrength("brake");

        if (Input.IsActionJustPressed("flaps_down")) Flaps = Mathf.Min(Flaps + 1, MaxFlaps);
        if (Input.IsActionJustPressed("flaps_up")) Flaps = Mathf.Max(Flaps - 1, 0);
    }
}