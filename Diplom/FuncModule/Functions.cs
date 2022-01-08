using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplom.FuncModule
{
    public static class Functions
    {
        public static readonly GPU GPU;
        public static readonly CPU CPU;

        static Functions()
        {
            GPU = new GPU();
            CPU = new CPU();
        }
      

    }
}
