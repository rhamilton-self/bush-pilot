using Godot;

public partial class Aircraft : RigidBody3D
{
    //DEBUG
    [Export] public bool DebugFreeFly = false;
    [Export] public float DebugMaxSpeed = 60f;
    
    //PROPULSION
    [Export] public float EnginePower = 170000f; //Watts (about 230hp)
    [Export] public float PropEfficiency = 0.8f;
    [Export] public float StaticThrust = 3000f; //Newtons

    public float Thrust { get; private set; }

    //AERODYNAMICS
    [Export] public float WingArea = 16f; //m^2
    [Export] public float CL0 = 0.3f;
    [Export] public float CLSlope = 5f; //per radian
    [Export] public float CLMax = 1.6f; //temporary cap
    [Export] public float CD0 = 0.03f; //zero-lift drag
    [Export] public float InducedDragK = 0.055f;

    public float Lift { get; private set; }
    public float Drag { get; private set; }
    public float CL { get; private set; }

    public AircraftInput Controls { get; private set; }
    public AirData Air { get; private set; }

    public override void _Ready()
    {
        Mass = 1100f;

        Controls = new AircraftInput();
        AddChild(Controls);

        BuildPlaceholder();

        LinearDampMode = DampMode.Replace;
        LinearDamp = 0f;

        AngularDampMode = DampMode.Replace;
        AngularDamp = 0f;

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
        Air = AirData.Compute(GlobalBasis, LinearVelocity, Vector3.Zero);

        if (DebugFreeFly)
        {
            DebugFly(delta);
            return;
        }

        ApplyThrust();
        ApplyAerodynamics();
    }

    void DebugFly(double delta)
    {
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

    void ApplyThrust()
    {
        float forwardSpeed = -Air.LocalVelocity.Z;
        Thrust = Propulsion.Thrust(Controls.Throttle, forwardSpeed,
            EnginePower, PropEfficiency, StaticThrust);
        ApplyCentralForce(-GlobalBasis.Z * Thrust);
    }

    void ApplyAerodynamics()
    {
        if (Air.Airspeed < 0.5f)
        {
            Lift = 0f; Drag = 0f; CL = 0f;
            return;
        }

        Vector3 flowDir = Air.Relative / Air.Airspeed;
        float q = Aero.DynamicPressure(Air.Airspeed);

        CL = Aero.LiftCoefficient(Air.AoA, CL0, CLSlope, CLMax);
        float cd = Aero.DragCoefficient(CL, CD0, InducedDragK);

        Lift = q * WingArea * CL;
        Drag = q * WingArea * cd;

        Vector3 liftDir = GlobalBasis.X.Cross(flowDir).Normalized();

        ApplyCentralForce(liftDir * Lift);
        ApplyCentralForce(-flowDir * Drag);
    }
}