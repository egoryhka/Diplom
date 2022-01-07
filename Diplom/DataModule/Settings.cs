using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Diplom.DataModule
{
    public class Settings
    {
        public Dictionary<int, string> Phases { get; set; } = new Dictionary<int, string>();

        public Color GrainBorderColor { get; set; } = Color.FromArgb(255, 255, 255, 255);

        public float MinGrainSize { get; set; } = 50;



    }

}
