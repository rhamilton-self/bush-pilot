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
    [Export] public float CL0 = 0.4f;
    [Export] public float CLSlope = 5f; //per radian
    [Export] public float CD0 = 0.03f; //zero-lift drag
    [Export] public float InducedDragK = 0.055f;
    [Export] public float StallAoADeg = 15f;
    [Export] public float StallBlendDeg = 6f;
    [Export] public float StallWarnMarginDeg = 3f;
    [Export] public float BuffetStrength = 2f;

    [Export] public float ElevatorAuthority = 5.5f;
    [Export] public float AileronAuthority = 4f;
    [Export] public float RudderAuthority = 3f;

    [Export] public float PitchStability = 14f;
    [Export] public float YawStability = 10f;
    [Export] public float TrimAoADeg = 1.5f;

    [Export] public float PitchDamping = 10f;
    [Export] public float RollDamping = 6.5f;
    [Export] public float YawDamping = 7f;

    [Export] public float FlapCL0Gain = 0.5f;           // extra lift at full flaps
    [Export] public float FlapCD0Gain = 0.035f;         // extra drag at full flaps
    [Export] public float FlapStallReductionDeg = 2f;   // critical AoA drops this much
    [Export] public float FlapTrimShiftDeg = 3f;        // nose-down trim shift
    [Export] public float FlapSpeed = 0.4f;             // travel per second (0.4 = 2.5 s full travel)
    [Export] public float RollStability = 5f;
    [Export] public float IdlePropDrag = 0.012f;
    [Export] public float StallNoseDrop = 4f;
    [Export] public float StallDragGain = 3.5f;
    [Export] public float StallHysteresisDeg = 5f;

    bool _stalled;

    float EffectiveStallAoA => Mathf.DegToRad(StallAoADeg - FlapStallReductionDeg * FlapPosition);

    public float FlapPosition { get; private set; }     // 0 to 1

    public float Lift { get; private set; }
    public float Drag { get; private set; }
    public float CL { get; private set; }
    public float StallFactor { get; private set; }
    public bool StallWarning { get; private set; }

    public AircraftInput Controls { get; private set; }
    public AirData Air { get; private set; }

    bool _resetRequested;

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

        Inertia = new Vector3(1825f, 2667f, 1285f);   // pitch (X), yaw (Y), roll (Z), kg/m^2

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
        if (Input.IsActionJustPressed("reset_flight")) _resetRequested = true;
        Air = AirData.Compute(GlobalBasis, LinearVelocity, Vector3.Zero);

        if (DebugFreeFly)
        {
            DebugFly(delta);
            return;
        }

        UpdateFlaps((float)delta);
        ApplyThrust();
        ApplyAerodynamics();
        ApplyControlTorques();
        ApplyBuffet();
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
            StallFactor = 0f; StallWarning = false;
            _stalled = false;
            return;
        }

        Vector3 flowDir = Air.Relative / Air.Airspeed;
        float q = Aero.DynamicPressure(Air.Airspeed);

        float stallAoA = EffectiveStallAoA;
        float blend = Mathf.DegToRad(StallBlendDeg);
        float cl0 = CL0 + FlapCL0Gain * FlapPosition;
        float cd0 = CD0 + FlapCD0Gain * FlapPosition + IdlePropDrag * (1f - Controls.Throttle);

        float hyst = Mathf.DegToRad(StallHysteresisDeg);
        float absAoA = Mathf.Abs(Air.AoA);
        if (absAoA > stallAoA) _stalled = true;
        else if (absAoA < stallAoA - hyst) _stalled = false;
        float activeStall = _stalled ? stallAoA - hyst : stallAoA;

        CL = Aero.LiftCoefficient(Air.AoA, cl0, CLSlope, activeStall, blend);
        StallFactor = Aero.StallFactor(Air.AoA, activeStall, blend);
        float cd = Aero.DragCoefficient(CL, cd0, InducedDragK, Air.AoA, StallFactor, StallDragGain);

        StallWarning = Air.AoA > stallAoA - Mathf.DegToRad(StallWarnMarginDeg);

        Lift = q * WingArea * CL;
        Drag = q * WingArea * cd;

        Vector3 liftDir = GlobalBasis.X.Cross(flowDir).Normalized();

        ApplyCentralForce(liftDir * Lift);
        ApplyCentralForce(-flowDir * Drag);
    }

    void ApplyControlTorques()
    {
        float q = Aero.DynamicPressure(Air.Airspeed);
        Vector3 w = GlobalBasis.Inverse() * AngularVelocity; //rotation rates in the aircraft's own axes
        float trim = Mathf.DegToRad(TrimAoADeg - FlapTrimShiftDeg * FlapPosition);

        var torque = new Vector3(
            q * (Controls.Pitch * ElevatorAuthority - (Air.AoA - trim) * PitchStability - w.X * PitchDamping
                - Mathf.Sign(Air.AoA) * StallFactor * StallNoseDrop),            
            q * (-Controls.Yaw * RudderAuthority - Air.Sideslip * YawStability - w.Y * YawDamping),
            q * (-Controls.Roll * AileronAuthority + Air.Sideslip * RollStability - w.Z * RollDamping));

        ApplyTorque(GlobalBasis * torque);
    }

    void ApplyBuffet()
    {
        float warnAoA = EffectiveStallAoA - Mathf.DegToRad(StallWarnMarginDeg);        float intensity = Mathf.Clamp((Air.AoA - warnAoA) / Mathf.DegToRad(8f), 0f, 1f);
        if (intensity <= 0f) return;

        float q = Aero.DynamicPressure(Air.Airspeed);
        var shake = new Vector3((GD.Randf() - 0.5f) * 0.6f, 0f, (GD.Randf() - 0.5f) * 2f);
        ApplyTorque(GlobalBasis * (shake * q * intensity * BuffetStrength));
    }

    void UpdateFlaps(float dt)
    {
        float target = Controls.Flaps / (float)AircraftInput.MaxFlaps;
        FlapPosition = Mathf.MoveToward(FlapPosition, target, FlapSpeed * dt);
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (!_resetRequested) return;
        _resetRequested = false;

        state.Transform = new Transform3D(Basis.Identity, new Vector3(0,300,0));
        state.LinearVelocity = new Vector3(0, 0, -45f);
        state.AngularVelocity = Vector3.Zero;
    }
}