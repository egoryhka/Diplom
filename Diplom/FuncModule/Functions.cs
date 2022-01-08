using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplom.FuncModule
{
    public class Functions
    {
        public readonly GPU GPU;
        public readonly CPU CPU;

        public Functions()
        {
            GPU = new GPU();
            CPU = new CPU();
        }


    }
}
