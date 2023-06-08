using Diplom.DataModule;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Diplom.FuncModule
{
	public class CPU
	{
		public CPU() { }

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
						{
							if (grain.Edges != null && grain.Edges.Count > 0)
							{
								grain.AspectRatio =
								   (grain.Edges.Max(e => e.x) - grain.Edges.Min(e => e.x)) /
								   (grain.Edges.Max(e => e.y) - grain.Edges.Min(e => e.y));

								grain.ECD = SmallestEnclosingCircle.MakeCircle(grain.Edges).r * 2;

							}

							definedGrains.Add(grain);
						}

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

				if (x < size.x && x >= 0 && y < size.y && y >= 0 && grainPointsDefined[x, y] == false) //make sure we stay within bounds
				{
					int linearId = (x + y * size.x) * 4;
					if (grainMask.colors[linearId] == 0 && grainMask.colors[linearId+1] == 0 && grainMask.colors[linearId+2] == 0)
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
		}

		public Mask GetGrainSizeMask(Vector2Int size, int opacity)
		{
			byte[] res = new byte[size.x * size.y * 4];

			float minSize = DataManager.RawGrains.Min(x => x.Size);
			float maxSize = DataManager.RawGrains.Max(x => x.Size);

			foreach (var grain in DataManager.RawGrains)
			{
				float t = (grain.Size - minSize) / (maxSize - minSize);
				var col = Rainbow(t);

				foreach (var point in grain.Points)
				{
					var x = point.x;
					var y = point.y;

					int linearId = (int)(x + y * size.x);

					int outId = linearId * 4;

					res[outId] = (byte)col.z; //r
					res[outId + 1] = (byte)col.y; //g
					res[outId + 2] = (byte)col.x; //b
					res[outId + 3] = (byte)opacity;
				}

				foreach (var edge in grain.Edges)
				{
					var x = edge.x;
					var y = edge.y;

					int linearId = (int)(x + y * size.x);

					int outId = linearId * 4;

					res[outId] = (byte)col.z; //r
					res[outId + 1] = (byte)col.y; //g
					res[outId + 2] = (byte)col.x; //b
					res[outId + 3] = (byte)opacity;
				}
			}
			return new Mask() { colors = res };
		}

		public Mask GetGrainAspectRatioMask(Vector2Int size, int opacity)
		{
			byte[] res = new byte[size.x * size.y * 4];

			var maxAspectRatio = DataManager.RawGrains.Max(x => x.AspectRatio);
			var minAspectRatio = DataManager.RawGrains.Min(x => x.AspectRatio);

			foreach (var grain in DataManager.RawGrains)
			{
				float t = (grain.AspectRatio - minAspectRatio) / (maxAspectRatio - minAspectRatio);
				var col = Rainbow(t);

				foreach (var point in grain.Points)
				{
					var x = point.x;
					var y = point.y;

					int linearId = (int)(x + y * size.x);

					int outId = linearId * 4;

					res[outId] = (byte)col.z; //r
					res[outId + 1] = (byte)col.y; //g
					res[outId + 2] = (byte)col.x; //b
					res[outId + 3] = (byte)opacity;
				}

				foreach (var edge in grain.Edges)
				{
					var x = edge.x;
					var y = edge.y;

					int linearId = (int)(x + y * size.x);

					int outId = linearId * 4;

					res[outId] = (byte)col.z; //r
					res[outId + 1] = (byte)col.y; //g
					res[outId + 2] = (byte)col.x; //b
					res[outId + 3] = (byte)opacity;
				}
			}
			return new Mask() { colors = res };
		}

		public Mask GetGrainECDMask(Vector2Int size, int opacity)
		{
			byte[] res = new byte[size.x * size.y * 4];

			var maxECD = DataManager.RawGrains.Max(x => x.ECD);
			var minECD = DataManager.RawGrains.Min(x => x.ECD);

			foreach (var grain in DataManager.RawGrains)
			{
				float t = (grain.ECD - minECD) / (maxECD - minECD);
				var col = Rainbow(t);

				foreach (var point in grain.Points)
				{
					var x = point.x;
					var y = point.y;

					int linearId = (int)(x + y * size.x);

					int outId = linearId * 4;

					res[outId] = (byte)col.z; //r
					res[outId + 1] = (byte)col.y; //g
					res[outId + 2] = (byte)col.x; //b
					res[outId + 3] = (byte)opacity;
				}

				foreach (var edge in grain.Edges)
				{
					var x = edge.x;
					var y = edge.y;

					int linearId = (int)(x + y * size.x);

					int outId = linearId * 4;

					res[outId] = (byte)col.z; //r
					res[outId + 1] = (byte)col.y; //g
					res[outId + 2] = (byte)col.x; //b
					res[outId + 3] = (byte)opacity;
				}
			}
			return new Mask() { colors = res };
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
					//int R = (int)(lowCol.r * (1.0f - t) + (int)(highCol.r * t));
					//int G = (int)(lowCol.g * (1.0f - t) + (int)(highCol.g * t));
					//int B = (int)(lowCol.b * (1.0f - t) + (int)(highCol.b * t));
					var col = Rainbow(t);

					//---------------
					int linearId = (int)(x + y * size.x);

					int outId = linearId * 4;
					//res[outId] = (byte)B; //r
					//res[outId + 1] = (byte)G; //g
					//res[outId + 2] = (byte)R; //b

					res[outId] = (byte)col.z; //r
					res[outId + 1] = (byte)col.y; //g
					res[outId + 2] = (byte)col.x; //b
					res[outId + 3] = (byte)opacity;
				}
			}
			return new Mask() { colors = res };
		}


		public Mask GetStrainMaskKAM(Euler[] eulers, Vector2Int size, int opacity)
		{
			byte[] res = new byte[eulers.Length * 4];
			//float[] averages = new float[size.x * size.y];
			//float maxDev = float.MinValue;

			for (int y = 0; y < size.y; y++)
			{
				for (int x = 0; x < size.x; x++)
				{
					int id = x + y * size.x;
					int outId = id * 4;

					int idUp = id - size.x;
					int idDown = id + size.x;
					int idLeft = id - 1;
					int idRight = id + 1;

					int idUpLeft = idUp - 1;
					int idDownLeft = idDown - 1;
					int idUpRight = idUp + 1;
					int idDownRight = idDown + 1;

					float upDeviation = 0.0f;
					float downDeviation = 0.0f;
					float leftDeviation = 0.0f;
					float rightDeviation = 0.0f;

					float upLeftDeviation = 0.0f;
					float downLeftDeviation = 0.0f;
					float upRightDeviation = 0.0f;
					float downRightDeviation = 0.0f;

					float n = 0.0f;
					float maxDev = float.MinValue;

					if (y > 0) { n = n + 1.0f; upDeviation = AngleBetween(eulers[id], eulers[idUp]); if (upDeviation > maxDev) maxDev = upDeviation; }
					if (y < size.y - 1) { n = n + 1.0f; downDeviation = AngleBetween(eulers[id], eulers[idDown]); if (downDeviation > maxDev) maxDev = downDeviation; }
					if (x > 0) { n = n + 1.0f; leftDeviation = AngleBetween(eulers[id], eulers[idLeft]); if (leftDeviation > maxDev) maxDev = leftDeviation; }
					if (x < size.x - 1) { n = n + 1.0f; rightDeviation = AngleBetween(eulers[id], eulers[idRight]); if (rightDeviation > maxDev) maxDev = rightDeviation; }

					if (y > 0 && x > 0) { n = n + 1.0f; upLeftDeviation = AngleBetween(eulers[id], eulers[idUpLeft]); if (upLeftDeviation > maxDev) maxDev = upLeftDeviation; }
					if (y < size.y - 1 && x > 0) { n = n + 1.0f; downLeftDeviation = AngleBetween(eulers[id], eulers[idDownLeft]); if (downLeftDeviation > maxDev) maxDev = downLeftDeviation; }
					if (y > 0 && x < size.x - 1) { n = n + 1.0f; upRightDeviation = AngleBetween(eulers[id], eulers[idUpRight]); if (upRightDeviation > maxDev) maxDev = upRightDeviation; }
					if (y < size.y - 1 && x < size.x - 1) { n = n + 1.0f; downRightDeviation = AngleBetween(eulers[id], eulers[idDownRight]); if (downRightDeviation > maxDev) maxDev = downRightDeviation; }

					float averageDeviation =
						(upDeviation +
							downDeviation +
							leftDeviation +
							rightDeviation +
							upLeftDeviation +
							downLeftDeviation +
							upRightDeviation +
							downRightDeviation) / n;

					//averages[id] = averageDeviation;

					float t = averageDeviation / maxDev;
					var col = Rainbow(t);

					res[outId] = (byte)col.x; //r
					res[outId + 1] = (byte)col.y; //g
					res[outId + 2] = (byte)col.z; //b
					res[outId + 3] = (byte)opacity;
				}
			}

			//for (int y = 0; y < size.y; y++)
			//{
			//	for (int x = 0; x < size.x; x++)
			//	{
			//		int id = x + y * size.x;
			//		int outId = id * 4;

			//		float t = averages[id] / maxDev;
			//		var col = Rainbow(t);

			//		res[outId] = (byte)col.z; //r
			//		res[outId + 1] = (byte)col.y; //g
			//		res[outId + 2] = (byte)col.x; //b
			//		res[outId + 3] = (byte)opacity;
			//	}
			//}

			return new Mask() { colors = res };
		}

		private Vector3 Rainbow(float t)
		{

			float a = (1.0f - t) / 0.25f;   //invert and group
			int X = (int)MathF.Floor(a);   //this is the integer part
			X = Math.Clamp(X, 0, 4);
			int Y = (int)MathF.Floor(255.0f * (a - X)); //fractional part from 0 to 255

			int r = 0; int g = 0; int b = 0;

			switch (X)
			{
				case 0: r = 255; g = Y; b = 0; break;
				case 1: r = 255 - Y; g = 255; b = 0; break;
				case 2: r = 0; g = 255; b = Y; break;
				case 3: r = 0; g = 255 - Y; b = 255; break;
				case 4: r = 0; g = 0; b = 255; break;
			}

			return new Vector3(r, g, b);
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



	public sealed class SmallestEnclosingCircle
	{

		/* 
		 * Returns the smallest circle that encloses all the given points. Runs in expected O(n) time, randomized.
		 * Note: If 0 points are given, a circle of radius -1 is returned. If 1 point is given, a circle of radius 0 is returned.
		 */
		// Initially: No boundary points known
		public static Circle MakeCircle(IList<Vector2> points)
		{
			// Clone list to preserve the caller's data, do Durstenfeld shuffle
			List<Point> shuffled = points.Select(p => new Point(p.x, p.y)).ToList();
			Random rand = new Random();
			for (int i = shuffled.Count - 1; i > 0; i--)
			{
				int j = rand.Next(i + 1);
				Point temp = shuffled[i];
				shuffled[i] = shuffled[j];
				shuffled[j] = temp;
			}

			// Progressively add points to circle or recompute circle
			Circle c = Circle.INVALID;
			for (int i = 0; i < shuffled.Count; i++)
			{
				Point p = shuffled[i];
				if (c.r < 0 || !c.Contains(p))
					c = MakeCircleOnePoint(shuffled.GetRange(0, i + 1), p);
			}
			return c;
		}


		// One boundary point known
		private static Circle MakeCircleOnePoint(List<Point> points, Point p)
		{
			Circle c = new Circle(p, 0);
			for (int i = 0; i < points.Count; i++)
			{
				Point q = points[i];
				if (!c.Contains(q))
				{
					if (c.r == 0)
						c = MakeDiameter(p, q);
					else
						c = MakeCircleTwoPoints(points.GetRange(0, i + 1), p, q);
				}
			}
			return c;
		}


		// Two boundary points known
		private static Circle MakeCircleTwoPoints(List<Point> points, Point p, Point q)
		{
			Circle circ = MakeDiameter(p, q);
			Circle left = Circle.INVALID;
			Circle right = Circle.INVALID;

			// For each point not in the two-point circle
			Point pq = q.Subtract(p);
			foreach (Point r in points)
			{
				if (circ.Contains(r))
					continue;

				// Form a circumcircle and classify it on left or right side
				float cross = pq.Cross(r.Subtract(p));
				Circle c = MakeCircumcircle(p, q, r);
				if (c.r < 0)
					continue;
				else if (cross > 0 && (left.r < 0 || pq.Cross(c.c.Subtract(p)) > pq.Cross(left.c.Subtract(p))))
					left = c;
				else if (cross < 0 && (right.r < 0 || pq.Cross(c.c.Subtract(p)) < pq.Cross(right.c.Subtract(p))))
					right = c;
			}

			// Select which circle to return
			if (left.r < 0 && right.r < 0)
				return circ;
			else if (left.r < 0)
				return right;
			else if (right.r < 0)
				return left;
			else
				return left.r <= right.r ? left : right;
		}


		public static Circle MakeDiameter(Point a, Point b)
		{
			Point c = new Point((a.x + b.x) / 2, (a.y + b.y) / 2);
			return new Circle(c, Math.Max(c.Distance(a), c.Distance(b)));
		}


		public static Circle MakeCircumcircle(Point a, Point b, Point c)
		{
			// Mathematical algorithm from Wikipedia: Circumscribed circle
			float ox = (Math.Min(Math.Min(a.x, b.x), c.x) + Math.Max(Math.Max(a.x, b.x), c.x)) / 2;
			float oy = (Math.Min(Math.Min(a.y, b.y), c.y) + Math.Max(Math.Max(a.y, b.y), c.y)) / 2;
			float ax = a.x - ox, ay = a.y - oy;
			float bx = b.x - ox, by = b.y - oy;
			float cx = c.x - ox, cy = c.y - oy;
			float d = (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by)) * 2;
			if (d == 0)
				return Circle.INVALID;
			float x = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
			float y = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;
			Point p = new Point(ox + x, oy + y);
			float r = Math.Max(Math.Max(p.Distance(a), p.Distance(b)), p.Distance(c));
			return new Circle(p, r);
		}


		public struct Circle
		{

			public static readonly Circle INVALID = new Circle(new Point(0, 0), -1);

			private const double MULTIPLICATIVE_EPSILON = 1 + 1e-14;


			public Point c;   // Center
			public float r;  // Radius


			public Circle(Point c, float r)
			{
				this.c = c;
				this.r = r;
			}


			public bool Contains(Point p)
			{
				return c.Distance(p) <= r * MULTIPLICATIVE_EPSILON;
			}


			public bool Contains(ICollection<Point> ps)
			{
				foreach (Point p in ps)
				{
					if (!Contains(p))
						return false;
				}
				return true;
			}

		}



		public struct Point
		{

			public float x;
			public float y;


			public Point(float x, float y)
			{
				this.x = x;
				this.y = y;
			}


			public Point Subtract(Point p)
			{
				return new Point(x - p.x, y - p.y);
			}


			public float Distance(Point p)
			{
				float dx = x - p.x;
				float dy = y - p.y;
				return MathF.Sqrt(dx * dx + dy * dy);
			}


			// Signed area / determinant thing
			public float Cross(Point p)
			{
				return x * p.y - y * p.x;
			}

		}
	}




}