using Godot;
using System.Collections.Generic;

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

    [Export] public float SuspensionLength = 0.6f;
    [Export] public float WheelRadius = 0.35f;
    [Export] public float TireGrip = 4000f;          // N per m/s of sliding speed
    [Export] public float TireMu = 0.8f;             // max friction coefficient (dry ground)
    [Export] public float RollingResistance = 0.03f;
    [Export] public float BrakeMu = 0.6f;
    [Export] public float MaxSteerDeg = 35f;

    [Export] public float WingSpan = 11f;
    [Export] public float GroundEffectStrength = 2f;
    [Export] public float GroundLiftGain = 0.3f;
    [Export] public float PropDiscArea = 3.1f; //m^2 - about 2m diameter
    [Export] public float PropwashFactor = 1f;

    public float GroundEffect { get; private set; } //0 = none, higher = stronger
    float _groundFactor = 1f; //induced drag multiplier

    readonly List<Wheel> _wheels = new List<Wheel>();
    Godot.Collections.Array<Rid> _rayExclude;

    public IReadOnlyList<Wheel> Wheels => _wheels;

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
    bool _resetToRunway;

    public event System.Action<TouchdownReport> Touchdown;
    public TouchdownReport? LastTouchdown { get; private set; }

    const float Gravity = 9.81f;

    bool _wasOnGround;
    float _airTime;
    float _impactTimer;
    float _peakLoad;
    float _tdSink;
    float _tdSpeed;

    public bool OnGround
    {
        get
        {
            foreach (var w in _wheels)
                if (w.Grounded) return true;
            return false;
        }
    }

    public override void _Ready()
    {
        Mass = 1100f;
        ContinuousCd = true;

        Controls = new AircraftInput();
        AddChild(Controls);

        BuildPlaceholder();
        BuildWheels();
        _rayExclude = new Godot.Collections.Array<Rid> { GetRid() };

        CenterOfMassMode = CenterOfMassModeEnum.Custom;
        CenterOfMass = Vector3.Zero;

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

        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(1.2f, 1.2f, 7f) } });
        AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(11f, 0.2f, 1.6f) },
            Position = new Vector3(0, 0.3f, -0.5f)
        });
    }

    void BuildWheels()
    {
        _wheels.Add(new Wheel { Mount = new Vector3(-1.6f, -0.5f, 0.5f), Stiffness = 22500f, Damping = 4000f, Braked = true });
        _wheels.Add(new Wheel { Mount = new Vector3(1.6f, -0.5f, 0.5f), Stiffness = 22500f, Damping = 4000f, Braked = true });
        _wheels.Add(new Wheel { Mount = new Vector3(0.0f, -0.5f, -2.5f), Stiffness = 9000f, Damping = 1600f, Steerable = true });

        foreach (var w in _wheels)
        {
            var mesh = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = WheelRadius, BottomRadius = WheelRadius, Height = 0.25f },
                RotationDegrees = new Vector3(0, 0, 90),
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.1f, 0.1f) } 
            };
            AddChild(mesh);
            w.Visual = mesh;
        }
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
        if (Input.IsActionJustPressed("reset_flight")) { _resetRequested = true; _resetToRunway = true; }
        if (Input.IsActionJustPressed("reset_air")) { _resetRequested = true; _resetToRunway = false; }
        
        Air = AirData.Compute(GlobalBasis, LinearVelocity, Vector3.Zero);

        if (DebugFreeFly)
        {
            DebugFly(delta);
            return;
        }

        UpdateFlaps((float)delta);
        ApplySuspension();
        TrackLanding((float)delta);
        ApplyTireForces();
        ApplyThrust();
        UpdateGroundEffect();
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

        float slope = CLSlope * (1f + GroundLiftGain * GroundEffect);
        CL = Aero.LiftCoefficient(Air.AoA, cl0, slope, activeStall, blend);
        StallFactor = Aero.StallFactor(Air.AoA, activeStall, blend);
        float cd = Aero.DragCoefficient(CL, cd0, InducedDragK * _groundFactor, Air.AoA, StallFactor, StallDragGain);

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
        float qTail = Aero.DynamicPressure(TailAirspeed());
        float conf = Mathf.Clamp(Air.Airspeed / 8f, 0f, 1f);

        Vector3 w = GlobalBasis.Inverse() * AngularVelocity;
        float trim = Mathf.DegToRad(TrimAoADeg - FlapTrimShiftDeg * FlapPosition);

        var torque = new Vector3(
            qTail * (Controls.Pitch * ElevatorAuthority
                    - conf * (Air.AoA - trim) * PitchStability
                    - w.X * PitchDamping
                    - conf * Mathf.Sign(Air.AoA) * StallFactor * StallNoseDrop),
            qTail * (-Controls.Yaw * RudderAuthority
                    - conf * Air.Sideslip * YawStability
                    - w.Y * YawDamping),
            q * (-Controls.Roll * AileronAuthority
                + conf * Air.Sideslip * RollStability
                - w.Z * RollDamping));

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

    void ApplySuspension()
    {
        var space = GetWorld3D().DirectSpaceState;
        float reach = SuspensionLength + WheelRadius;

        foreach (var w in _wheels)
        {
            Vector3 from = GlobalTransform * w.Mount;
            Vector3 to = from - GlobalBasis.Y * reach;

            var query = PhysicsRayQueryParameters3D.Create(from,to);
            query.Exclude = _rayExclude;
            var hit = space.IntersectRay(query);

            float distance = reach;
            w.Grounded = hit.Count > 0;

            if (w.Grounded)
            {
                Vector3 point = (Vector3)hit["position"];
                Vector3 normal = (Vector3)hit["normal"];
                distance = from.DistanceTo(point);

                w.Compression = reach - distance;

                Vector3 r = point - GlobalPosition;
                Vector3 pointVelocity = LinearVelocity + AngularVelocity.Cross(r);
                float normalVelocity = pointVelocity.Dot(normal);

                w.Load = Mathf.Max(0f, w.Stiffness * w.Compression - w.Damping * normalVelocity);
                ApplyForce(normal * w.Load, r);

                w.ContactPoint = point;
                w.ContactNormal = normal;
            }
            else
            {
                w.Compression = 0f;
                w.Load = 0f;
            }

            w.Visual.Position = w.Mount + new Vector3(0f, -(distance - WheelRadius), 0f);
        }
    }

    void ApplyTireForces()
    {
        float steerScale = Mathf.Clamp(1f - LinearVelocity.Length() / 30f, 0.2f, 1f);
        float steerAngle = -Controls.Yaw * Mathf.DegToRad(MaxSteerDeg) * steerScale;

        foreach (var w in _wheels)
        {
            float angle = w.Steerable ? steerAngle : 0f;
            w.Visual.Rotation = new Vector3(0f, angle, Mathf.Pi / 2);

            if (!w.Grounded) continue;

            Vector3 n = w.ContactNormal;
            Vector3 r = w.ContactPoint - GlobalPosition;
            Vector3 v = LinearVelocity + AngularVelocity.Cross(r);

            Vector3 wheelForward = GlobalBasis * (new Basis(Vector3.Up, angle) * Vector3.Forward);
            wheelForward = (wheelForward - n * wheelForward.Dot(n)).Normalized();
            Vector3 wheelSide = wheelForward.Cross(n);

            float vLong = v.Dot(wheelForward);
            float vLat = v.Dot(wheelSide);

            float maxLat = TireMu * w.Load;
            float maxLong = (RollingResistance + (w.Braked ? Controls.Brake * BrakeMu : 0f)) * w.Load;

            float fLat = Mathf.Clamp(-vLat * TireGrip, -maxLat, maxLat);
            float fLong = Mathf.Clamp(-vLong * TireGrip, -maxLong, maxLong);

            ApplyForce(wheelForward * fLong + wheelSide * fLat, r);
        }
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

        Vector3 pos = _resetToRunway ? new Vector3(0, 1.3f, 300) : new Vector3(0,300,0);
        Vector3 vel = _resetToRunway ? Vector3.Zero : new Vector3(0, 0, -45f);

        state.Transform = new Transform3D(Basis.Identity, pos);
        state.LinearVelocity = vel;
        state.AngularVelocity = Vector3.Zero;
    }

    void TrackLanding(float dt)
    {
        bool onGround = OnGround;

        if (onGround && !_wasOnGround && _airTime > 0.5f)
        {
            _tdSink = Mathf.Max(0f, -LinearVelocity.Y);
            _tdSpeed = LinearVelocity.Length();
            _impactTimer = 0.4f;
            _peakLoad = 0f;
        }

        if (onGround) _airTime = 0f;
        else _airTime += dt;

        if (_impactTimer > 0f)
        {
            float total = 0f;
            foreach (var w in _wheels) total += w.Load;
            _peakLoad = Mathf.Max(_peakLoad, total);

            _impactTimer -= dt;
            if (_impactTimer <= 0f)
            {
                var report = new TouchdownReport
                {
                    SinkRate = _tdSink,
                    GroundSpeed = _tdSpeed,
                    PeakG = _peakLoad / (Mass * Gravity),
                    Grade = Landing.GradeFor(_tdSink)
                };
                LastTouchdown = report;
                Touchdown?.Invoke(report);
                GD.Print($"Touchdown: {report.Grade}, sink {report.SinkRate:0.0} m/s, {report.PeakG:0.0} g");
            }
        }

        _wasOnGround = onGround;
    }

    void UpdateGroundEffect()
    {
        var space = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            GlobalPosition, GlobalPosition + Vector3.Down * WingSpan);
        query.Exclude = _rayExclude;
        var hit = space.IntersectRay(query);

        if (hit.Count == 0)
        {
            _groundFactor = 1f;
            GroundEffect = 0f;
            return;
        }

        float h = GlobalPosition.DistanceTo((Vector3)hit["position"]) + 0.3f;
        float x = 16f * h / WingSpan;
        float textbook = x * x / (1f + x * x); //1 = no effect

        _groundFactor = Mathf.Max(1f - GroundEffectStrength * (1f - textbook), 0.3f);
        GroundEffect = 1f - _groundFactor;
    }

    public float TailAirspeed()
    {
        float v = Mathf.Max(-Air.LocalVelocity.Z, 0f);
        float half = v * 0.5f;
        float vi = Mathf.Sqrt(Thrust / (2f * Aero.AirDensity * PropDiscArea) + half * half) - half;
        return Air.Airspeed + PropwashFactor * vi;
    }
}