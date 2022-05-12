using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloo;
using Diplom.DataModule;

namespace Diplom.FuncModule
{
    public class GPU
    {
        private ComputeContext Context;
        private ComputeProgram Program;

        private ComputeCommandQueue CommandQueue;

        public GPU()
        {
            BuildProgramm();
        }

        private const string reserveGpuCode = @"
struct Euler
{
	float x;
	float y;
	float z;
}; typedef struct Euler euler;

bool isUnSolved(euler e) {
	return e.x == 0 && e.y == 0 && e.z == 0;
}

euler eul_sum(euler a, euler b) {
	return (euler) { a.x + b.x, a.y + b.y, a.z + b.z };
}

euler eul_subtract(euler a, euler b) {
	return (euler) { a.x - b.x, a.y - b.y, a.z - b.z };
}

int4 eulerToColor(euler eul) {
	return convert_int4((float4)(255.0f * eul.x / 360.0f, 255.0f * eul.y / 90.0f, 255.0f * eul.z / 90.0f, 0));
}

euler averageEuler(euler e1, euler e2, euler e3)
{
	euler sum = eul_sum(e3, eul_sum(e1, e2));
	euler av = (euler){ sum.x / 3.0, sum.y / 3.0, sum.z / 3.0 };
	return av;
}

float brightness(int4 col) {
	return 0.2126 * col.x + 0.7152 * col.y + 0.0722 * col.z;
}

float deviation(euler e1, euler e2, euler e3, euler M)
{
	int4 mCol = eulerToColor(M);
	int4 e1Col = eulerToColor(e1);
	int4 e2Col = eulerToColor(e2);
	int4 e3Col = eulerToColor(e3);
	float _e1 = (float)(brightness(e1Col) - brightness(mCol)) * (brightness(e1Col) - brightness(mCol));
	float _e2 = (float)(brightness(e2Col) - brightness(mCol)) * (brightness(e2Col) - brightness(mCol));
	float _e3 = (float)(brightness(e3Col) - brightness(mCol)) * (brightness(e3Col) - brightness(mCol));
	return (_e1 + _e2 + _e3) / 3.0;
}


float3 rotateVector(float3 a, euler eul)
{
	float3 angles = (float3)(radians(eul.x), radians(eul.y), radians(eul.z));
	a = (float3)(a.x * cos(angles.x) - a.y * sin(angles.x), a.x * sin(angles.x) + a.y * cos(angles.x), a.z); // Z - rotation
	a = (float3)(a.x * cos(angles.y) - a.z * sin(angles.y), a.y, -a.x * sin(angles.y) + a.z * cos(angles.y)); // Y - rotation
	a = (float3)(a.x, a.y * cos(angles.z) - a.z * sin(angles.z), a.y * sin(angles.z) + a.z * cos(angles.z)); // X - rotation
	return a;
}

float angleBetween(euler eul1, euler eul2) {

	float3 a = (float3)(1, 1, 1);
	float3 b = (float3)(1, 1, 1);

	a = rotateVector(a, eul1);
	b = rotateVector(b, eul2);

	return degrees(acos(dot(a, b) / (length(a) * length(b))));
}


__kernel void Euler2Color(__global euler* in, int width, int height, __global uchar* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);

	int inlinearId = (int)(x + y * width);
	euler eul = in[inlinearId];

	int4 col = eulerToColor(eul);

	int outlinearId = (int)((x + y * width) * 4);
	out[outlinearId] = col.z; // R
	out[outlinearId + 1] = col.y; // G
	out[outlinearId + 2] = col.x; // B
	out[outlinearId + 3] = 255; // A
}

__kernel void Bc2Color(__global int* in, int width, int height, __global uchar* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);

	int inlinearId = (int)(x + y * width);
	int BC = in[inlinearId];

	int4 col = (int4)(BC, BC, BC, 0);

	int outlinearId = (int)((x + y * width) * 4);
	out[outlinearId] = col.z; // R
	out[outlinearId + 1] = col.y; // G
	out[outlinearId + 2] = col.x; // B
	out[outlinearId + 3] = 255; // A
}

