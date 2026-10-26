using Godot;

public partial class Aircraft : RigidBody3D
{
    [Export] public bool DebugFreeFly = true;
    [Export] public float DebugMaxSpeed = 60f;

    public AircraftInput Controls { get; private set; }

    public override void _Ready()
    {
        Mass = 1100f;

        Controls = new AircraftInput();
        AddChild(Controls);

        BuildPlaceholder();

        FreezeMode = FreezeModeEnum.Kinematic;
        Freeze = DebugFreeFly;
    }

    void BuildPlaceholder()
    {
        var body = new Color(0.85f, 0.85f, 0.85f);
        var trim = new Color(0.9f, 0.3f, 0.2f);

        AddBox(new Vector3(1.2f, 1.2f, 7f),   new Vector3(0, 0, 0),       body);  // fuselage
        AddBox(new Vector3(11f, 0.2f, 1.6f),  new Vector3(0, 0.3f, -0.5f), trim); // wing
        AddBox(new Vector3(3.5f, 0.15f, 1f),  new Vector3(0, 0.3f, 3.2f),  trim); // horizontal tail
        AddBox(new Vector3(0.15f, 1.4f, 1f),  new Vector3(0, 1f, 3.2f),    trim); // fin
        AddBox(new Vector3(0.8f, 0.8f, 0.6f), new Vector3(0, 0, -3.5f),    new Color(1f, 0.8f, 0.1f)); // nose marker

        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(11f, 1.2f, 7f) } });
    }

    void AddBox(Vector3 size, Vector3 pos, Color color)
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size },
            Position = pos,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color }
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!DebugFreeFly) return;

        float dt = (float)delta;
        Transform3D t = GlobalTransform;
        Basis b = t.Basis;

        b = b.Rotated(b.X,  Controls.Pitch * 1.2f * dt);
        b = b.Rotated(b.Z,  -Controls.Roll  * 1.8f * dt);
        b = b.Rotated(b.Y, -Controls.Yaw   * 0.6f * dt);
        b = b.Orthonormalized();

        Vector3 forward = -b.Z;
        Vector3 origin = t.Origin + forward * DebugMaxSpeed * Controls.Throttle * dt;
        GlobalTransform = new Transform3D(b, origin);
    }
}