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
        public List<Phase> Phases { get; set; } = new List<Phase>();
        public Color GrainSelectBorderColor { get; set; } = Color.FromArgb(255, 255, 255, 255);
        public Color GrainsBorderColor { get; set; } = Color.FromArgb(180, 0, 0, 0);
        public float MinGrainSize { get; set; } = 0.05f;
        public float NmPpx { get; set; } = 0.0f;
        public bool AutoUpdate { get; set; } = true;
    }

}
