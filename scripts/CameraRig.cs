using Godot;

public partial class CameraRig : Node3D
{
    [Export] public Node3D Target;
    [Export] public Vector3 ChaseOffset = new Vector3(0, 3f, 14f);
    [Export] public Vector3 CockpitOffset = new Vector3(0, 0.7f, -1.5f);
    [Export] public float ChaseFollowRate = 5f;
    [Export] public float LookAhead = 6f;
    [Export] public float KeyLookRate = 2f;
    [Export] public float MouseLookSens = 0.005f;
    [Export] public float LookRecenterRate = 4f;

    Camera3D _chase;
    Camera3D _cockpit;
    bool _cockpitActive;

    float _lookYaw;
    float _lookPitch;
    float _zoom = 1f;

    public override void _Ready()
    {
        _chase = new Camera3D { TopLevel = true, Far = 12000f };
        AddChild(_chase);

        _cockpit = new Camera3D { Position = CockpitOffset, Far = 12000f, Fov = 80f };
        Target.AddChild(_cockpit);

        _chase.GlobalPosition = Target.GlobalTransform * ChaseOffset;
        _chase.Current = true;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (Input.IsActionJustPressed("toggle_camera"))
        {
            _cockpitActive = !_cockpitActive;
            if (_cockpitActive) _cockpit.Current = true;
            else _chase.Current = true;
        }

        float lx = Input.GetAxis("look_left", "look_right");
        float ly = Input.GetAxis("look_down", "look_up");
        _lookYaw -= lx * KeyLookRate * dt;
        _lookPitch += ly * KeyLookRate * dt;

        bool looking = lx != 0f || ly != 0f || Input.IsMouseButtonPressed(MouseButton.Right);
        if (!looking)
        {
            float r = 1f - Mathf.Exp(-LookRecenterRate * dt);
            _lookYaw = Mathf.Lerp(_lookYaw, 0f, r);
            _lookPitch = Mathf.Lerp(_lookPitch, 0f, r);
        }

        _lookYaw = Mathf.Clamp(_lookYaw, -Mathf.Pi, Mathf.Pi);
        _lookPitch = Mathf.Clamp(_lookPitch, -1.2f, 0.6f);

        if (_cockpitActive)
        {
            _cockpit.Rotation = new Vector3(_lookPitch, Mathf.Clamp(_lookYaw, -2f, 2f), 0f);
            return;
        }

        Basis orbit = new Basis(Vector3.Up, _lookYaw) * new Basis(Vector3.Right, _lookPitch);
        Vector3 offset = orbit * (ChaseOffset * _zoom);

        float k = 1f - Mathf.Exp(-ChaseFollowRate * dt);
        Vector3 desired = Target.GlobalTransform * offset;
        _chase.GlobalPosition = _chase.GlobalPosition.Lerp(desired, k);

        float ahead = LookAhead * Mathf.Max(Mathf.Cos(_lookYaw), 0f);
        Vector3 lookAt = Target.GlobalPosition - Target.GlobalBasis.Z * ahead;
        _chase.LookAt(lookAt, Vector3.Up);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion m && Input.IsMouseButtonPressed(MouseButton.Right))
        {
            _lookYaw -= m.Relative.X * MouseLookSens;
            _lookPitch -= m.Relative.Y * MouseLookSens;
        }
        else if (e is InputEventMouseButton b && b.Pressed)
        {
            if (b.ButtonIndex == MouseButton.WheelUp)
                _zoom = Mathf.Max(_zoom - 0.1f, 0.5f);
            else if (b.ButtonIndex == MouseButton.WheelDown)
                _zoom = Mathf.Min(_zoom + 0.1f, 2.5f);
        }
    }
}