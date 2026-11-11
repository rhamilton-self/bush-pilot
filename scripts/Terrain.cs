using System.Threading.Tasks;
using Godot;

public partial class Terrain : StaticBody3D
{
    public const float Size = 4096f;
    public const float Cell = 16f;

    public void Build(TerrainHeight terrain)
    {
        ulong t0 = Time.GetTicksMsec();

        int n = (int)(Size / Cell) + 1;
        float half = Size / 2f;

        var heights = new float[n * n];
        for (int iz = 0; iz < n; iz ++)
        {
            for (int ix = 0; ix < n; ix++)
            {
                heights[iz * n + ix] = terrain.Height(ix * Cell - half, iz * Cell - half);
            }
        }

        var vertices = new Vector3[n*n];
        var normals = new Vector3[n*n];
        var colors = new Color[n*n];

        for (int iz = 0; iz < n; iz++)
        {
            for (int ix = 0; ix < n; ix++)
            {
                int i = iz * n + ix;
                vertices[i] = new Vector3(ix * Cell - half, heights[i], iz * Cell - half);

                float hl = heights[iz * n + Mathf.Max(ix - 1, 0)];
                float hr = heights[iz * n + Mathf.Min(ix + 1, n - 1)];
                float hd = heights[Mathf.Max(iz - 1, 0) * n + ix];
                float hu = heights[Mathf.Min(iz + 1, n - 1) * n + ix];

                normals[i] = new Vector3(hl - hr, 2f * Cell, hd - hu).Normalized();
                colors[i] = ColorFor(heights[i], normals[i].Y);
            }
        }

        var indices = new int[(n - 1) * (n - 1) * 6];
        int k = 0;
        for (int iz = 0; iz < n - 1; iz ++)
        {
            for (int ix = 0; ix < n - 1; ix++)
            {
                int a = iz * n + ix;
                int b = a + 1;
                int c = a + n;
                int d = c + 1;

                indices[k++] = a; indices[k++] = b; indices[k++] = d;
                indices[k++] = a; indices[k++] = d; indices[k++] = c;
            }
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        AddChild(new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true,
                Roughness = 1f
            }
        });

        AddChild(new CollisionShape3D
        {
            Shape = new HeightMapShape3D
            {
                MapWidth = n,
                MapDepth = n,
                MapData = heights
            },
            Scale = new Vector3(Cell, 1f, Cell)
        });

        GD.Print($"Terrain built in {Time.GetTicksMsec() - t0} ms ({n * n} vertices)");
    }

    static Color ColorFor(float h, float normalY)
    {
        var sand = new Color(0.76f, 0.70f, 0.50f);
        var grass = new Color(0.36f, 0.52f, 0.28f);
        var upland = new Color(0.45f, 0.45f, 0.30f);
        var rock = new Color(0.45f, 0.42f, 0.40f);
        var snow = new Color(0.92f, 0.93f, 0.95f);

        Color c = sand.Lerp(grass, Mathf.Clamp((h - 2f) / 6f, 0f, 1f));
        c = c.Lerp(upland, Mathf.Clamp((h - 100f) / 120f, 0f, 1f));
        c = c.Lerp(snow, Mathf.Clamp((h - 270f) / 40f, 0f, 1f));

        c = c.Lerp(new Color(0.18f, 0.28f, 0.30f), Mathf.Clamp((TerrainHeight.WaterLevel - h) / 20f, 0f, 1f));

        float steep = Mathf.Clamp((0.88f - normalY) / 0.18f, 0f, 1f);
        return c.Lerp(rock, steep);
    }
}