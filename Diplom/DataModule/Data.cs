using System;
using System.Linq;

namespace Diplom.DataModule
{
    [Serializable]
    public class Data
    {
        public Settings Settings { get; set; } = new Settings();
        public EBSD[] Points;
        public Vector2Int Size;
        public Euler[] Eulers;
        public Grain[] Grains;
        public int[] BC;

        public void Initialize()
        {
            if (Points.Length == 0) return;

            Eulers = Points.Select(x => x.Euler).ToArray();
            BC = Points.Select(x => x.BC).ToArray();
            Size.x = Points.Count(x => x.Pos.y == 0);
            Size.y = Points.Length / Size.x;
        }
    }
}
