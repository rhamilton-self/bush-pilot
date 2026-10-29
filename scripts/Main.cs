using Godot;

public partial class Main : Node3D
{
    public override void _Ready()
	{
		InputSetup.Register();
		BuildEnvironment();
		BuildGround();
		BuildRunway();
		BuildPillars();
		
		var aircraft = new Aircraft { Position = new Vector3(0, 1.5f, 300) };
		AddChild(aircraft);

		AddChild(new CameraRig { Target = aircraft });
		AddChild(new DebugHud { Aircraft = aircraft });
	}

	void BuildEnvironment()
	{
		var env = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Sky,
			Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() },
			AmbientLightSource = Godot.Environment.AmbientSource.Sky
		};
		AddChild(new WorldEnvironment { Environment = env });

		AddChild(new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-50, -30, 0),
			ShadowEnabled = true
		});
	}

	void BuildGround()
	{
		var ground = new StaticBody3D();

		ground.AddChild(new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(6000, 1, 6000) },
			Position = new Vector3(0, -0.5f, 0)
		});

		ground.AddChild(new MeshInstance3D
		{
			Mesh = new PlaneMesh { Size = new Vector2(6000, 6000) },
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.35f, 0.5f, 0.3f)}
		});

		AddChild(ground);
	}

	void BuildPillars()
	{
		var rng = new RandomNumberGenerator { Seed = 1234 };
		var mat = new StandardMaterial3D { AlbedoColor = new Color(0.6f, 0.55f, 0.5f) };

		for (int i = 0; i < 250; i++)
		{
			float h = rng.RandfRange(5f, 40f);
			var size = new Vector3(rng.RandfRange(4f, 12f), h, rng.RandfRange(4f, 12f));
			float x = rng.RandfRange(-2500, 2500);
			float z = rng.RandfRange(-2500, 2500);

			if (Mathf.Abs(x) < 80f && Mathf.Abs(z) < 600f) continue;

			AddChild(new MeshInstance3D
			{
				Mesh = new BoxMesh { Size = size },
				Position = new Vector3(x, h / 2f, z),
				MaterialOverride = mat
			});
		}
	}

	void BuildRunway()
	{
		AddChild(new MeshInstance3D
		{
			Mesh = new PlaneMesh { Size = new Vector2(20, 700) },
			Position = new Vector3(0, 0.02f, 0),
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.2f, 0.2f, 0.22f) }
		});

		var paint = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.9f, 0.9f) };
		for (int i = -16; i <= 16; i++)
		{
			AddChild(new MeshInstance3D
			{
				Mesh = new PlaneMesh { Size = new Vector2(0.6f, 8f)},
				Position = new Vector3(0, 0.03f, i * 20f),
				MaterialOverride = paint
			});
		}
	}
}

