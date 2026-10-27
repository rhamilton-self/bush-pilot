using Godot;

public partial class CameraRig : Node3D
{
    [Export] public Node3D Target;
    [Export] public Vector3 ChaseOffset = new Vector3(0, 3f, 14f);
    [Export] public Vector3 CockpitOffset = new Vector3(0, 0.7f, -1.5f);
    [Export] public float ChaseFollowRate = 5f;
    [Export] public float LookAhead = 6f;

    Camera3D _chase;
    Camera3D _cockpit;
    bool _cockpitActive;

    public override void _Ready()
    {
        _chase = new Camera3D { TopLevel = true, Far = 8000f };
        AddChild(_chase);

        _cockpit = new Camera3D { Position = CockpitOffset, Far = 8000f, Fov = 80f };
        Target.AddChild(_cockpit);

        _chase.GlobalPosition = Target.GlobalTransform * ChaseOffset;
        _chase.Current = true;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("toggle_camera"))
        {
            _cockpitActive = !_cockpitActive;
            if (_cockpitActive) _cockpit.Current = true;
            else _chase.Current = true;
        }

        if (_cockpitActive) return;

        float k = 1f - Mathf.Exp(-ChaseFollowRate * (float)delta);
        Vector3 desired = Target.GlobalTransform * ChaseOffset;
        _chase.GlobalPosition = _chase.GlobalPosition.Lerp(desired, k);

        Vector3 lookAt = Target.GlobalPosition - Target.GlobalBasis.Z * LookAhead;
        _chase.LookAt(lookAt, Vector3.Up);
    }
}