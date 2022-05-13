using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Diplom.DataModule;

namespace Diplom.FuncModule
{
    public class CPU
    {
        public CPU()
        {

        }

        public static int CountUnsolved(Euler[] eulers) => eulers.Count(x => x.x == 0 && x.y == 0 && x.z == 0);

        public Grain[] DefineGrains(Mask grainMask, float minSize)
        {
            List<Grain> definedGrains = new List<Grain>();
            Vector2Int size = DataManager.CurrentData.Size;

            bool[,] grainPointsDefined = new bool[size.x, size.y];

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {

                    int id_2D = (x + y * size.x) * 4;
                    if (grainMask.colors[id_2D] == 0 && !grainPointsDefined[x, y])
                    {
                        Grain grain = new Grain() { Edges = new List<Vector2>(), Points = new List<Vector2>() };
                        FloodFillGrain(new Vector2(x, y), ref grain, ref grainPointsDefined, grainMask);

                        if (grain.Size * MathF.Pow(DataManager.CurrentData.Settings.NmPpx, 2) >= minSize )
                            definedGrains.Add(grain);
                    }
                }
            }

            return definedGrains.ToArray();
        }


        private void FloodFillGrain(Vector2 pt, ref Grain grain, ref bool[,] grainPointsDefined, Mask grainMask)
        {
            Vector2Int size = DataManager.CurrentData.Size;

            Stack<Vector2> pixels = new Stack<Vector2>();
            pixels.Push(pt);

            //if (pt.x < size.x && pt.x >= 0 && pt.y < size.y && pt.y >= 0)

            while (pixels.Count > 0)
            {
                Vector2 a = pixels.Pop();
                int x = (int)a.x;
                int y = (int)a.y;

                if (x < size.x && x >= 0 && y < size.y && y >= 0 && grainPointsDefined[x, y] == false)//make sure we stay within bounds
                {
                    int linearId = (x + y * size.x) * 4;
                    if (grainMask.colors[linearId] == 0)
                    {
                        grain.Points.Add(a);
                        grainPointsDefined[x, y] = true;
                        pixels.Push(new Vector2(x - 1, y));
                        pixels.Push(new Vector2(x + 1, y));
                        pixels.Push(new Vector2(x, y - 1));
                        pixels.Push(new Vector2(x, y + 1));
                    }
                    else
                    {
                        grain.Edges.Add(a);
                    }
                }
            }

            return;
        }




    }
}
