using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Cursor = System.Windows.Input.Cursor;
using lab4_5.Models;
using lab4_5.ViewModels;

namespace lab4_5;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        vm.LanguageChanged += OnLanguageChanged;
        DataContext = vm;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var curPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "guitar.cur");
        if (File.Exists(curPath))
            Cursor = new Cursor(curPath);
        RefreshGridColumnHeaders();
    }

    private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e) => Close();

    private void OnLanguageChanged()
    {
        RefreshGridColumnHeaders();
    }

    private void RefreshGridColumnHeaders()
    {
        var grid = ProductsGrid;
        if (grid is null || grid.Columns.Count < 4) return;

        grid.Columns[0].Header = TryGetText("ColShort", "Название");
        grid.Columns[1].Header = TryGetText("ColCategory", "Категория");
        grid.Columns[2].Header = TryGetText("ColPrice", "Цена");
        grid.Columns[3].Header = TryGetText("ColQty", "Кол-во");
    }

    private string TryGetText(string key, string fallback) =>
        Application.Current.TryFindResource(key) as string ?? fallback;

    private void OnPickImageForSelectedProductClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedProduct is not GuitarProduct)
            return;

        var dlg = new OpenFileDialog
        {
            Title = Application.Current.TryFindResource("PickImageTitle") as string ?? "Select image",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|All files (*.*)|*.*",
            Multiselect = true
        };

        if (dlg.ShowDialog(this) != true || dlg.FileNames.Length == 0)
            return;

        TbImagesMain.Text = AppendPaths(TbImagesMain.Text, dlg.FileNames);
    }

    private static string AppendPaths(string current, IEnumerable<string> filePaths)
    {
        var merged = current
            .Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Concat(filePaths)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(";", merged);
    }
}
