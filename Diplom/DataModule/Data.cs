using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplom.DataModule
{
    [Serializable]
    public class Data
    {
        public Settings Settings = new Settings();
        public EBSD[] Points;
    }
}
