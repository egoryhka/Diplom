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
using Diplom.DataModule;

namespace Diplom
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private Settings _settings = new Settings();

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void ApplySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            //Apply
            DataManager.CurrentData.settings = _settings;
            Close();
        }

        private void DenySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //DataManager.CurrentData


    }
}
