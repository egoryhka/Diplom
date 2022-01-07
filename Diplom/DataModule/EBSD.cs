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

    public struct Vector2
    {
        public Vector2(float _x, float _y) { x = _x; y = _y; }
        public float x, y;
    }

    public struct Euler
    {
        public Euler(float _x, float _y, float _z) { x = _x; y = _y; z = _z; }
        public float x, y, z;
    }

}
