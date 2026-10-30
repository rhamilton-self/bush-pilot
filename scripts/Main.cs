using Godot;

public partial class Main : Node3D
{
    public override void _Ready()
	{
		var terrain = new TerrainHeight(1234);
		terrain.AddPad(new Pad
		{
			Center = new Vector2(0,0),
			Yaw = 0f,
			HalfLength = 400f,
			HalfWidth = 40f,
			Height = 0f,
			Slope = 0f,
			Blend = 150f
		});

		var terrainNode = new Terrain();
		AddChild(terrainNode);
		terrainNode.Build(terrain);

		float min = float.MaxValue, max = float.MinValue, sum = 0f;
		int count = 0;
		for (float x = -2000f; x <= 2000f; x += 40f)
		{	
			for (float z = -2000f; z <= 2000f; z += 40f)
			{
				float h = terrain.Height(x, z);
				min = Mathf.Min(min, h);
				max = Mathf.Max(max, h);
				sum += h;
				count ++;
			}
		}

		InputSetup.Register();
		BuildEnvironment();
		BuildRunway();
				
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
			Position = new Vector3(0, 0.05f, 0),
			MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.2f, 0.2f, 0.22f) }
		});

		var paint = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.9f, 0.9f) };
		for (int i = -16; i <= 16; i++)
		{
			AddChild(new MeshInstance3D
			{
				Mesh = new PlaneMesh { Size = new Vector2(0.6f, 8f)},
				Position = new Vector3(0, 0.06f, i * 20f),
				MaterialOverride = paint
			});
		}
	}
}