__kernel void ApplyMask(__global uchar* in, __global uchar* mask, int width, int height, __global uchar* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);

	int id = (int)(x + y * width) * 4;

	if (mask[id + 3] > 0 && mask[id + 3] < 255) {

		int R = convert_int(in[id] * (1.0f - (mask[id + 3] / 255.0f))) + convert_int(mask[id] * (mask[id + 3] / 255.0f));
		int G = convert_int(in[id + 1] * (1.0f - (mask[id + 3] / 255.0f))) + convert_int(mask[id + 1] * (mask[id + 3] / 255.0f));
		int B = convert_int(in[id + 2] * (1.0f - (mask[id + 3] / 255.0f))) + convert_int(mask[id + 2] * (mask[id + 3] / 255.0f));

		out[id] = R;
		out[id + 1] = G;
		out[id + 2] = B;
		out[id + 3] = 255;
	}
	else {
		if (mask[id] == 0 && mask[id + 1] == 0 && mask[id + 2] == 0 && mask[id + 3] == 0) {
			out[id] = in[id];
			out[id + 1] = in[id + 1];
			out[id + 2] = in[id + 2];
			out[id + 3] = in[id + 3];
		}
		else {
			out[id] = convert_int(mask[id]);
			out[id + 1] = convert_int(mask[id + 1]);
			out[id + 2] = convert_int(mask[id + 2]);
			out[id + 3] = convert_int(mask[id + 3]);
		}
	}
}


__kernel void GetGrainMask(__global euler* in, int width, int height, float MissOrientationTreshold, float4 grainMaskColor, __global uchar* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);
	int id = (int)(x + y * width);

	int idUp = (int)(x + (y + 1) * width);
	int idDown = (int)(x + (y - 1) * width);
	int idLeft = (int)((x - 1) + y * width);
	int idRight = (int)((x + 1) + y * width);

	bool isEdge = false;

	if (!isEdge && y > 0) { if (angleBetween(in[id], in[idUp]) > MissOrientationTreshold) isEdge = true; }
	if (!isEdge && y < height) { if (angleBetween(in[id], in[idDown]) > MissOrientationTreshold) isEdge = true; }
	if (!isEdge && x > 0) { if (angleBetween(in[id], in[idLeft]) > MissOrientationTreshold) isEdge = true; }
	if (!isEdge && x < width) { if (angleBetween(in[id], in[idRight]) > MissOrientationTreshold) isEdge = true; }

	int outId = id * 4;
	if (isEdge) {
		out[outId] = convert_int(grainMaskColor.z);
		out[outId + 1] = convert_int(grainMaskColor.y);
		out[outId + 2] = convert_int(grainMaskColor.x);
		out[outId + 3] = convert_int(grainMaskColor.w);
	}
}


__kernel void StandartCleanUp(__global euler* in, int width, int height, __global euler* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);
	int id = (int)(x + y * width);

	int idUp = (int)(x + (y + 1) * width);
	int idDown = (int)(x + (y - 1) * width);
	int idLeft = (int)((x - 1) + y * width);
	int idRight = (int)((x + 1) + y * width);

	int idUpLeft = (int)((x - 1) + (y + 1) * width);
	int idDownLeft = (int)((x - 1) + (y - 1) * width);
	int idUpRight = (int)((x + 1) + (y + 1) * width);
	int idDownRight = (int)((x + 1) + (y - 1) * width);

	euler eul = in[id];
	if (eul.x != 0 || eul.y != 0 || eul.z != 0) out[id] = eul;
	else {

		euler sum = (euler){ 0,0,0 };

		//Standart ----------------
		int k = 0;

		if (in[idUp].x != 0 || in[idUp].y != 0 || in[idUp].z != 0) {
			sum = eul_sum(sum, in[idUp]);
			k++;
		}
		if (in[idDown].x != 0 || in[idDown].y != 0 || in[idDown].z != 0) {
			sum = eul_sum(sum, in[idDown]);
			k++;
		}
		if (in[idLeft].x != 0 || in[idLeft].y != 0 || in[idLeft].z != 0) {
			sum = eul_sum(sum, in[idLeft]);
			k++;
		}
		if (in[idRight].x != 0 || in[idRight].y != 0 || in[idRight].z != 0) {
			sum = eul_sum(sum, in[idRight]);
			k++;
		}

		if (in[idUpLeft].x != 0 || in[idUpLeft].y != 0 || in[idUpLeft].z != 0) {
			sum = eul_sum(sum, in[idUpLeft]);
			k++;
		}
		if (in[idDownLeft].x != 0 || in[idDownLeft].y != 0 || in[idDownLeft].z != 0) {
			sum = eul_sum(sum, in[idDownLeft]);
			k++;
		}
		if (in[idUpRight].x != 0 || in[idUpRight].y != 0 || in[idUpRight].z != 0) {
			sum = eul_sum(sum, in[idUpRight]);
			k++;
		}
		if (in[idDownRight].x != 0 || in[idDownRight].y != 0 || in[idDownRight].z != 0) {
			sum = eul_sum(sum, in[idDownRight]);
			k++;
		}

		if (k > 0) {
			out[id] = (euler){ sum.x / k, sum.y / k, sum.z / k };
		}

	}
}


