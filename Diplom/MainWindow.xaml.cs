using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Diplom.DataModule;
using Microsoft.Win32;

namespace Diplom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Сохранить изменения?", "Выход", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                try
                {
                    DataManager.SaveCurrent();
                }
                catch (FileNotFoundException)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "diplomData files (*.diplomData)|*.diplomData";
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string pathToFile = saveFileDialog.FileName;
                        DataManager.Save(pathToFile);
                    }
                }
            }
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "diplomData files (*.diplomData)|*.diplomData";
            if (saveFileDialog.ShowDialog() == true)
            {
                string pathToFile = saveFileDialog.FileName;
                DataManager.Save(pathToFile);
            }
        }

        private void SaveCurrentFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataManager.SaveCurrent();
            }
            catch (FileNotFoundException)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "diplomData files (*.diplomData)|*.diplomData";
                if (saveFileDialog.ShowDialog() == true)
                {
                    string pathToFile = saveFileDialog.FileName;
                    DataManager.Save(pathToFile);
                }
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "diplomData files (*.diplomData)|*.diplomData";
            if (openFileDialog.ShowDialog() == true)
            {
                string pathToFile = openFileDialog.FileName;
                DataManager.Load(pathToFile);
            }
        }

        private void ExcelImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Excel files (*.xslx)|*.xlsx";
                if (openFileDialog.ShowDialog() == true)
                {
                    string pathToFile = openFileDialog.FileName;
                    DataManager.LoadEbsdFromExcel(pathToFile);
                }
            }
            catch (ExcelNotValidException)
            {
                MessageBox.Show("Выбранный файл не подходит!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().Show();
        }

    }
}
