using Godot;

public partial class Main : Node3D
{
	public override void _Ready()
	{
		InputSetup.Register();
		BuildEnvironment();
		BuildWater();

		var terrain = new TerrainHeight(290996);

		var home = new Airstrip
		{
			StripName = "Home Strip",
			Position = new Vector3(0f, 0f, 0f),
			Length = 800f,
			Width = 20f
		};
		terrain.AddPad(home.ToPad());

		var terrainNode = new Terrain();
		AddChild(terrainNode);
		terrainNode.Build(terrain);

		AddChild(home);

		var aircraft = new Aircraft
		{
			RunwayStart = home.StartTransform(),
			AirStart = home.AirTransform(250f)
		};
		aircraft.Transform = aircraft.RunwayStart;
		AddChild(aircraft);

		AddChild(new CameraRig { Target = aircraft });
		AddChild(new DebugHud { Aircraft = aircraft });
	}

	void BuildEnvironment()
	{
		var horizon = new Color(0.65f, 0.73f, 0.82f);

		var env = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Sky,
			Sky = new Sky { 
				SkyMaterial = new ProceduralSkyMaterial
				{
					SkyHorizonColor = horizon,
					GroundHorizonColor = horizon
				}
			},
			AmbientLightSource = Godot.Environment.AmbientSource.Sky,
			FogEnabled = true,
			FogLightColor = horizon,
			FogDensity = 0.0002f
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

	void BuildWater()
	{
		AddChild(new MeshInstance3D
		{
			Mesh = new PlaneMesh { 
				Size = new Vector2(40000, 40000),
				SubdivideWidth = 199,
				SubdivideDepth = 199
			},
			Position = new Vector3(0, TerrainHeight.WaterLevel, 0),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = new Color(0.08f, 0.32f, 0.45f, 0.82f),
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				Roughness = 0.1f
			}
		});
	}
}

