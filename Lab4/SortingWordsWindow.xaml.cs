using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Lab4
{
    public partial class SortingWordsWindow : Window
    {
        public SortingWordsWindow()
        {
            InitializeComponent();
        }

        // Базовая сортировка (например, с использованием LINQ)
        private async void BaseSortButton_Click(object sender, RoutedEventArgs e)
        {
            var words = ExtractWords(InputTextBox.Text);
            if (words.Count == 0)
            {
                MessageBox.Show("No words to sort!");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var sortedWords = words.OrderBy(w => w).ToList();
            stopwatch.Stop();

            var results = CountWords(sortedWords);
            DisplayResults(results, stopwatch.ElapsedMilliseconds);
        }

        // Radix Sort для строк
        private async void RadixSortButton_Click(object sender, RoutedEventArgs e)
        {
            var words = ExtractWords(InputTextBox.Text);
            if (words.Count == 0)
            {
                MessageBox.Show("No words to sort!");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var sortedWords = RadixSort(words);
            stopwatch.Stop();

            var results = CountWords(sortedWords);
            DisplayResults(results, stopwatch.ElapsedMilliseconds);
        }

        // Запуск экспериментов
        private async void RunExperimentsButton_Click(object sender, RoutedEventArgs e)
        {
            var experimentSizes = new[] { 100, 500, 1000, 2000, 5000, 100000, 200000, 500000, 1000000 };
            var results = new List<WordResult>();

            foreach (var size in experimentSizes)
            {
                var words = GenerateRandomWords(size);

                // Базовая сортировка
                var stopwatch = Stopwatch.StartNew();
                var baseSortedWords = words.OrderBy(w => w).ToList();
                stopwatch.Stop();
                results.Add(new WordResult
                {
                    Word = $"BaseSort {size} words",
                    Count = size,
                    SortTime = stopwatch.ElapsedMilliseconds
                });

                // Radix сортировка
                stopwatch.Restart();
                var radixSortedWords = RadixSort(words);
                stopwatch.Stop();
                results.Add(new WordResult
                {
                    Word = $"RadixSort {size} words",
                    Count = size,
                    SortTime = stopwatch.ElapsedMilliseconds
                });
            }

            ResultsTable.ItemsSource = results;
        }

        // Извлечение слов из текста
        private List<string> ExtractWords(string text)
        {
            return Regex.Matches(text.ToLower(), @"\b[a-z]+\b")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToList();
        }

        // Подсчет слов
        private List<WordResult> CountWords(List<string> words)
        {
            return words.GroupBy(w => w)
                        .Select(g => new WordResult
                        {
                            Word = g.Key,
                            Count = g.Count()
                        })
                        .ToList();
        }

        // Реализация Radix Sort для строк
        private List<string> RadixSort(List<string> words)
        {
            int maxLength = words.Max(w => w.Length);
            for (int i = maxLength - 1; i >= 0; i--)
            {
                words = words.OrderBy(w => i < w.Length ? w[i] : char.MinValue).ToList();
            }
            return words;
        }

        // Генерация случайных слов
        private List<string> GenerateRandomWords(int count)
        {
            var random = new Random();
            var words = new List<string>();
            for (int i = 0; i < count; i++)
            {
                int length = random.Next(3, 10);
                words.Add(new string(Enumerable.Range(0, length).Select(_ => (char)random.Next('a', 'z' + 1)).ToArray()));
            }
            return words;
        }

        // Отображение результатов
        private void DisplayResults(List<WordResult> results, long sortTime)
        {
            foreach (var result in results)
            {
                result.SortTime = sortTime;
            }

            ResultsTable.ItemsSource = results;
        }
    }
    public class WordResult
    {
        public string Word { get; set; }
        public int Count { get; set; }
        public long SortTime { get; set; }
    }

}
