using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private static readonly Regex _numericOnlyRegex = new Regex("[^0-9.-]+");

        public Settings _settings { get; set; } = new Settings();

        public SettingsWindow()
        {
            LoadCurrentSettings();

            InitializeComponent();

            TextBox_A.PreviewTextInput += (object sender, TextCompositionEventArgs e) => { e.Handled = !CheckNumeric(e.Text); };

        }


        private void ApplySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            DataManager.CurrentData.settings = _settings;
            Close();
        }

        private void DenySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadCurrentSettings()
        {
            var currentSettings = DataManager.CurrentData.settings;
            _settings.A = currentSettings.A;



        }

        private static bool CheckNumeric(string text)
        {
            return !_numericOnlyRegex.IsMatch(text);
        }
    }
}
