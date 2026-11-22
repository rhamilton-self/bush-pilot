using Godot;

public partial class Windsock : Node3D
{
    public WindField Wind;
    public GameClock Clock;

    const float PoleHeight = 8f;

    Node3D _pivot;
    Vector3 _dir = new Vector3(0f, -0.5f, 1f).Normalized();
    Vector3 _flat = new Vector3(0f, 0f, 1f);

    public override void _Ready()
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.07f, BottomRadius = 0.09f, Height = PoleHeight },
            Position = new Vector3(0f, PoleHeight / 2f, 0f),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.8f, 0.8f, 0.8f) }
        });

        _pivot = new Node3D { Position = new Vector3(0f, PoleHeight, 0f) };
        AddChild(_pivot);

        var orange = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.45f, 0.05f),
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        var white = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.95f, 0.95f, 0.95f),
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };

        const int segments = 3;
        const float segLength = 1.3f;
        const float baseRadius = 0.5f;
        const float tipRadius = 0.2f;

        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments;
            float t1 = (i + 1) / (float)segments;

            _pivot.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh
                {
                    BottomRadius = Mathf.Lerp(baseRadius, tipRadius, t0),
                    TopRadius = Mathf.Lerp(baseRadius, tipRadius, t1),
                    Height = segLength,
                    CapTop = false,
                    CapBottom = false
                },
                RotationDegrees = new Vector3(-90f, 0f, 0f),
                Position = new Vector3(0f, 0f, -(i + 0.5f) * segLength),
                MaterialOverride = i % 2 == 0 ? orange : white
            });
        }
    }

    public override void _Process(double delta)
    {
        Vector3 pos = GlobalPosition + Vector3.Up * PoleHeight;
        Vector3 mean = Wind.At(pos, Clock.Time);
        Vector3 w = mean + Wind.Gust(pos, Clock.RealSeconds, mean.Length());
        w.Y = 0f;

        float speed = w.Length();
        if (speed > 0.1f) _flat = w / speed;

        float droop = (1f - Mathf.Clamp(speed / 8f, 0f, 1f)) * Mathf.DegToRad(80f);
        var target = new Vector3(_flat.X * Mathf.Cos(droop), -Mathf.Sin(droop), _flat.Z * Mathf.Cos(droop));

        float k = 1f - Mathf.Exp(-4f * (float)delta);
        _dir = _dir.Lerp(target, k).Normalized();

        _pivot.GlobalTransform = new Transform3D(Basis.LookingAt(_dir, Vector3.Up), _pivot.GlobalPosition);
    }
}