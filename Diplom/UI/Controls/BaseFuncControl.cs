using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Diplom.UI.Controls
{
    public delegate void OnClose();
    public delegate void OnMinimize();
    public delegate void OnMaximize();
    public delegate void OnWork();

    public class BaseFuncControl : UserControl
    {
        public OnClose OnClose;

    }
}
