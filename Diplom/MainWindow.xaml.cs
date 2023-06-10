using Caliburn.Micro;
using Diplom.DataModule;
using Diplom.FuncModule;
using Diplom.UI;
using Microsoft.Win32;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Diplom
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private Functions functions;
		//----------------------------------------
		public List<AnalysContainer> AllAnalysBlocks { get; set; } = new List<AnalysContainer>();
		public BindableCollection<AnalysContainer> AnalysBlocks { get; set; } = new BindableCollection<AnalysContainer>();
		//----------------------------------------
		public List<FunctionContainer> AllFunctions { get; set; } = new List<FunctionContainer>();
		public BindableCollection<FunctionContainer> Functions { get; set; } = new BindableCollection<FunctionContainer>();
		//----------------------------------------
		private System.Drawing.Bitmap mainImgBitmapBuffer { get; set; }
		//-------------------------------------------
		public string DebugText
		{
			get { return _debugText; }
			set
			{
				if (string.Equals(value, _debugText)) return;
				_debugText = value;
				OnPropertyChanged("DebugText");
			}
		}
		private string _debugText = "";
		private Dictionary<string, int> logs = new Dictionary<string, int>();
		//-------------------------------------------
		private bool isMouseDrag = false;

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
							  DataManager.RawEulers = functions.GPU.StandartCleanUp(DataManager.BufferEulers, DataManager.CurrentData.Size, maxIterations);
							else
							   DataManager.RawEulers = functions.GPU.StandartCleanUp(DataManager.RawEulers, DataManager.CurrentData.Size, maxIterations);
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
								DataManager.RawEulers = functions.GPU.KuwaharaCleanUp(DataManager.BufferEulers, DataManager.CurrentData.Size, maxIterations);
							else
								DataManager.RawEulers = functions.GPU.KuwaharaCleanUp(DataManager.RawEulers, DataManager.CurrentData.Size, maxIterations);
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
								DataManager.RawEulers = functions.GPU.AutomaticCleanUp(DataManager.BufferEulers, DataManager.CurrentData.Size, maxIterations);
							else
								DataManager.RawEulers = functions.GPU.AutomaticCleanUp(DataManager.RawEulers, DataManager.CurrentData.Size, maxIterations);
						}),
						moveFuncUP,
						moveFuncDOWN,
						removeFunc
					),

					new FunctionContainer("Расчет границ зерен", new BindableCollection<Argument>{
						new FloatArgument("Низкогуловые >=", 0, 180, 2, 0.1d),
						new FloatArgument("Высокоугловые >=", 0, 180, 10, 0.1d),
						new ColorArgument("Цвет низ.", Color.FromArgb(200,0, 0, 200)),
						new ColorArgument("Цвет выс.", Color.FromArgb(200, 0, 200, 0)),
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
							float lowAngle = (ar[0] as FloatArgument).Value;
							float highAngle = (ar[1] as FloatArgument).Value;
							Color lowColor = (ar[2] as ColorArgument).Value;
							Color highColor = (ar[3] as ColorArgument).Value;

							var color = DataManager.CurrentData.Settings.GrainsBorderColor;
							if(color.R == 0 && color.G == 0 && color.B == 0) color = Color.FromArgb(color.A, 1, 1, 1);

							Mask mask = functions.GPU.GetGrainMask(
								DataManager.RawEulers,
								DataManager.CurrentData.Size,
								lowAngle,highAngle,
								new GpuColor(lowColor.R, lowColor.G, lowColor.B, lowColor.A),
								new GpuColor(highColor.R, highColor.G, highColor.B, highColor.A));

							DataManager.GrainMask = mask;
						}),
						moveFuncUP,
						moveFuncDOWN,
						removeFunc
					),

					new FunctionContainer("Обнаружение зерен", new BindableCollection<Argument>{
						new FloatArgument("Минимальный размер, µm²", 0, 200, 0.05f, 0.005f),
						new BoolArgument("Использовать общий параметр", false),
					},
						new FunctionWithArgumentCommand(args =>
						{
							if (DataManager.CurrentData.Eulers == null || DataManager.GrainMask.colors.Length == 0) return;

							var ar = args as BindableCollection<Argument>;
							if(ar == null)
							{
								MessageBox.Show("АРГУМЕНТЫ ПОПУТАЛИСЬ!!!");
								return;
							}
							float minSize = (ar[0] as FloatArgument).Value;
							bool useGlobalMinSize = (ar[1] as BoolArgument).Value;

							if(useGlobalMinSize)
								DataManager.RawGrains = functions.CPU.DefineGrains(DataManager.GrainMask, DataManager.CurrentData.Settings.MinGrainSize);
							else
								DataManager.RawGrains = functions.CPU.DefineGrains(DataManager.GrainMask, minSize);

						}),
						moveFuncUP,
						moveFuncDOWN,
						removeFunc
					),

					new FunctionContainer("Картирование размера зерна*", new BindableCollection<Argument>{
						new IntArgument("Прозрачность", 0, 255, 120),
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
							int opacity = (ar[0] as IntArgument).Value;
							Mask mask = functions.CPU.GetGrainSizeMask(DataManager.CurrentData.Size, opacity);
							DataManager.StrainMask = mask; //маска не напряжения тут должна быть а другая (подумой) !!!!!!!!!!!!
                        }),
						moveFuncUP,
						moveFuncDOWN,
						removeFunc
					),


					new FunctionContainer("Картирование формы зерна*", new BindableCollection<Argument>{
						new IntArgument("Прозрачность", 0, 255, 120),
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
							int opacity = (ar[0] as IntArgument).Value;
							Mask mask = functions.CPU.GetGrainAspectRatioMask(DataManager.CurrentData.Size, opacity);
							DataManager.StrainMask = mask; //маска не напряжения тут должна быть а другая (подумой) !!!!!!!!!!!!
                        }),
						moveFuncUP,
						moveFuncDOWN,
						removeFunc
					),

					new FunctionContainer("Картирование диаметра эквивалентной окружности (ECD)*", new BindableCollection<Argument>{
						new IntArgument("Прозрачность", 0, 255, 120),
					},
						new FunctionWithArgumentCommand(args =>
						{
							if (DataManager.CurrentData.Eulers == null) return;
							if (!DataManager.RawGrains.Any()) return;

							var ar = args as BindableCollection<Argument>;
							if(ar == null)
							{
								MessageBox.Show("АРГУМЕНТЫ ПОПУТАЛИСЬ!!!");
								return;
							}
							int opacity = (ar[0] as IntArgument).Value;
							Mask mask = functions.CPU.GetGrainECDMask(DataManager.CurrentData.Size, opacity);
							DataManager.StrainMask = mask; //маска не напряжения тут должна быть а другая (подумой) !!!!!!!!!!!!
                        }),
						moveFuncUP,
						moveFuncDOWN,
						removeFunc
					),

					new FunctionContainer("Расчет напряжений (KAM)", new BindableCollection<Argument>{
						new FloatArgument("Максимальное отклонение", 0f, 15, 2.5f, 0.25f),
						new IntArgument("Прозрачность", 0, 255, 120),
                        //new ColorArgument("Низкое ", Color.FromArgb(255, 0, 0, 0)),
                        //new ColorArgument("Высокое", Color.FromArgb(255, 0, 220, 220)),
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
                            //Color lowColor = (ar[2] as ColorArgument).Value;
                            //Color highColor = (ar[3] as ColorArgument).Value;

                            //GpuColor lowGpuColor = new GpuColor(lowColor.R, lowColor.G, lowColor.B, lowColor.A);
                            //GpuColor highGpuColor = new GpuColor(highColor.R, highColor.G, highColor.B, highColor.A);

                            Mask mask = functions.GPU.GetStrainMaskKAM(DataManager.RawEulers, DataManager.CurrentData.Size/*, lowGpuColor, highGpuColor*/, referenceDeviation, opacity);

							DataManager.StrainMask = mask;
						}),
						moveFuncUP,
						moveFuncDOWN,
						removeFunc
					),

					new FunctionContainer("Расчет напряжений (KAM) CPU*", new BindableCollection<Argument>{
						new IntArgument("Прозрачность", 0, 255, 120),
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
							int opacity = (ar[0] as IntArgument).Value;
							Mask mask = functions.CPU.GetStrainMaskKAM(DataManager.RawEulers, DataManager.CurrentData.Size, opacity);
							DataManager.StrainMask = mask;
						}),
						moveFuncUP,
						moveFuncDOWN,
						removeFunc
					),

					new FunctionContainer("Расчет напряжений (GOS)", new BindableCollection<Argument>{
						new FloatArgument("Максимальное отклонение", 0f, 270, 20f, 10f),
						new IntArgument("Прозрачность", 0, 255, 120),
						new ColorArgument("Низкое ", Color.FromArgb(255, 0, 100, 0)),
						new ColorArgument("Высокое", Color.FromArgb(255, 255, 255, 255)),
					},
						new FunctionWithArgumentCommand(args =>
						{
							if (DataManager.CurrentData.Eulers == null || DataManager.RawGrains.Length == 0) return;

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

							Mask mask = functions.CPU.GetStrainMaskGOS(DataManager.RawEulers, DataManager.RawGrains, DataManager.CurrentData.Size, lowGpuColor, highGpuColor, referenceDeviation, opacity);

							DataManager.StrainMask = mask;
						}),
						moveFuncUP,
						moveFuncDOWN,
						removeFunc
					),

					new FunctionContainer("Картирование (BC/Euler/Phase...)", new BindableCollection<Argument>{
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
										DataManager.Colors = functions.GPU.GetColorMapBC(DataManager.CurrentData.BC, DataManager.CurrentData.Size);
										break;
									}

								case MapVariant.Euler:
									{
										DataManager.Colors = functions.GPU.GetColorMapEuler(DataManager.RawEulers, DataManager.CurrentData.Size);
										break;
									}

								case MapVariant.Phases:
									{
										DataManager.Colors = functions.GPU.GetColorMapPhases(DataManager.RawPhaseIndexes,
											DataManager.CurrentData.Settings.Phases.ToArray(),
											DataManager.CurrentData.Size);
										break;
									}
							}

							DataManager.MaskedColors = DataManager.Colors;

							if(displayGrainMask && DataManager.GrainMask.colors != null && DataManager.GrainMask.colors.Length != 0)
							DataManager.MaskedColors = functions.GPU.ApplyMask(DataManager.MaskedColors, DataManager.GrainMask, DataManager.CurrentData.Size);

							if(displayStrainMask && DataManager.StrainMask.colors != null && DataManager.StrainMask.colors.Length != 0)
							DataManager.MaskedColors = functions.GPU.ApplyMask(DataManager.MaskedColors, DataManager.StrainMask, DataManager.CurrentData.Size);

							var bmp = functions.BitmapFunc.ByteArrayToBitmap(DataManager.CurrentData.Size, DataManager.MaskedColors);
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

		private void InitializeAnalysBlocks()
		{

			var removeAnalysBlock = new MoveAnalysBlockCommand(f =>
			{
				if (AnalysBlocks.Count == 0) return;
				AnalysContainer analysBlock = f as AnalysContainer;
				if (analysBlock != null)
				{
					int indexOfFunc = AnalysBlocks.IndexOf(analysBlock);
					AnalysBlocks.RemoveAt(indexOfFunc);
				}
			});

			AllAnalysBlocks.AddRange
				(
				new AnalysContainer[] {

					new AnalysContainer("Распределение размеров зерен", new BindableCollection<AnalysData>{
						new Diagram("Диаграмма", "Размер, µm²","Количество, шт"),
					},
						new AnalysWithArgumentCommand(args =>
						{
							if (DataManager.CurrentData.Eulers == null || DataManager.RawGrains.Length == 0) return;

							var ar = args as BindableCollection<AnalysData>;
							if(ar == null)
							{
								MessageBox.Show("АРГУМЕНТЫ ПОПУТАЛИСЬ!!!");
								return;
							}
							var points = (ar[0] as Diagram).Values;
							var categoriesLabels = (ar[0] as Diagram).CategoriesLabels;
							points.Clear();
							categoriesLabels.Clear();

							List<float> categoriesIndexes = new List<float>();
							Grain[] orderedGrains = DataManager.RawGrains.OrderBy(x => x.Size).ToArray();

							for (int i = 0; i < orderedGrains.Last().Size / 100f; i+=100)
							{
								categoriesIndexes.Add(i * DataManager.CurrentData.Settings.NmPpx * DataManager.CurrentData.Settings.NmPpx);
							}
							for (int i = 0; i < categoriesIndexes.Count-1; i++)
							{
								int count = orderedGrains.Count(x =>
																	x.Size * DataManager.CurrentData.Settings.NmPpx *
																	DataManager.CurrentData.Settings.NmPpx > categoriesIndexes[i] &&
																	x.Size * DataManager.CurrentData.Settings.NmPpx *
																	DataManager.CurrentData.Settings.NmPpx < categoriesIndexes[i+1]);

								points.Add(new ColumnItem(count));
								categoriesLabels.Add(categoriesIndexes[i].ToString());
							}

						}),
						removeAnalysBlock
					),


					new AnalysContainer("Фазовый состав", new BindableCollection<AnalysData>{
						new UI.Table("Диаграмма"),
					},
						new AnalysWithArgumentCommand(args =>
						{
							if (DataManager.CurrentData.Eulers == null || DataManager.RawGrains.Length == 0) return;

							var ar = args as BindableCollection<AnalysData>;
							if(ar == null)
							{
								MessageBox.Show("АРГУМЕНТЫ ПОПУТАЛИСЬ!!!");
								return;
							}
							var tableValues = (ar[0] as UI.Table).Values;
							tableValues.Clear();

							foreach(var p in DataManager.CurrentData.Settings.Phases)
							{
								int count = DataManager.CurrentData.Points.Count(x=>x.Phase==p.Index);
								float percent = 100f* ((float)count/DataManager.CurrentData.Points.Length);
								tableValues.Add(new PhaseConsist(){Name=p.Name, Count =  count, Percent=percent});
							}
						}),
						removeAnalysBlock
					),
				});

			// Functions.AddRange(AllFunctions);
			AnalysBlocks.AddRange(AllAnalysBlocks);
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
			InitializeAnalysBlocks();

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

			MainImageContainer.label = MainImageScaleLabel;
			MainImageContainer.image = MainImage;
			mainImgBitmapBuffer = functions.BitmapFunc.ImageSourceToBitmap(MainImage.Source);
			Closing += MainWindow_Closing;

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
				DataManager.RawEulers = DataManager.CurrentData.Eulers;
				DataManager.BufferEulers = DataManager.RawEulers;
				DataManager.RawPhaseIndexes = DataManager.CurrentData.Points.Select(x => x.Phase).ToArray();
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
				DataManager.RawEulers = DataManager.CurrentData.Eulers;
				DataManager.BufferEulers = DataManager.RawEulers;
				DataManager.RawPhaseIndexes = DataManager.CurrentData.Points.Select(x => x.Phase).ToArray();
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

			isMouseDrag = e.LeftButton == MouseButtonState.Pressed;
			//if (DataManager.RawGrains.Length == 0) return;
			//SelectGrain(GetGrainByCoords(coords));
		}

		private void MainImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (isMouseDrag || DataManager.RawGrains.Length == 0)
			{
				isMouseDrag = false;
				return;
			}
			Vector2Int coords = GetPixelCoordinate(MainImage, e);
			SelectGrain(DataManager.GetGrainByCoords(coords));
		}

		private void LaunchAllButton_Click(object sender, RoutedEventArgs e) => LaunchAllFunctions();
		private void SettingsButton_Click(object sender, RoutedEventArgs e) => new SettingsWindow().ShowDialog();
		private void AddFunction_Click(object sender, RoutedEventArgs e) => new FunctionsListWindow(Functions, AllFunctions.Except(Functions)).ShowDialog();

		private void AddAnalys_Click(object sender, RoutedEventArgs e) => new AnalysListWindow(AnalysBlocks, AllAnalysBlocks.Except(AnalysBlocks)).ShowDialog();
		private void ResetImageSize_Click(object sender, RoutedEventArgs e) => MainImageContainer.Reset();

		//--------------------------------------------

		private void AutoUpdateTextBox(object sender, TextChangedEventArgs e) => AutoUpdate();
		private void AutoUpdateSlider(object sender, RoutedPropertyChangedEventArgs<double> e) => AutoUpdate();
		private void AutoUpdateCombobox(object sender, SelectionChangedEventArgs e) => AutoUpdate();
		private void AutoUpdateCheckbox(object sender, RoutedEventArgs e) => AutoUpdate();

		private static readonly Regex _regexInt = new Regex("[^0-9-]+");    //regex that matches disallowed text
		private static readonly Regex _regexFloat = new Regex("[^0-9.-]+"); //regex that matches disallowed text

		private static bool IsTextAllowedInt(string text) => !_regexInt.IsMatch(text);
		private static bool IsTextAllowedFloat(string text) => !_regexFloat.IsMatch(text);


		private void NumericTextboxInt_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
			e.Handled = !IsTextAllowedInt(e.Text);
		private void NumericTextboxFloat_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
			e.Handled = !IsTextAllowedFloat(e.Text);

		private void RebuildGpuButton_Click(object sender, RoutedEventArgs e) => PrintLog(functions.GPU.RebuildProgramm());
		private void ClearLogButton_Click(object sender, RoutedEventArgs e) => ClearLog();

		#endregion


		//INotifyPropertyChanged members
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}

		private void PrintLog(string msg)
		{
			if (logs.ContainsKey(msg)) logs[msg]++;
			else logs.Add(msg, 1);

			var log = string.Join('\n', logs.Select(x => $"{x.Key} :{x.Value}"));
			DebugText = log;
		}

		private void ClearLog()
		{
			logs = new Dictionary<string, int>();
			DebugText = "";
		}

		private void MainImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			e.Handled = true; // to prevent scrollViewer event

			var e2 = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
			e2.RoutedEvent = UIElement.MouseWheelEvent;
			(MainImage).RaiseEvent(e2); // to raise zoom event
		}
	}
}
