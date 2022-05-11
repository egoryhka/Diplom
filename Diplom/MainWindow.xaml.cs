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
using Caliburn.Micro;
using Diplom.DataModule;
using Diplom.FuncModule;
using Diplom.UI;
using Microsoft.Win32;

namespace Diplom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Functions functions;

        public List<FunctionContainer> AllFunctions { get; set; } = new List<FunctionContainer>();

        public BindableCollection<FunctionContainer> Functions { get; set; } = new BindableCollection<FunctionContainer>();

        private byte[] colors = new byte[0];
        private Mask grainMask = new Mask();
        private byte[] maskedColors = new byte[0];
        private Euler[] bufferEulers = new Euler[0];
        private Euler[] rawEulers = new Euler[0];

        private bool someParameterChanged = false;
        //-------------------------------------------

        private void InitializeFunctions()
        {
            var moveFuncUP = new MoveFuncCommand(f =>
            {
                if (Functions.Count < 2) return;
                FunctionContainer func = f as FunctionContainer;
                if (func != null)
                {
                    int indexOfFunc = Functions.IndexOf(func);
                    if (indexOfFunc > 0)
                        SwapFunc(indexOfFunc, indexOfFunc - 1);
                }
            });

            var moveFuncDOWN = new MoveFuncCommand(f =>
            {
                if (Functions.Count < 2) return;
                FunctionContainer func = f as FunctionContainer;
                if (func != null)
                {
                    int indexOfFunc = Functions.IndexOf(func);
                    if (indexOfFunc < Functions.Count - 1)
                        SwapFunc(indexOfFunc, indexOfFunc + 1);
                }
            });

            var removeFunc = new MoveFuncCommand(f =>
            {
                if (Functions.Count == 0) return;
                FunctionContainer func = f as FunctionContainer;
                if (func != null)
                {
                    int indexOfFunc = Functions.IndexOf(func);
                    Functions.RemoveAt(indexOfFunc);
                }
            });

            AllFunctions.AddRange
                (
                new FunctionContainer[] {

                     new FunctionContainer("Стандартная Очистка", new BindableCollection<Argument>{
                        new IntArgument("Число шагов", 1, 50, 5),
                    },
                        new FunctionWithArgumentCommand(args =>
                        {
                            if (DataManager.CurrentData.Eulers == null) return;

                            var ar = args as BindableCollection<Argument>;
                            if(ar == null)
                            {
                                MessageBox.Show("АРГУМЕНТЫ ПОПУТАЛИСЬ!!!");
                                return;
                            }

                            FunctionContainer fcThis = Functions.FirstOrDefault(x => x.Name == "Стандартная Очистка");
                            if(fcThis != null && Functions.IndexOf(fcThis) == 0)
                                rawEulers = DataManager.CurrentData.Eulers;

                            int maxIterations = (ar[0] as IntArgument).Value;
                            rawEulers = functions.GPU.StandartCleanUp(rawEulers, DataManager.CurrentData.Size, maxIterations);
                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                    new FunctionContainer("Kuwahara Очистка", new BindableCollection<Argument>{
                        new IntArgument("Число шагов", 1, 30, 15),
                    },
                        new FunctionWithArgumentCommand(args =>
                        {
                            if (DataManager.CurrentData.Eulers == null) return;

                            var ar = args as BindableCollection<Argument>;
                            if(ar == null)
                            {
                                MessageBox.Show("АРГУМЕНТЫ ПОПУТАЛИСЬ!!!");
                                return;
                            }

                            FunctionContainer fcThis = Functions.FirstOrDefault(x => x.Name == "Kuwahara Очистка");
                            if(fcThis != null && Functions.IndexOf(fcThis) == 0)
                                rawEulers = DataManager.CurrentData.Eulers;

                            int maxIterations = (ar[0] as IntArgument).Value;
                            rawEulers = functions.GPU.KuwaharaCleanUp(rawEulers, DataManager.CurrentData.Size, maxIterations);
                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                    new FunctionContainer("Автоматическая Очистка", new BindableCollection<Argument>{
                        new IntArgument("Число шагов", 1, 100, 10),
                    },
                        new FunctionWithArgumentCommand(args =>
                        {
                            if (DataManager.CurrentData.Eulers == null) return;

                            var ar = args as BindableCollection<Argument>;
                            if(ar == null)
                            {
                                MessageBox.Show("АРГУМЕНТЫ ПОПУТАЛИСЬ!!!");
                                return;
                            }

                            FunctionContainer fcThis = Functions.FirstOrDefault(x => x.Name == "Автоматическая Очистка");
                            if(fcThis != null && Functions.IndexOf(fcThis) == 0)
                                rawEulers = DataManager.CurrentData.Eulers;

                            int maxIterations = (ar[0] as IntArgument).Value;
                            rawEulers = functions.GPU.AutomaticCleanUp(rawEulers, DataManager.CurrentData.Size, maxIterations);
                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                    new FunctionContainer("Расчет границ зерен", new BindableCollection<Argument>{
                        new FloatArgument("Пороговый угол", 0, 90, 10, 0.1d),
                    },
                        new FunctionWithArgumentCommand(args =>
                        {
                            if (DataManager.CurrentData.Eulers == null) return;

                            var ar = args as BindableCollection<Argument>;
                            if(ar == null)
                            {
                                MessageBox.Show("АРГУМЕНТЫ ПОПУТАЛИСЬ!!!");
                                return;
                            }
                            float tresholdAngle = (ar[0] as FloatArgument).Value;
                            var color = DataManager.CurrentData.Settings.GrainSelectBorderColor;
                            Mask mask = functions.GPU.GetGrainMask(rawEulers, DataManager.CurrentData.Size, tresholdAngle, new GpuColor(color.R, color.G, color.B, color.A));

                            grainMask = mask;
                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                    new FunctionContainer("Картирование (BC/Euler/Strain...)", new BindableCollection<Argument>{
                        new MapVariantArgument("Вариант", MapVariant.Euler),
                        new BoolArgument("Отображать границы", true),
                    },
                        new FunctionWithArgumentCommand(args =>
                        {
                            if (DataManager.CurrentData.Eulers == null) return;

                            var ar = args as BindableCollection<Argument>;
                            if(ar == null)
                            {
                                MessageBox.Show("АРГУМЕНТЫ ПОПУТАЛИСЬ!!!");
                                return;
                            }
                            MapVariant mapVariant = (ar[0] as MapVariantArgument).Value;
                            bool displayGrainMask = (ar[1] as BoolArgument).Value;

                            if(rawEulers.Length != DataManager.CurrentData.Size.x*DataManager.CurrentData.Size.y) return;

                            switch (mapVariant)
                            {
                                case MapVariant.BandContrast:
                                    {
                                        colors = functions.GPU.GetColorMapBC(DataManager.CurrentData.BC, DataManager.CurrentData.Size);
                                        break;
                                    }

                                case MapVariant.Euler:
                                    {
                                        colors = functions.GPU.GetColorMapEuler(rawEulers, DataManager.CurrentData.Size);
                                        break;
                                    }

                                case MapVariant.Strain:
                                    {
                                        //colors = functions.GPU.GetColorMapBC(DataManager.CurrentData.BC, DataManager.CurrentData.Size);
                                        break;
                                    }
                            }

                            maskedColors = colors;

                            if(displayGrainMask && grainMask.colors != null && colors.Length == grainMask.colors.Length)
                            maskedColors = functions.GPU.ApplyMask(colors, grainMask, DataManager.CurrentData.Size);

                            var bmp = functions.BitmapFunc.ByteArrayToBitmap(DataManager.CurrentData.Size, maskedColors);
                            MainImage.Source = functions.BitmapFunc.CreateBitmapSource(bmp);
                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                });


            Functions.AddRange(AllFunctions);
        }

        public void AutoUpdate()
        {
            if (DataManager.CurrentData.Settings.AutoUpdate)
            {
                LaunchAllFunctions();
            }
        }

        public void LaunchAllFunctions()
        {
            foreach (var f in Functions)
            {
                f.Run();
            }
        }

        private Vector2Int GetPixelCoordinate(Image image, MouseEventArgs e)
        {
            Point pt = e.GetPosition(image);
            pt.X *= image.Source.Width / image.ActualWidth;
            pt.Y *= image.Source.Height / image.ActualHeight;
            return new Vector2Int((int)pt.X, (int)pt.Y);
        }

        private void SwapFunc(int a, int b)
        {
            var buff = Functions[a];
            Functions[a] = Functions[b];
            Functions[b] = buff;
        }

        private void MainWin_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string path = files[0];
                string extension = Path.GetExtension(path);
                if (extension == ".dip")
                {
                    OpenFile(path);
                }
                else if (Path.GetExtension(path) == ".xlsx")
                {
                    ExcelImport(path);
                }
            }
        }

        private void OpenFileUsingCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                string pathToFile = args[1];
                OpenFile(pathToFile);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeFunctions();

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

            OpenFileUsingCommandLineArgs();
            bufferEulers = DataManager.CurrentData.Eulers;
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
                    saveFileDialog.Filter = "diplom files (*.dip)|*.dip";
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string pathToFile = saveFileDialog.FileName;
                        DataManager.Save(pathToFile);
                    }
                }
            }
        }

        private void ExcelImport(string pathToFile)
        {
            try
            {
                DataManager.LoadEbsdFromExcel(pathToFile);
                rawEulers = DataManager.CurrentData.Eulers;
                bufferEulers = rawEulers;
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

        private void SaveFile()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "diplom files (*.dip)|*.dip";
            if (saveFileDialog.ShowDialog() == true)
            {
                string pathToFile = saveFileDialog.FileName;
                DataManager.Save(pathToFile);
                Title = DataManager.ProjectName;
            }
        }

        private void OpenFile(string pathToFile)
        {
            try
            {
                DataManager.Load(pathToFile);
                rawEulers = DataManager.CurrentData.Eulers;
                bufferEulers = rawEulers;
            }
            catch (JsonLoadException)
            {
                MessageBox.Show("Ошибка чтения файла проекта\nФайл поврежден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Title = DataManager.ProjectName;
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
            openFileDialog.Filter = "diplom files (*.dip)|*.dip";
            if (openFileDialog.ShowDialog() == true)
            {
                string pathToFile = openFileDialog.FileName;
                OpenFile(pathToFile);
            }
        }

        private void ExcelImportButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
            if (openFileDialog.ShowDialog() == true)
            {
                string pathToFile = openFileDialog.FileName;
                ExcelImport(pathToFile);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
        }

        private void ResetImageSize_Click(object sender, RoutedEventArgs e)
        {
            MainImageContainer.Reset();
        }

        private void MainImage_MouseMove(object sender, MouseEventArgs e)
        {
            Vector2Int coords = GetPixelCoordinate(MainImage, e);
            X.Content = coords.x;
            Y.Content = coords.y;
        }

        private void AutoUpdateTextBox(object sender, TextChangedEventArgs e) => AutoUpdate();
        private void AutoUpdateSlider(object sender, RoutedPropertyChangedEventArgs<double> e) => AutoUpdate();
        private void AutoUpdateCombobox(object sender, SelectionChangedEventArgs e) => AutoUpdate();
        private void AutoUpdateCheckbox(object sender, RoutedEventArgs e) => AutoUpdate();

    }
}
