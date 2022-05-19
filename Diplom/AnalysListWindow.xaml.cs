using System;
using Caliburn.Micro;
using Diplom.UI;
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
    /// Логика взаимодействия для AnalysListWindow.xaml
    /// </summary>
    public partial class AnalysListWindow : Window
    {
        public BindableCollection<SelectableAnalys> AnalysToSelect { get; set; } = new BindableCollection<SelectableAnalys>();

        private BindableCollection<AnalysContainer> existingAnalys { get; set; }
        public AnalysListWindow(BindableCollection<AnalysContainer> _existingAnalys, IEnumerable<AnalysContainer> avaliableAnalys)
        {
            InitializeComponent();
            foreach (var analys in avaliableAnalys)
            {
                AnalysToSelect.Add(new SelectableAnalys() { Analys = analys, IsSelected = false });
            }
            existingAnalys = _existingAnalys;
        }

        private void ApplyAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            existingAnalys.AddRange(AnalysToSelect.Where(x => x.IsSelected).Select(x => x.Analys));
            Close();
        }

        private void DenyAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
