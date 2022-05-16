using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private byte[] maskedColors = new byte[0];

        private Mask strainMask = new Mask();

        private Mask grainMask = new Mask();
        private Grain[] rawGrains = new Grain[0];

        private Euler[] rawEulers = new Euler[0];
        private Euler[] bufferEulers = new Euler[0];

        private System.Drawing.Bitmap mainImgBitmapBuffer;
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
                        new IntArgument("Число шагов", 1, 12, 1),
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
                            int maxIterations = (ar[0] as IntArgument).Value;

                            FunctionContainer fcThis = Functions.FirstOrDefault(x => x.Name == "Стандартная Очистка");
                            if(fcThis != null && Functions.IndexOf(fcThis) == 0)
                               rawEulers = functions.GPU.StandartCleanUp(bufferEulers, DataManager.CurrentData.Size, maxIterations);
                            else
                               rawEulers = functions.GPU.StandartCleanUp(rawEulers, DataManager.CurrentData.Size, maxIterations);
                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                    new FunctionContainer("Kuwahara Очистка", new BindableCollection<Argument>{
                        new IntArgument("Число шагов", 1, 30, 5),
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
                            int maxIterations = (ar[0] as IntArgument).Value;

                            FunctionContainer fcThis = Functions.FirstOrDefault(x => x.Name == "Kuwahara Очистка");
                            if(fcThis != null && Functions.IndexOf(fcThis) == 0)
                                rawEulers = functions.GPU.KuwaharaCleanUp(bufferEulers, DataManager.CurrentData.Size, maxIterations);
                            else
                                rawEulers = functions.GPU.KuwaharaCleanUp(rawEulers, DataManager.CurrentData.Size, maxIterations);
                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                    new FunctionContainer("Автоматическая Очистка", new BindableCollection<Argument>{
                        new IntArgument("Число шагов", 1, 10, 1),
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
                            int maxIterations = (ar[0] as IntArgument).Value;

                            FunctionContainer fcThis = Functions.FirstOrDefault(x => x.Name == "Автоматическая Очистка");
                            if(fcThis != null && Functions.IndexOf(fcThis) == 0)
                                rawEulers = functions.GPU.AutomaticCleanUp(bufferEulers, DataManager.CurrentData.Size, maxIterations);
                            else
                                rawEulers = functions.GPU.AutomaticCleanUp(rawEulers, DataManager.CurrentData.Size, maxIterations);
                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                    new FunctionContainer("Расчет границ зерен", new BindableCollection<Argument>{
                        new FloatArgument("Пороговый угол", 0, 180, 10, 0.1d),
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
                            var color = DataManager.CurrentData.Settings.GrainsBorderColor;
                            if(color.R == 0 && color.G == 0 && color.B == 0) color = Color.FromArgb(color.A, 1, 1, 1);

                            Mask mask = functions.GPU.GetGrainMask(rawEulers, DataManager.CurrentData.Size, tresholdAngle, new GpuColor(color.R, color.G, color.B, color.A));

                            grainMask = mask;
                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                    new FunctionContainer("Обнаружение зерен", new BindableCollection<Argument>{
                        new FloatArgument("Минимальный размер, µm²", 0, 200, 0.1f, 0.005f),
                        new BoolArgument("Использовать общий параметр", false),
                    },
                        new FunctionWithArgumentCommand(args =>
                        {
                            if (DataManager.CurrentData.Eulers == null || grainMask.colors.Length == 0) return;

                            var ar = args as BindableCollection<Argument>;
                            if(ar == null)
                            {
                                MessageBox.Show("АРГУМЕНТЫ ПОПУТАЛИСЬ!!!");
                                return;
                            }
                            float minSize = (ar[0] as FloatArgument).Value;
                            bool useGlobalMinSize = (ar[1] as BoolArgument).Value;

                            if(useGlobalMinSize)
                                rawGrains = functions.CPU.DefineGrains(grainMask, DataManager.CurrentData.Settings.MinGrainSize);
                            else
                                rawGrains = functions.CPU.DefineGrains(grainMask, minSize);

                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                    new FunctionContainer("Расчет напряжений (KAM)", new BindableCollection<Argument>{
                        new FloatArgument("Максимальное отклонение", 0f, 15, 4f, 0.25f),
                        new IntArgument("Прозрачность", 0, 255, 255),
                        new ColorArgument("Низкое ", Color.FromArgb(255, 80, 80, 80)),
                        new ColorArgument("Высокое", Color.FromArgb(255, 0, 220, 220)),
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
                            float referenceDeviation = (ar[0] as FloatArgument).Value;
                            int opacity = (ar[1] as IntArgument).Value;
                            Color lowColor = (ar[2] as ColorArgument).Value;
                            Color highColor = (ar[3] as ColorArgument).Value;

                            GpuColor lowGpuColor = new GpuColor(lowColor.R, lowColor.G, lowColor.B, lowColor.A);
                            GpuColor highGpuColor = new GpuColor(highColor.R, highColor.G, highColor.B, highColor.A);

                            Mask mask = functions.GPU.GetStrainMaskKAM(rawEulers, DataManager.CurrentData.Size, lowGpuColor, highGpuColor, referenceDeviation, opacity);

                            strainMask = mask;
                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                    new FunctionContainer("Картирование (BC/Euler/Strain...)", new BindableCollection<Argument>{
                        new MapVariantArgument("Вариант", MapVariant.Euler),
                        new BoolArgument("Отображать границы", false),
                        new BoolArgument("Отображать напряжения", false),
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
                            bool displayStrainMask = (ar[2] as BoolArgument).Value;

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

                            }

                            maskedColors = colors;

                            if(displayGrainMask && grainMask.colors != null && grainMask.colors.Length != 0)
                            maskedColors = functions.GPU.ApplyMask(maskedColors, grainMask, DataManager.CurrentData.Size);

                            if(displayStrainMask && strainMask.colors != null && strainMask.colors.Length != 0)
                            maskedColors = functions.GPU.ApplyMask(maskedColors, strainMask, DataManager.CurrentData.Size);

                            var bmp = functions.BitmapFunc.ByteArrayToBitmap(DataManager.CurrentData.Size, maskedColors);
                            mainImgBitmapBuffer = bmp;

                            MainImage.Source = functions.BitmapFunc.BitmapToBitmapSource(bmp);

                        }),
                        moveFuncUP,
                        moveFuncDOWN,
                        removeFunc
                    ),

                });


            // Functions.AddRange(AllFunctions);
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

        public Vector2Int GetPixelCoordinate(Image image, MouseEventArgs e)
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

        private Grain GetGrainByCoords(Vector2Int coords) => rawGrains.FirstOrDefault(x =>
            x.Points.Contains(new Vector2(coords.x, coords.y)) || x.Edges.Contains(new Vector2(coords.x, coords.y)));

        private void SelectGrain(Grain grain)
        {
            if (grain.Edges == null || grain.Points == null) return;

            var bmp = (System.Drawing.Bitmap)mainImgBitmapBuffer.Clone();
            foreach (var p in grain.Edges)
            {
                var selectColor = DataManager.CurrentData.Settings.GrainSelectBorderColor;

                bmp.SetPixel((int)p.x, (int)p.y, System.Drawing.Color.FromArgb(selectColor.A, selectColor.R, selectColor.G, selectColor.B));
            }

            MainImage.Source = functions.BitmapFunc.BitmapToBitmapSource(bmp);
        }

        #region --------------------- MAIN WINDOW ---------------------
        public MainWindow()
        {
            InitializeComponent();
            InitializeFunctions();

            MainImageContainer.label = MainImageScaleLabel;
            MainImageContainer.image = MainImage;
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
        #endregion


        #region --------------------- SAVE OPEN ---------------------
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

        private void OpenFileUsingCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                string pathToFile = args[1];
                OpenFile(pathToFile);
            }
        }
        #endregion


        #region --------------------- EVENT HANDLERS ---------------------
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

        private void SaveFileButton_Click(object sender, RoutedEventArgs e) => SaveFile();

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

        private void MainImage_MouseMove(object sender, MouseEventArgs e)
        {
            Vector2Int coords = GetPixelCoordinate(MainImage, e);
            X.Content = "x: " + coords.x.ToString();
            Y.Content = "y: " + coords.y.ToString();

            //if (rawGrains.Length == 0) return;
            //SelectGrain(GetGrainByCoords(coords));
        }

        private void MainImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (rawGrains.Length == 0) return;
            Vector2Int coords = GetPixelCoordinate(MainImage, e);
            SelectGrain(GetGrainByCoords(coords));
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e) => new SettingsWindow().ShowDialog();
        private void AddFunction_Click(object sender, RoutedEventArgs e) => new FunctionsListWindow(Functions, AllFunctions.Except(Functions)).ShowDialog();
        private void ResetImageSize_Click(object sender, RoutedEventArgs e) => MainImageContainer.Reset();

        //--------------------------------------------

        private void AutoUpdateTextBox(object sender, TextChangedEventArgs e) => AutoUpdate();
        private void AutoUpdateSlider(object sender, RoutedPropertyChangedEventArgs<double> e) => AutoUpdate();
        private void AutoUpdateCombobox(object sender, SelectionChangedEventArgs e) => AutoUpdate();
        private void AutoUpdateCheckbox(object sender, RoutedEventArgs e) => AutoUpdate();

        private static readonly Regex _regexInt = new Regex("[^0-9-]+"); //regex that matches disallowed text
        private static readonly Regex _regexFloat = new Regex("[^0-9.-]+"); //regex that matches disallowed text

        private static bool IsTextAllowedInt(string text) => !_regexInt.IsMatch(text);
        private static bool IsTextAllowedFloat(string text) => !_regexFloat.IsMatch(text);


        private void NumericTextboxInt_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowedInt(e.Text);
        private void NumericTextboxFloat_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowedFloat(e.Text);


        #endregion

    }
}
