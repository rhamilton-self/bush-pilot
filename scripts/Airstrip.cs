using Godot;

public partial class Airstrip : Node3D
{
    public const float PadMargin = 25f;
    public const float Blend = 150f;

    public string StripName = "Strip";
    public float Length = 800f;
    public float Width = 20f;
    public float Slope = 0f; //rise per metre toward the far end
    public float HeadingDeg = 0f; //clockwise from north (-Z)

    public float Yaw => -Mathf.DegToRad(HeadingDeg);

    public Pad ToPad() => new Pad
    {
        Center = new Vector2(Position.X, Position.Z),
        Yaw = Yaw,
        HalfLength = Length / 2f + PadMargin,
        HalfWidth = Width / 2f + PadMargin,
        Height = Position.Y,
        Slope = Slope,
        Blend = Blend
    };

    public override void _Ready()
    {
        Rotation = new Vector3(0f, Yaw, 0f);

        float tilt = Mathf.Atan(Slope);
        float len = Length / Mathf.Cos(tilt);

        var surface = new Node3D
        {
            Position = new Vector3(0f, 0.05f, 0f),
            Rotation = new Vector3(tilt, 0f, 0f)
        };
        AddChild(surface);

        surface.AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(Width, len) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.2f, 0.2f, 0.22f) }
        });

        var paint = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.9f, 0.9f) };

        for (float z = -len / 2f + 40f; z <= len / 2f - 40f; z += 20f)
            AddPaint(surface, paint, new Vector2(0.6f, 8f), z);

        AddPaint(surface, paint, new Vector2(Width * 0.8f, 2f), -len / 2f + 4f);
        AddPaint(surface, paint, new Vector2(Width * 0.8f, 2f), len / 2f - 4f);
    }

    static void AddPaint(Node3D parent, Material mat, Vector2 size, float z)
    {
        parent.AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = size },
            Position = new Vector3(0f, 0.02f, z),
            MaterialOverride = mat
        });
    }

    public Transform3D StartTransform()
    {
        float lz = Length / 2f - 60f;
        var local = new Vector3(0f, 1.3f - Slope * lz, lz);
        return GlobalTransform * new Transform3D(Basis.Identity, local);
    }

    public Transform3D AirTransform(float altitude)
    {
        var local = new Vector3(0f, altitude, Length / 2f);
        return GlobalTransform * new Transform3D(Basis.Identity, local);
    }
}