__kernel void KuwaharaCleanUp(__global euler* in, int width, int height, __global euler* out)
{
	int x = get_global_id(0);
	int y = get_global_id(1);
	int id = (int)(x + y * width);

	int idUp = (int)(x + (y + 1) * width);
	int idDown = (int)(x + (y - 1) * width);
	int idLeft = (int)((x - 1) + y * width);
	int idRight = (int)((x + 1) + y * width);

	int idUpLeft = (int)((x - 1) + (y + 1) * width);
	int idDownLeft = (int)((x - 1) + (y - 1) * width);
	int idUpRight = (int)((x + 1) + (y + 1) * width);
	int idDownRight = (int)((x + 1) + (y - 1) * width);

	euler eul = in[id];
	if (eul.x != 0 || eul.y != 0 || eul.z != 0) out[id] = eul;
	else {

		euler average = (euler){ 0,0,0 };

		//Kuwahara ----------------
		float minD = 10000;

		if (x > 0 && y > 0 && !isUnSolved(in[idUp]) && !isUnSolved(in[idLeft]) && !isUnSolved(in[idUpLeft])) {
			euler M1 = averageEuler(in[idUp], in[idLeft], in[idUpLeft]);
			float D1 = deviation(in[idUp], in[idLeft], in[idUpLeft], M1);
			if (D1 < minD) {
				minD = D1;
				average = M1;
			}
		}
		if (x < width && y > 0 && !isUnSolved(in[idUp]) && !isUnSolved(in[idRight]) && !isUnSolved(in[idUpRight])) {
			euler M2 = averageEuler(in[idUp], in[idRight], in[idUpRight]);
			float D2 = deviation(in[idUp], in[idRight], in[idUpRight], M2);
			if (D2 < minD) {
				minD = D2;
				average = M2;
			}
		}
		if (x < width && y < height && !isUnSolved(in[idDown]) && !isUnSolved(in[idRight]) && !isUnSolved(in[idDownRight])) {
			euler M3 = averageEuler(in[idDown], in[idRight], in[idDownRight]);
			float D3 = deviation(in[idDown], in[idRight], in[idDownRight], M3);
			if (D3 < minD) {
				minD = D3;
				average = M3;
			}
		}
		if (x > 0 && y < height && !isUnSolved(in[idDown]) && !isUnSolved(in[idLeft]) && !isUnSolved(in[idDownLeft])) {
			euler M4 = averageEuler(in[idDown], in[idLeft], in[idDownLeft]);
			float D4 = deviation(in[idDown], in[idLeft], in[idDownLeft], M4);
			if (D4 < minD) {
				minD = D4;
				average = M4;
			}
		}

		out[id] = average;
	}
}


