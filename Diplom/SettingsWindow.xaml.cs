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
using Caliburn.Micro;

namespace Diplom
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static readonly Regex _regexFloat = new Regex("[^0-9.-]+"); //regex that matches disallowed text

        public Settings _settings { get; set; } = new Settings();

        public BindableCollection<Phase> _phases { get; set; } = new BindableCollection<Phase>();

        public SettingsWindow()
        {
            LoadCurrentSettings();
            InitializeComponent();

            //foreach (var phase in _settings.Phases)
            //{
            //    CreatePhaseNameEditor(phase);
            //}

        }


        private void ApplySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            DataManager.CurrentData.Settings = _settings;
            DataManager.CurrentData.Settings.Phases = _phases.ToList();
            Close();
        }

        private void DenySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadCurrentSettings()
        {
            var currentSettings = DataManager.CurrentData.Settings;

            _phases.AddRange(currentSettings.Phases);

            _settings.GrainsBorderColor = currentSettings.GrainsBorderColor;
            _settings.GrainSelectBorderColor = currentSettings.GrainSelectBorderColor;

            _settings.MinGrainSize = currentSettings.MinGrainSize;
            _settings.NmPpx = currentSettings.NmPpx;
            _settings.AutoUpdate = currentSettings.AutoUpdate;

        }

        //private void CreatePhaseNameEditor(KeyValuePair<int, string> phase)
        //{
        //    DockPanel EditorPanel = new DockPanel() { LastChildFill = true };

        //    TextBlock phaseNumberText = new TextBlock();
        //    phaseNumberText.Text = phase.Key.ToString();
        //    phaseNumberText.Width = 20;
        //    phaseNumberText.Margin = new Thickness(5);
        //    phaseNumberText.VerticalAlignment = VerticalAlignment.Center;
        //    DockPanel.SetDock(phaseNumberText, Dock.Left);

        //    TextBox phaseNameTextBox = new TextBox();
        //    phaseNameTextBox.Height = 25;
        //    phaseNameTextBox.Width = Double.NaN;

        //    Binding binding = new Binding()
        //    {
        //        Source = _settings,
        //        Path = new PropertyPath($"Phases[{phase.Key}]"),
        //        Mode = BindingMode.TwoWay,
        //        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        //    };

        //    phaseNameTextBox.SetBinding(TextBox.TextProperty, binding);

        //    EditorPanel.Children.Add(phaseNumberText);
        //    EditorPanel.Children.Add(phaseNameTextBox);

        //    PhasesStackPanel.Children.Add(EditorPanel);

        //}


        private static bool IsTextAllowedFloat(string text) => !_regexFloat.IsMatch(text);

        private void NumericTextboxFloat_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
    e.Handled = !IsTextAllowedFloat(e.Text);

    }
}
