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

        private void BuildProgramm()
        {
            ComputeContextPropertyList Properties = new ComputeContextPropertyList(ComputePlatform.Platforms[0]);
            Context = new ComputeContext(ComputeDeviceTypes.All, Properties, null, IntPtr.Zero);

            List<ComputeDevice> Devices = new List<ComputeDevice>();
            Devices.Add(ComputePlatform.Platforms[0].Devices[0]);

            try
            {
                string gpuCode = File.ReadAllText(Directory.GetCurrentDirectory() + @"\GpuCode.c");

                Program = new ComputeProgram(Context, gpuCode);
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

            byte[] outColors = new byte[size.x * size.y * 4]; // output
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
            
            byte[] outColors = new byte[size.x * size.y * 4]; // output
            CommandQueue.ReadFromBuffer(outColorBuffer, ref outColors, true, null);

            kernel.Dispose(); outColorBuffer.Dispose(); eulerBuffer.Dispose();

            return outColors;
        }

        public byte[] ApplyMask(byte[] inputColors, Mask mask)
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
            kernel.SetMemoryArgument(2, outputBuffer);

            CommandQueue.Execute(kernel, null, new long[] { inputColors.Length }, null, null);

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

            CommandQueue.Execute(kernel, null, new long[] { eulers.Length }, null, null);

            byte[] res = new byte[eulers.Length * 4];
            CommandQueue.ReadFromBuffer(outputBuffer, ref res, true, null);

            inputBuffer.Dispose(); outputBuffer.Dispose(); kernel.Dispose();

            return new Mask() { colors = res };
        }
    }

    public class Mask { public byte[] colors; }

    public class ProgramBuildException : Exception { }
}
