using Godot;

public class Wheel
{
    //Setup 
    public Vector3 Mount;        // where the strut attaches (aircraft local space)
    public float Stiffness;      // spring, N per metre
    public float Damping;        // damper, N per m/s
    public bool Steerable;
    public bool Braked;

    // Live state, updated every physics tick
    public bool Grounded;
    public float Compression;    // metres
    public float Load;           // newtons pushing up on the aircraft
    public Node3D Visual;        // the wheel mesh
    public Vector3 ContactPoint;
    public Vector3 ContactNormal;
}