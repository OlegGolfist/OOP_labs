using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Cursor = System.Windows.Input.Cursor;
using lab4_5.Commands;
using lab4_5.Controls;
using lab4_5.Models;
using lab4_5.ViewModels;

namespace lab4_5;

public partial class MainWindow : Window
{
    private bool _routingHandlersAttached;

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
        AttachRoutingHandlers();
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

    private void AttachRoutingHandlers()
    {
        if (_routingHandlersAttached || RoutingHost is null)
            return;
        _routingHandlersAttached = true;

        AddHandler(RatingStarsPicker.PreviewRatingChangingEvent, new RoutedEventHandler((s, e) => WriteRouting("Window", "Tunnel", e)), false);
        AddHandler(RatingStarsPicker.RatingChangingEvent, new RoutedEventHandler((s, e) => WriteRouting("Window", "Bubble", e)), false);
        AddHandler(DiscountBarControl.PreviewDiscountChangingEvent, new RoutedEventHandler((s, e) => WriteRouting("Window", "Tunnel", e)), false);
        AddHandler(DiscountBarControl.DiscountChangingEvent, new RoutedEventHandler((s, e) => WriteRouting("Window", "Bubble", e)), false);

        RoutingHost.AddHandler(RatingStarsPicker.PreviewRatingChangingEvent, new RoutedEventHandler((s, e) => WriteRouting("Host", "Tunnel", e)), false);
        RoutingHost.AddHandler(RatingStarsPicker.RatingChangingEvent, new RoutedEventHandler((s, e) => WriteRouting("Host", "Bubble", e)), false);
        RoutingHost.AddHandler(DiscountBarControl.PreviewDiscountChangingEvent, new RoutedEventHandler((s, e) => WriteRouting("Host", "Tunnel", e)), false);
        RoutingHost.AddHandler(DiscountBarControl.DiscountChangingEvent, new RoutedEventHandler((s, e) => WriteRouting("Host", "Bubble", e)), false);
    }

    private void WriteRouting(string place, string strategy, RoutedEventArgs e)
    {
        var detail = e switch
        {
            ShopDoubleRoutedEventArgs a => $"rating {a.OldValue:0.#}->{a.NewValue:0.#}",
            ShopDecimalRoutedEventArgs a => $"discount {a.OldValue:0.#}%->{a.NewValue:0.#}%",
            _ => "event"
        };

        RoutingLogText.Text = $"{strategy} @ {place}: {detail}";
    }

    private void OnRoutingInfoExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            TryGetText("RoutingInfoBody", "Tunnel goes top-down, Bubble bottom-up, Direct only on source control."),
            TryGetText("RoutingInfoTitle", "Routing info"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

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
