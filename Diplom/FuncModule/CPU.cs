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

        public static int CountUnsolved(Euler[] eulers)
        {
            return eulers.Count(x => x.x == 0 && x.y == 0 && x.z == 0);
        }
    }
}
