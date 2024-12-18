using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Lab4
{
    /// <summary>
    /// Логика взаимодействия для EtcSorts.xaml
    /// </summary>
    public partial class EtcSorts : Window
    {
        public EtcSorts()
        {
            InitializeComponent();
            InitializePlot();
        }

        //private List<int> _data;
        private List<Record> _dataRecord;
        private int _delay = 500; // Задержка по умолчанию (в мс)
        private PlotModel _plotModel;

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

        private void UpdatePlot()
        {
            var barSeries = (BarSeries)_plotModel.Series[0];

            // Получаем выбранный атрибут
            var selectedAttribute = ((ComboBoxItem)AttributeSelector.SelectedItem)?.Content?.ToString();

            if (string.IsNullOrEmpty(selectedAttribute))
            {
                MessageBox.Show("Please select an attribute to visualize.");
                return;
            }

            // Обновление данных в зависимости от выбранного атрибута
            barSeries.ItemsSource = _dataRecord.ConvertAll(record =>
            {
                return new BarItem
                {
                    Value = selectedAttribute switch
                    {
                        "Population" => record.Population,
                        "Area" => record.Area,
                        "Country" => ConvertToDouble(record.Country),
                        "Continent" => ConvertToDouble(record.Continent),
                        _ => 0
                    }
                };
            });

            // Обновление категорий
            var categoryAxis = _plotModel.Axes[0] as CategoryAxis;
            if (categoryAxis != null)
            {
                categoryAxis.ItemsSource = _dataRecord.Select(record =>
                    selectedAttribute switch
                    {
                        "Country" => record.Country,
                        "Continent" => record.Continent,
                        "Capital" => record.Capital,
                        _ => string.Empty
                    }).ToList();
            }

            _plotModel.InvalidatePlot(true);
        }

        // Преобразование строки в число (например, длина строки или ASCII-код)
        private double ConvertToDouble(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Например, используем длину строки
            return text.Length;
        }

        private async Task SortWithSteps(Func<Task> sortAlgorithm)
        {
            Logs.Text = string.Empty;
            await sortAlgorithm();
            MessageBox.Show("Sorting Completed!");
        }

        private void Log(string message)
        {
            Logs.AppendText(message + "\n");
            Logs.ScrollToEnd();
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

        private void DelaySlider_ValueChanged2(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _delay = (int)DelaySlider2.Value;
        }

        private List<Record> LoadDataFromFile(string filePath)
        {
            var records = new List<Record>();

            try
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8); // Убедитесь, что используете правильную кодировку
                foreach (var line in lines.Skip(1)) // Пропускаем заголовок
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
                                Country = columns[0].Trim(), // Убираем лишние пробелы
                                Continent = columns[1].Trim(),
                                Capital = columns[2].Trim(),
                                Area = double.Parse(columns[3].Trim(), CultureInfo.InvariantCulture), // Преобразуем строку в число с учетом культуры
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

        private async Task NaturalMergeSort(List<Record> data, Func<Record, IComparable> keySelector)
        {
            if (data.Count <= 1)
                return;

            bool sorted = false;
            while (!sorted)
            {
                sorted = true;
                var runs = new List<List<Record>>();
                var currentRun = new List<Record> { data[0] };

                // Разбиваем данные на естественные подпоследовательности
                for (int i = 1; i < data.Count; i++)
                {
                    if (keySelector(data[i]).CompareTo(keySelector(data[i - 1])) >= 0)
                    {
                        currentRun.Add(data[i]);
                    }
                    else
                    {
                        runs.Add(currentRun);
                        currentRun = new List<Record> { data[i] };
                        sorted = false;
                    }
                }

                runs.Add(currentRun);

                if (runs.Count > 1)
                {
                    // Объединяем последовательности
                    var mergedData = MergeRuns(runs, keySelector);
                    data.Clear();
                    data.AddRange(mergedData);

                    UpdatePlot(); // Обновление графика
                    await Task.Delay(_delay); // Задержка
                }
            }
        }

        private List<Record> MergeRuns(List<List<Record>> runs, Func<Record, IComparable> keySelector)
        {
            var result = new List<Record>();
            foreach (var run in runs)
            {
                result = Merge(result, run, keySelector);
            }
            return result;
        }

        private async Task KWayMergeSort(List<Record> data, Func<Record, IComparable> keySelector, int k = 4)
        {
            if (data.Count <= 1)
                return;

            // Разбиваем данные на k частей
            var partitions = new List<List<Record>>();
            int partitionSize = (int)Math.Ceiling((double)data.Count / k);
            for (int i = 0; i < data.Count; i += partitionSize)
            {
                partitions.Add(data.Skip(i).Take(partitionSize).ToList());
            }

            // Рекурсивно сортируем каждую часть
            foreach (var partition in partitions)
            {
                await DirectMergeSort(partition, keySelector);
            }

            // Объединяем k частей
            var mergedData = KWayMerge(partitions, keySelector);
            data.Clear();
            data.AddRange(mergedData);

            UpdatePlot(); // Обновление графика
            await Task.Delay(_delay); // Задержка
        }

        private List<Record> KWayMerge(List<List<Record>> partitions, Func<Record, IComparable> keySelector)
        {
            var result = new List<Record>();
            var indices = new int[partitions.Count]; // Текущие индексы для каждой части

            while (true)
            {
                int minIndex = -1;
                Record minValue = null;

                // Ищем минимальный элемент среди всех частей
                for (int i = 0; i < partitions.Count; i++)
                {
                    if (indices[i] < partitions[i].Count)
                    {
                        var currentValue = partitions[i][indices[i]];
                        if (minValue == null || keySelector(currentValue).CompareTo(keySelector(minValue)) < 0)
                        {
                            minValue = currentValue;
                            minIndex = i;
                        }
                    }
                }

                if (minIndex == -1)
                    break; // Все части обработаны

                result.Add(minValue);
                indices[minIndex]++;
            }

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

        private async void StartSorting_Click2(object sender, RoutedEventArgs e)
        {
            // Получаем выбранные атрибут и метод сортировки
            var selectedAttribute = ((ComboBoxItem)AttributeSelector.SelectedItem)?.Content?.ToString();
            var selectedMethod = ((ComboBoxItem)SortingAlgorithm.SelectedItem)?.Content?.ToString();

            if (string.IsNullOrEmpty(selectedAttribute) || string.IsNullOrEmpty(selectedMethod))
            {
                MessageBox.Show("Please select an attribute and sorting method.");
                return;
            }

            if (selectedMethod == "Direct Merge Sort" ||
                selectedMethod == "Natural Merge Sort" ||
                selectedMethod == "K-Way Merge Sort")
            {
                // Загрузка данных из файла
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

            UpdatePlot();

            // Функция выбора атрибута для сортировки
            Func<Record, IComparable> keySelector = selectedAttribute switch
            {
                "Population" => record => record.Population,
                "Area" => record => record.Area,
                "Capital" => record => record.Capital,
                "Country" => record => record.Country,
                "Continent" => record => record.Continent,
                _ => throw new InvalidOperationException("Unknown attribute")
            };

            // В зависимости от выбранного метода сортировки, выполняем сортировку
            switch (selectedMethod)
            {
                case "Direct Merge Sort":
                    await SortWithSteps(() => DirectMergeSort(_dataRecord, keySelector));
                    break;
                case "Natural Merge Sort":
                    await SortWithSteps(() => NaturalMergeSort(_dataRecord, keySelector));
                    break;
                case "K-Way Merge Sort":
                    await SortWithSteps(() => KWayMergeSort(_dataRecord, keySelector));
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
    }

    public class Record
    {
        public string Country { get; set; }
        public string Continent { get; set; }
        public string Capital { get; set; }
        public double Area { get; set; }
        public int Population { get; set; }
    }
}


