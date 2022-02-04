using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Diplom.FuncModule;
using Microsoft.Win32;

namespace Diplom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Functions functions;

        private Vector2Int GetPixelCoordinate(Image image, MouseEventArgs e)
        {
            Point pt = e.GetPosition(image);
            pt.X *= image.Source.Width / image.ActualWidth;
            pt.Y *= image.Source.Height / image.ActualHeight;
            return new Vector2Int((int)pt.X, (int)pt.Y);
        }

        public MainWindow()
        {
            InitializeComponent();

            Closing += MainWindow_Closing;

            try
            {
                functions = new Functions();
            }
            catch (ProgramBuildException)
            {
                MessageBox.Show("Ошибка сборки!\nВозможно файл 'GpuCode.c'\nотсутствует или поврежден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Closing -= MainWindow_Closing;
                Close();
            }

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

        private void SaveFile()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "diplomData files (*.diplomData)|*.diplomData";
            if (saveFileDialog.ShowDialog() == true)
            {
                string pathToFile = saveFileDialog.FileName;
                DataManager.Save(pathToFile);
                Title = DataManager.ProjectName;
            }
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void SaveCurrentFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataManager.SaveCurrent();
            }
            catch (FileNotFoundException)
            {
                SaveFile();
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "diplomData files (*.diplomData)|*.diplomData";
            if (openFileDialog.ShowDialog() == true)
            {
                string pathToFile = openFileDialog.FileName;
                try
                {
                    DataManager.Load(pathToFile);
                }
                catch (JsonLoadException)
                {
                    MessageBox.Show("Ошибка чтения файла проекта\nФайл поврежден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Title = DataManager.ProjectName;
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
            catch (ReadExcelException)
            {
                MessageBox.Show("Ошибка чтения файла!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
        }

        private void ResetImageSize_Click(object sender, RoutedEventArgs e)
        {
            MainImageContainer.Reset();

            //if (DataManager.CurrentData.BC == null) return;
            //byte[] colors = functions.GPU.GetColorMapBC(DataManager.CurrentData.BC, DataManager.CurrentData.Size);
            //Mask mask = functions.GPU.GetGrainMask(DataManager.CurrentData.Eulers, DataManager.CurrentData.Size, 5, new GpuColor(0, 0, 0, 0));
            //byte[] maskedColors = functions.GPU.ApplyMask(colors, mask);
            //var bmp = functions.BitmapFunc.ByteArrayToBitmap(DataManager.CurrentData.Size, maskedColors);
            //MainImage.Source = functions.BitmapFunc.CreateBitmapSource(bmp);


            if (DataManager.CurrentData.Eulers == null) return;
            var color = DataManager.CurrentData.Settings.GrainSelectBorderColor;
            //DataManager.CurrentData.Eulers = functions.GPU.StandartCleanUp(DataManager.CurrentData.Eulers, DataManager.CurrentData.Size, 1);

            DataManager.CurrentData.Eulers = functions.GPU.AutomaticCleanUp(DataManager.CurrentData.Eulers, DataManager.CurrentData.Size, 10);
            Mask mask = functions.GPU.GetGrainMask(DataManager.CurrentData.Eulers, DataManager.CurrentData.Size, DataManager.CurrentData.Settings.MinGrainSize, new GpuColor(color.R, color.G, color.B, color.A));
            byte[] colors = functions.GPU.GetColorMapEuler(DataManager.CurrentData.Eulers, DataManager.CurrentData.Size);
            byte[] maskedColors = functions.GPU.ApplyMask(colors, mask, DataManager.CurrentData.Size);
            var bmp = functions.BitmapFunc.ByteArrayToBitmap(DataManager.CurrentData.Size, colors);

            MainImage.Source = functions.BitmapFunc.CreateBitmapSource(bmp);

        }

        private void MainImage_MouseMove(object sender, MouseEventArgs e)
        {
            Vector2Int coords = GetPixelCoordinate(MainImage, e);
            X.Content = coords.x;
            Y.Content = coords.y;
        }
    }
}
