using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplom.DataModule
{
    public class EBSD
    {
        public int Index;
        public int Phase;
        public int AFI;
        public int BC;
        public int BS;
        public int Status;
        public float MAD;
        public Vector2 Pos;
        public Euler Euler;
    }

    public class Phase
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public System.Windows.Media.Color Color { get; set; }
    }

    public struct Vector3
    {
        public Vector3(float _x, float _y, float _z) { x = _x; y = _y; z = _z; }
        public float x, y, z;
        public float length => MathF.Sqrt(x * x + y * y + z * z);
    }

    public struct Vector2
    {
        public Vector2(float _x, float _y) { x = _x; y = _y; }
        public float x, y;
    }

    public struct Vector2Int
    {
        public Vector2Int(int _x, int _y) { x = _x; y = _y; }
        public int x, y;
    }

    public struct GpuColor
    {
        public GpuColor(float _r, float _g, float _b, float _a) { r = _r; g = _g; b = _b; a = _a; }
        public float r, g, b, a;
    }

    public struct Euler
    {
        public Euler(float _x, float _y, float _z) { x = _x; y = _y; z = _z; }
        public float x, y, z;
    }

    public struct Grain
    {
        public float Size => Points.Count + Edges.Count;

        public List<Vector2> Points;
        public List<Vector2> Edges;
    }

}
