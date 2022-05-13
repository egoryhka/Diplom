using Caliburn.Micro;
using Diplom.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Diplom
{
    /// <summary>
    /// Логика взаимодействия для FunctionsListWindow.xaml
    /// </summary>
    public partial class FunctionsListWindow : Window
    {
        public BindableCollection<SelectableFunction> FunctionsToSelect { get; set; } = new BindableCollection<SelectableFunction>();

        private BindableCollection<FunctionContainer> existingFuncs { get; set; }
        public FunctionsListWindow(BindableCollection<FunctionContainer> existingFunctions, IEnumerable<FunctionContainer> avaliableFunctions)
        {
            InitializeComponent();
            foreach (var f in avaliableFunctions)
            {
                FunctionsToSelect.Add(new SelectableFunction() { Function = f, IsSelected = false });
            }

            existingFuncs = existingFunctions;
        }

        private void ApplyFunctionsButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedFunctions = FunctionsToSelect.Where(x => x.IsSelected).Select(x => x.Function);
            foreach (var f in selectedFunctions)
            {
                existingFuncs.Insert(0, f);
            }
            Close();
        }

        private void DenyFunctionsButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
