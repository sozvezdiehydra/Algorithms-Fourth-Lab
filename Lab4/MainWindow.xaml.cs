using OxyPlot.Series;
using OxyPlot.Wpf;
using OxyPlot;
using System.Windows;
using System.Windows.Controls;
using OxyPlot.Axes;
using System.IO;
using System.Globalization;
using System.Text;


namespace Lab4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<int> _data;
        private List<Record> _dataRecord;
        private int _delay = 500; // Задержка по умолчанию (в мс)
        private PlotModel _plotModel;

        public MainWindow()
        {
            InitializeComponent();
            InitializePlot();
        }

        private void OpenSecondWindowButton_Click(object sender, RoutedEventArgs e)
        {
            EtcSorts secondWindow = new EtcSorts();
            secondWindow.Show(); // Показывает окно немодально
            // secondWindow.ShowDialog(); // Показывает окно модально
        }

        private void InitializePlot()
        {
            _plotModel = new PlotModel { Title = "Sorting Visualization" };

            // Настройка категорий и осей для вертикальных столбцов
            _plotModel.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Left,  // CategoryAxis should be on the Y-axis (for categories)
                Key = "Categories",
                ItemsSource = new List<string>() // Заглушка, обновится динамически
            });

            _plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom, // LinearAxis should be on the X-axis (for values)
                Minimum = 0
            });

            var barSeries = new BarSeries
            {
                ItemsSource = new List<BarItem>(),
                LabelPlacement = LabelPlacement.Outside,
                LabelFormatString = "{0:.0}",
                FillColor = OxyColors.SkyBlue
            };

            _plotModel.Series.Add(barSeries);

            PlotView.Model = _plotModel;
        }

        private void UpdatePlot(List<int> highlightIndices = null, List<OxyColor> highlightColors = null, List<int> sortedIndices = null)
        {
            var barSeries = (BarSeries)_plotModel.Series[0];

            // Обновление данных
            barSeries.ItemsSource = _data.Select((value, index) =>
                new BarItem
                {
                    Value = value,
                    Color = sortedIndices != null && sortedIndices.Contains(index)
                        ? OxyColors.Gray // Цвет для отсортированных элементов
                        : (highlightIndices != null && highlightIndices.Contains(index)
                            ? highlightColors[highlightIndices.IndexOf(index)]
                            : OxyColors.SkyBlue) // Цвет по умолчанию
                }).ToList();

            // Обновление категорий
            var categoryAxis = _plotModel.Axes[0] as CategoryAxis;
            if (categoryAxis != null)
            {
                categoryAxis.ItemsSource = Enumerable.Range(0, _data.Count).Select(i => i.ToString()).ToList();
            }

            _plotModel.InvalidatePlot(true);
        }



        private async Task SortWithSteps(Func<Task> sortAlgorithm)
        {
            Logs.Text = string.Empty;
            await sortAlgorithm();
            MessageBox.Show("Sorting Completed!");
        }

        private void Log(string message)
        {
            string newLog = $"{message}\n";
            Logs.Text = newLog + Logs.Text;
            /*Logs.AppendText(message + "\n");
            Logs.ScrollToEnd();*/
        }

        private async Task BubbleSort()
        {
            var sortedIndices = new List<int>();
            for (int i = 0; i < _data.Count - 1; i++)
            {
                for (int j = 0; j < _data.Count - i - 1; j++)
                {
                    // Подсветка сравниваемых элементов
                    UpdatePlot(new List<int> { j, j + 1 }, new List<OxyColor> { OxyColors.Red, OxyColors.Orange }, sortedIndices);
                    Log($"Compare {_data[j]} and {_data[j + 1]}");
                    await Task.Delay(_delay);

                    if (_data[j] > _data[j + 1])
                    {
                        // Подсветка обмениваемых элементов
                        Log($"Swap {_data[j]} and {_data[j + 1]}");
                        (_data[j], _data[j + 1]) = (_data[j + 1], _data[j]);
                        UpdatePlot(new List<int> { j, j + 1 }, new List<OxyColor> { OxyColors.Green, OxyColors.Yellow }, sortedIndices);
                        await Task.Delay(_delay);
                    }

                    UpdatePlot(null, null, sortedIndices);
                }

                sortedIndices.Add(_data.Count - 1 - i);
            }

            sortedIndices.Add(0);
            UpdatePlot(null, null, sortedIndices);
        }




        private async Task InsertSort()
        {
            var sortedIndices = new List<int>();
            for (int i = 1; i < _data.Count; i++)
            {
                var key = _data[i];
                int j = i - 1;

                // Подсветка текущего ключа
                UpdatePlot(new List<int> { i }, new List<OxyColor> { OxyColors.Orange }, sortedIndices);
                Log($"Key: {key}");
                await Task.Delay(_delay);

                while (j >= 0 && _data[j] > key)
                {
                    // Подсветка сравниваемого элемента
                    UpdatePlot(new List<int> { j, j + 1 }, new List<OxyColor> { OxyColors.Red, OxyColors.Yellow }, sortedIndices);
                    Log($"Move {_data[j]} to position {j + 1}");
                    _data[j + 1] = _data[j];
                    j--;
                    UpdatePlot(null, null, sortedIndices);
                    await Task.Delay(_delay);
                }

                // Подсветка вставки
                Log($"Insert {key} at position {j + 1}");
                _data[j + 1] = key;
                UpdatePlot(new List<int> { j + 1 }, new List<OxyColor> { OxyColors.Green }, sortedIndices);
                await Task.Delay(_delay);

                sortedIndices.Add(j + 1);

                UpdatePlot(null, null, sortedIndices);
            }
        }



        private async Task QuickSort(int low, int high)
        {
            var sortedIndices = new List<int>();
            if (low < high)
            {
                int pi = await Partition(low, high);

                sortedIndices.Add(pi);

                await QuickSort(low, pi - 1);
                await QuickSort(pi + 1, high);
            }

            UpdatePlot(null, null, sortedIndices);
        }

        private async Task<int> Partition(int low, int high)
        {
            int pivot = _data[high];
            Log($"Pivot: {pivot}");
            int i = low - 1;

            // Подсветка опорного элемента
            UpdatePlot(new List<int> { high }, new List<OxyColor> { OxyColors.Blue }, null);
            await Task.Delay(_delay);

            for (int j = low; j < high; j++)
            {
                // Подсветка текущего элемента
                UpdatePlot(new List<int> { j, high }, new List<OxyColor> { OxyColors.Red, OxyColors.Blue }, null);
                Log($"Compare {_data[j]} and {pivot}");
                await Task.Delay(_delay);

                if (_data[j] < pivot)
                {
                    i++;
                    // Подсветка элементов, которые меняются местами
                    Log($"Swap {_data[i]} and {_data[j]}");
                    UpdatePlot(new List<int> { i, j }, new List<OxyColor> { OxyColors.Green, OxyColors.Green }, null);
                    (_data[i], _data[j]) = (_data[j], _data[i]);
                    await Task.Delay(_delay);
                }
            }

            // Подсветка обмена
            Log($"Swap {_data[i + 1]} and {pivot}");
            (_data[i + 1], _data[high]) = (_data[high], _data[i + 1]);
            UpdatePlot(new List<int> { i + 1, high }, new List<OxyColor> { OxyColors.Green, OxyColors.Green }, null);
            await Task.Delay(_delay);

            // Сброс цвета
            UpdatePlot(null, null, null);
            return i + 1;
        }



        private async Task HeapSort()
        {
            int n = _data.Count;
            var sortedIndices = new List<int>();

            for (int i = n / 2 - 1; i >= 0; i--)
            {
                await Heapify(n, i, sortedIndices);
            }

            for (int i = n - 1; i > 0; i--)
            {
                Log($"Swap {_data[0]} and {_data[i]}");
                (_data[0], _data[i]) = (_data[i], _data[0]);
                UpdatePlot(null, null, sortedIndices);
                await Task.Delay(_delay);
                await Heapify(i, 0, sortedIndices);
            }
        }

        private async Task Heapify(int n, int i, List<int> sortedIndices)
        {
            int largest = i;
            int left = 2 * i + 1;
            int right = 2 * i + 2;

            // Подсветка текущего узла
            UpdatePlot(new List<int> { i }, new List<OxyColor> { OxyColors.Blue }, sortedIndices);
            await Task.Delay(_delay);

            if (left < n && _data[left] > _data[largest])
            {
                Log($"{_data[left]} is greater than {_data[largest]}");
                largest = left;
            }

            if (right < n && _data[right] > _data[largest])
            {
                Log($"{_data[right]} is greater than {_data[largest]}");
                largest = right;
            }

            if (largest != i)
            {
                // Подсветка обмена
                Log($"Swap {_data[i]} and {_data[largest]}");
                UpdatePlot(new List<int> { i, largest }, new List<OxyColor> { OxyColors.Green, OxyColors.Green }, sortedIndices);
                (_data[i], _data[largest]) = (_data[largest], _data[i]);
                await Task.Delay(_delay);

                await Heapify(n, largest, sortedIndices);
            }

            // Добавляем элемент в отсортированные
            if (i == n - 1)
            {
                sortedIndices.Add(i);
            }

            UpdatePlot(null, null, sortedIndices);
        }



        private async void StartSorting_Click(object sender, RoutedEventArgs e)
        {
            // Получаем выбранные атрибут и метод сортировки
            //var selectedAttribute = ((ComboBoxItem)AttributeSelector.SelectedItem)?.Content?.ToString();
            var selectedMethod = ((ComboBoxItem)SortingAlgorithm.SelectedItem)?.Content?.ToString();
            var selectedAlgorithm = ((ComboBoxItem)SortingAlgorithm.SelectedItem)?.Content?.ToString();

            /*if (string.IsNullOrEmpty(selectedAttribute) || string.IsNullOrEmpty(selectedMethod))
            {
                MessageBox.Show("Please select an attribute and sorting method.");
                return;
            }*/

            if (selectedAlgorithm == "Direct Merge Sort" ||
                selectedAlgorithm == "Natural Merge Sort" ||
                selectedAlgorithm == "K-Way Merge Sort")
            {
                // Загрузка данных из файла для методов слияния
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Text Files (*.csv)|*.csv|All Files (*.*)|*.*",
                    Title = "Select a Data File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _dataRecord = LoadDataFromFile(openFileDialog.FileName);
                    if (_dataRecord == null || !_dataRecord.Any())
                    {
                        MessageBox.Show("Failed to load data from file. Please check the file format.");
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("No file selected. Please load a file for this algorithm.");
                    return;
                }
            }
            else
            {
                // Генерация случайных данных для остальных алгоритмов
                _data = GenerateRandomData(50);
            }

            UpdatePlot();

            // Функция выбора атрибута для сортировки
            /*Func<Record, IComparable> keySelector = selectedAttribute switch
            {
                "Population" => record => record.Population,
                "Area" => record => record.Area,
                "Country" => record => record.Country,
                "Continent" => record => record.Continent,
                _ => throw new InvalidOperationException("Unknown attribute")
            };*/

            // В зависимости от выбранного метода сортировки, выполняем сортировку
            switch (selectedMethod)
            {
                /*case "Direct Merge Sort":
                    await SortWithSteps(() => DirectMergeSort(_dataRecord, keySelector));
                    break;
                case "Natural Merge Sort":
                    //await SortWithSteps(() => NaturalMergeSort(_data, keySelector));
                    break;
                case "K-Way Merge Sort":
                    //await SortWithSteps(() => KWayMergeSort(_data, keySelector));
                    break;*/

                // Алгоритмы сортировки
                case "Bubble Sort":
                    await SortWithSteps(BubbleSort);
                    break;
                case "Insertion Sort":
                    await SortWithSteps(InsertSort);
                    break;
                case "Quick Sort":
                    await SortWithSteps(() => QuickSort(0, _data.Count - 1));
                    break;
                case "Heap Sort":
                    await SortWithSteps(HeapSort);
                    break;

                default:
                    MessageBox.Show("Unknown sorting method.");
                    return;
            }

            // После сортировки можно записать результат в файл
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Save Sorted Data"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                WriteDataToFile(_dataRecord, saveFileDialog.FileName);
                MessageBox.Show("Sorting completed and results saved.");
            }
        }



        private List<int> GenerateRandomData(int size)
        {
            var rand = new Random();
            var data = new List<int>();

            for (int i = 0; i < size; i++)
            {
                data.Add(rand.Next(1, 101)); // Генерация чисел от 1 до 100
            }

            return data;
        }

        private void DelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _delay = (int)DelaySlider.Value;
        }

        private List<Record> LoadDataFromFile(string filePath)
        {
            var records = new List<Record>();

            try
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in lines.Skip(1))
                {
                    // Пропускаем пустые строки
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        Log("Skipped empty line");
                        continue;
                    }

                    var columns = line.Split(',');

                    // Проверка на правильное количество столбцов
                    if (columns.Length == 5)
                    {
                        try
                        {
                            var record = new Record
                            {
                                Country = columns[0].Trim(),
                                Continent = columns[1].Trim(),
                                Capital = columns[2].Trim(),
                                Area = double.Parse(columns[3].Trim(), CultureInfo.InvariantCulture),
                                Population = (int)long.Parse(columns[4].Trim())
                            };

                            records.Add(record);
                        }
                        catch (Exception ex)
                        {
                            Log($"Error parsing line: {line} - {ex.Message}");
                            continue;
                        }
                    }
                    else
                    {
                        Log($"Invalid data format in line: {line}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }

            return records;
        }

        private async Task DirectMergeSort(List<Record> data, Func<Record, IComparable> keySelector)
        {
            if (data.Count <= 1)
                return;

            int mid = data.Count / 2;
            var left = data.Take(mid).ToList();
            var right = data.Skip(mid).ToList();

            // Рекурсивно сортируем каждую половину
            await DirectMergeSort(left, keySelector);
            await DirectMergeSort(right, keySelector);

            // Слияние двух отсортированных половин
            var merged = Merge(left, right, keySelector);
            data.Clear();
            data.AddRange(merged);

            UpdatePlot();  // Обновление графика на каждом шаге
            await Task.Delay(_delay);  // Задержка
        }

        private List<Record> Merge(List<Record> left, List<Record> right, Func<Record, IComparable> keySelector)
        {
            var result = new List<Record>();
            int i = 0, j = 0;

            while (i < left.Count && j < right.Count)
            {
                if (keySelector(left[i]).CompareTo(keySelector(right[j])) <= 0)
                {
                    result.Add(left[i]);
                    i++;
                }
                else
                {
                    result.Add(right[j]);
                    j++;
                }
            }

            result.AddRange(left.Skip(i));
            result.AddRange(right.Skip(j));

            return result;
        }

        private void WriteDataToFile(List<Record> data, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                foreach (var record in data)
                {
                    var line = $"{record.Country},{record.Continent},{record.Capital},{record.Area},{record.Population}";
                    writer.WriteLine(line);
                }
            }
        }

        private void OpenThirdWindowButton_Click(object sender, RoutedEventArgs e)
        {
            SortingWordsWindow secondWindow = new SortingWordsWindow();
            secondWindow.Show();
            // secondWindow.ShowDialog(); 
        }
    }
}