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


    }

    public class ProgramBuildException : Exception { }
}
