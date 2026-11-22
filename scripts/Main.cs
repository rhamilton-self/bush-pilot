using Godot;
using System.Collections.Generic;

public partial class Main : Node3D
{
	readonly List<Airstrip> _strips = new();
	int _stripIndex;
	Aircraft _aircraft;
	readonly GameClock _clock = new GameClock();
	readonly WindField _wind = new WindField(1234);
	int _presetIndex;

	Airstrip AddStrip(TerrainHeight terrain, string name, float x, float z,
		float headingDeg, float length, float width, float slope)
	{
		float natural = terrain.Natural(x, z);

		var strip = new Airstrip
		{
			StripName = name,
			Position = new Vector3(x, Mathf.Round(Mathf.Max(natural, 3f)), z),
			HeadingDeg = headingDeg,
			Length = length,
			Width = width,
			Slope = slope,
			Wind = _wind,
			Clock = _clock,
		};

		terrain.AddPad(strip.ToPad());
		_strips.Add(strip);
    	GD.Print($"{name}: natural height {natural:0.0} m, strip elevation {strip.Position.Y:0} m");
		return strip;
	}

	public override void _Ready()
	{
		InputSetup.Register();
		BuildEnvironment();
		BuildWater();

		var terrain = new TerrainHeight(290996);

		var home = AddStrip(terrain, "Home Strip", 0f, 0f, 0f, 800f, 20f, 0f);
		PrintSurvey(terrain);

		AddStrip(terrain, "Valley Strip", 700f, -1050f, 180f, 450f, 15f, 0.04f);
		AddStrip(terrain, "Cove Strip", 1300f, 880f, 0f, 320f, 12f, 0f);

		var terrainNode = new Terrain();
		AddChild(terrainNode);
		terrainNode.Build(terrain);

		foreach (var strip in _strips)
			AddChild(strip);

		_aircraft = new Aircraft
		{
			RunwayStart = home.StartTransform(),
			AirStart = home.AirTransform(250f),
			Wind = _wind,
			Clock = _clock
		};
		_aircraft.Transform = _aircraft.RunwayStart;
		AddChild(_aircraft);

		ApplyPreset();

		AddChild(new CameraRig { Target = _aircraft });
		AddChild(new DebugHud { Aircraft = _aircraft, Clock = _clock });
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

	static void PrintSurvey(TerrainHeight terrain)
	{
	    GD.Print("Survey. ~ sea  . <10m  - <40m  + <100m  # <200m  ^ higher.  Column = 64 m, row = 128 m, O = origin, top = north");
		for (int rz = -15; rz <= 15; rz++)
		{
			var line = new System.Text.StringBuilder();
			for (int cx = -30; cx <= 30; cx++)
			{
				float h = terrain.Height(cx * 64f, rz * 128f);
				char c = h < TerrainHeight.WaterLevel ? '~'
					: h < 10f ? '.'
					: h < 40f ? '-'
					: h < 100f ? '+'
					: h < 200f ? '#'
					: '^';
				if (cx == 0 && rz == 0) c = 'O';
				line.Append(c);
			}
			GD.Print($"{rz * 128,6} {line}");
		}
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("weather_next"))
		{
			_presetIndex = (_presetIndex + 1) % WeatherPreset.All.Length;
			ApplyPreset();
		}

		if (Input.IsActionJustPressed("time_skip")) _clock.Time += 3600.0;
		if (!Input.IsActionJustPressed("next_strip")) return;

		_stripIndex = (_stripIndex + 1) % _strips.Count;
		var strip = _strips[_stripIndex];

		_aircraft.RunwayStart = strip.StartTransform();
		_aircraft.AirStart = strip.AirTransform(250f);
		_aircraft.RequestReset(true);

		GD.Print($"Spawn: {strip.StripName}");
	}

	public override void _PhysicsProcess(double delta)
	{
		_clock.Advance(delta);
	}

	void ApplyPreset()
	{
		var p = WeatherPreset.All[_presetIndex];

		_wind.PresetName = p.Name;
		_wind.Turbulence = p.Turbulence;
		_wind.UseFixed = !p.Dynamic;

		if (!p.Dynamic)
		{
			float baseHeading = p.RelativeToRunway ? _strips[_stripIndex].HeadingDeg : 0f;
			_wind.FixedSpeed = p.Speed;
			_wind.FixedFromDeg = Mathf.PosMod(baseHeading + p.FromDeg, 360f);
		}
	}
}