";

        private void BuildProgramm()
        {
            ComputeContextPropertyList Properties = new ComputeContextPropertyList(ComputePlatform.Platforms[0]);
            Context = new ComputeContext(ComputeDeviceTypes.All, Properties, null, IntPtr.Zero);

            List<ComputeDevice> Devices = new List<ComputeDevice>();
            Devices.AddRange(ComputePlatform.Platforms[0].Devices);


            try
            {
               // string gpuCode = File.ReadAllText(Directory.GetCurrentDirectory() + @"\GpuCode.c");

                Program = new ComputeProgram(Context, /*gpuCode*/reserveGpuCode);
                Program.Build(Devices, "", null, IntPtr.Zero);
            }
            catch { throw new ProgramBuildException(); }

            CommandQueue = new ComputeCommandQueue(Context, Devices[0], ComputeCommandQueueFlags.None);
        }

        public byte[] GetColorMapBC(int[] bc, Vector2Int size)
        {
            ComputeKernel kernel = Program.CreateKernel("Bc2Color");
            if (kernel == null) return null;

            ComputeBuffer<byte> outColorBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadWrite, size.x * size.y * 4);
            ComputeBuffer<int> bcBuffer = new ComputeBuffer<int>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, bc);

            kernel.SetMemoryArgument(0, bcBuffer);
            kernel.SetValueArgument(1, size.x);
            kernel.SetValueArgument(2, size.y);
            kernel.SetMemoryArgument(3, outColorBuffer);

            CommandQueue.Execute(kernel, null, new long[] { size.x, size.y }, null, null);

            byte[] outColors = new byte[outColorBuffer.Count]; // output
            CommandQueue.ReadFromBuffer(outColorBuffer, ref outColors, true, null);

            kernel.Dispose(); outColorBuffer.Dispose(); bcBuffer.Dispose();

            return outColors;
        }

        public byte[] GetColorMapEuler(Euler[] eulers, Vector2Int size)
        {
            ComputeKernel kernel = Program.CreateKernel("Euler2Color");
            if (kernel == null) return null;

            ComputeBuffer<byte> outColorBuffer = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadWrite, size.x * size.y * 4);
            ComputeBuffer<Euler> eulerBuffer = new ComputeBuffer<Euler>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, eulers);

            kernel.SetMemoryArgument(0, eulerBuffer);
            kernel.SetValueArgument(1, size.x);
            kernel.SetValueArgument(2, size.y);
            kernel.SetMemoryArgument(3, outColorBuffer);

            CommandQueue.Execute(kernel, null, new long[] { size.x, size.y }, null, null);

            byte[] outColors = new byte[outColorBuffer.Count]; // output
            CommandQueue.ReadFromBuffer(outColorBuffer, ref outColors, true, null);

            kernel.Dispose(); outColorBuffer.Dispose(); eulerBuffer.Dispose();

            return outColors;
        }

        public byte[] ApplyMask(byte[] inputColors, Mask mask, Vector2Int size)
        {
            ComputeKernel kernel = Program.CreateKernel("ApplyMask");

            ComputeBuffer<byte> inputBuffer
                = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, inputColors);
            ComputeBuffer<byte> maskBuffer
                = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, mask.colors);
            ComputeBuffer<byte> outputBuffer
                = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, new byte[inputColors.Length]);

            kernel.SetMemoryArgument(0, inputBuffer);
            kernel.SetMemoryArgument(1, maskBuffer);
            kernel.SetValueArgument(2, size.x);
            kernel.SetValueArgument(3, size.y);
            kernel.SetMemoryArgument(4, outputBuffer);

            CommandQueue.Execute(kernel, null, new long[] { /*inputColors.Length*/size.x, size.y }, null, null);

            byte[] res = new byte[inputColors.Length];
            CommandQueue.ReadFromBuffer(outputBuffer, ref res, true, null);

            inputBuffer.Dispose(); maskBuffer.Dispose(); outputBuffer.Dispose(); kernel.Dispose();

            return res;
        }

        public Mask GetGrainMask(Euler[] eulers, Vector2Int size, float treshold, GpuColor grainMaskColor)
        {
            ComputeKernel kernel = Program.CreateKernel("GetGrainMask");

            ComputeBuffer<Euler> inputBuffer
                = new ComputeBuffer<Euler>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, eulers);
            ComputeBuffer<byte> outputBuffer
                = new ComputeBuffer<byte>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, new byte[eulers.Length * 4]);

            kernel.SetMemoryArgument(0, inputBuffer);
            kernel.SetValueArgument(1, size.x);
            kernel.SetValueArgument(2, size.y);
            kernel.SetValueArgument(3, treshold);
            kernel.SetValueArgument(4, grainMaskColor);
            kernel.SetMemoryArgument(5, outputBuffer);

            CommandQueue.Execute(kernel, null, new long[] { size.x, size.y }, null, null);

            byte[] res = new byte[eulers.Length * 4];
            CommandQueue.ReadFromBuffer(outputBuffer, ref res, true, null);

            inputBuffer.Dispose(); outputBuffer.Dispose(); kernel.Dispose();

            return new Mask() { colors = res };
        }

        public/* Euler[]*/ void AutomaticCleanUp(Euler[] eulers, Vector2Int size, int maxIterations, ref Euler[] target)
        {
            int i = 0;
            int unsolvedCount = CPU.CountUnsolved(eulers);
            while (i++ < maxIterations)
            {
                if (unsolvedCount == 0) return /*eulers*/;

                /*eulers = */ KuwaharaCleanUp(eulers, size, 1, ref target);

                int newUnsolvedCount = CPU.CountUnsolved(eulers);

                if (newUnsolvedCount == unsolvedCount) break;
                unsolvedCount = newUnsolvedCount;
            }

            while (unsolvedCount > 0)
            {
                /*eulers = */ StandartCleanUp(eulers, size, 1, ref target);
                unsolvedCount = CPU.CountUnsolved(eulers);
            }

            //return eulers;
        }

        public /*Euler[]*/ void StandartCleanUp(Euler[] eulers, Vector2Int size, int iterations, ref Euler[] target)
        {
            ComputeKernel kernel = Program.CreateKernel("StandartCleanUp");

            ComputeBuffer<Euler> inputBuffer
                = new ComputeBuffer<Euler>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, eulers);
            ComputeBuffer<Euler> outputBuffer
                = new ComputeBuffer<Euler>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, new Euler[eulers.Length]);

            kernel.SetMemoryArgument(0, inputBuffer);
            kernel.SetValueArgument(1, size.x);
            kernel.SetValueArgument(2, size.y);
            kernel.SetMemoryArgument(3, outputBuffer);

            CommandQueue.Execute(kernel, null, new long[] { size.x, size.y }, null, null);

            for (int i = 0; i < iterations - 1; i++)
            {
                CommandQueue.CopyBuffer(outputBuffer, inputBuffer, null);
                CommandQueue.Execute(kernel, null, new long[] { size.x, size.y }, null, null);
            }

            //Euler[] res = new Euler[eulers.Length];
            CommandQueue.ReadFromBuffer(outputBuffer, ref /*res*/ target, true, null);

            inputBuffer.Dispose(); outputBuffer.Dispose(); kernel.Dispose();

            //return res;
        }

        public/* Euler[]*/ void KuwaharaCleanUp(Euler[] eulers, Vector2Int size, int iterations, ref Euler[] target)
        {
            ComputeKernel kernel = Program.CreateKernel("KuwaharaCleanUp");

            ComputeBuffer<Euler> inputBuffer
                = new ComputeBuffer<Euler>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, eulers);
            ComputeBuffer<Euler> outputBuffer
                = new ComputeBuffer<Euler>(Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, new Euler[eulers.Length]);

            kernel.SetMemoryArgument(0, inputBuffer);
            kernel.SetValueArgument(1, size.x);
            kernel.SetValueArgument(2, size.y);
            kernel.SetMemoryArgument(3, outputBuffer);

            CommandQueue.Execute(kernel, null, new long[] { size.x, size.y }, null, null);

            for (int i = 0; i < iterations - 1; i++)
            {
                CommandQueue.CopyBuffer(outputBuffer, inputBuffer, null);
                CommandQueue.Execute(kernel, null, new long[] { size.x, size.y }, null, null);
            }

            //Euler[] res = new Euler[eulers.Length];
            CommandQueue.ReadFromBuffer(outputBuffer, ref /*res*/target, true, null);

            inputBuffer.Dispose(); outputBuffer.Dispose(); kernel.Dispose();

            //return res;
        }

    }

    public class Mask { public byte[] colors; }

    public class ProgramBuildException : Exception { }
}
