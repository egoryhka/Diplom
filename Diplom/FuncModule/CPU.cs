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

                        if (grain.Size * MathF.Pow(DataManager.CurrentData.Settings.NmPpx, 2) >= minSize)
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

        public Mask GetStrainMaskGOS(Euler[] eulers, Grain[] grains, Vector2Int size, GpuColor lowCol, GpuColor highCol, float referenceDeviation, int opacity)
        {
            byte[] res = new byte[eulers.Length * 4];
            float[,] deviations = new float[size.x, size.y];

            foreach (Grain grain in grains)
            {
                List<Vector2> allGrainPoints = grain.Points; allGrainPoints.AddRange(grain.Edges);
                Euler sumOrient = new Euler();

                foreach (Vector2 p in allGrainPoints)
                {
                    sumOrient = Eul_sum(sumOrient, eulers[(int)(p.x + p.y * size.x)]);
                }

                Euler avgOrient = new Euler(sumOrient.x / allGrainPoints.Count, sumOrient.y / allGrainPoints.Count, sumOrient.z / allGrainPoints.Count);

                foreach (Vector2 p in allGrainPoints)
                {
                    int linearId = (int)(p.x + p.y * size.x);
                    deviations[(int)p.x, (int)p.y] = AngleBetween(avgOrient, eulers[linearId]);
                }
            }

            float maxDev = referenceDeviation;
            foreach (float dev in deviations) if (dev > maxDev) maxDev = dev;


            for (int y = 0; y < deviations.GetLength(1); y++)
            {
                for (int x = 0; x < deviations.GetLength(0); x++)
                {
                    float t = deviations[x, y] / maxDev;
                    int R = (int)(lowCol.r * (1.0f - t) + (int)(highCol.r * t));
                    int G = (int)(lowCol.g * (1.0f - t) + (int)(highCol.g * t));
                    int B = (int)(lowCol.b * (1.0f - t) + (int)(highCol.b * t));

                    //---------------
                    int linearId = (int)(x + y * size.x);

                    int outId = linearId * 4;
                    res[outId] = (byte)B; //r
                    res[outId + 1] = (byte)G; //g
                    res[outId + 2] = (byte)R; //b
                    res[outId + 3] = (byte)opacity;

                }
            }

            return new Mask() { colors = res };
        }




        private Euler Eul_sum(Euler a, Euler b) => new Euler(a.x + b.x, a.y + b.y, a.z + b.z);

        private float AngleBetween(Euler eul1, Euler eul2)
        {
            Vector3 a = new Vector3(1, 1, 1);
            Vector3 b = new Vector3(1, 1, 1);

            a = RotateVector(a, eul1);
            b = RotateVector(b, eul2);

            return Rad2Deg * (MathF.Acos(Dot(a, b) / (a.length * b.length)));
        }

        private Vector3 RotateVector(Vector3 a, Euler eul)
        {
            Vector3 angles = new Vector3(eul.x * Deg2Rad, eul.y * Deg2Rad, eul.z * Deg2Rad);
            a = new Vector3(a.x * MathF.Cos(angles.x) - a.y * MathF.Sin(angles.x), a.x * MathF.Sin(angles.x) + a.y * MathF.Cos(angles.x), a.z); // Z - rotation
            a = new Vector3(a.x * MathF.Cos(angles.y) - a.z * MathF.Sin(angles.y), a.y, -a.x * MathF.Sin(angles.y) + a.z * MathF.Cos(angles.y)); // Y - rotation
            a = new Vector3(a.x, a.y * MathF.Cos(angles.z) - a.z * MathF.Sin(angles.z), a.y * MathF.Sin(angles.z) + a.z * MathF.Cos(angles.z)); // X - rotation
            return a;
        }

        private float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

        private const float Deg2Rad = 0.0174533f;
        private const float Rad2Deg = 57.2958f;

    }
}
