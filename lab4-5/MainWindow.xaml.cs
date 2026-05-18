using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media;
using Cursor = System.Windows.Input.Cursor;
using lab4_5.Commands;
using lab4_5.Models;
using lab4_5.ViewModels;

namespace lab4_5;

public partial class MainWindow : Window
{
    public static readonly RoutedEvent ProductCardHighlightEvent = EventManager.RegisterRoutedEvent(
        "ProductCardHighlight",
        RoutingStrategy.Direct,
        typeof(RoutedEventHandler),
        typeof(MainWindow));

    public static readonly RoutedEvent PreviewProductOpenEvent = EventManager.RegisterRoutedEvent(
        "PreviewProductOpen",
        RoutingStrategy.Tunnel,
        typeof(RoutedEventHandler),
        typeof(MainWindow));

    public static readonly RoutedEvent AddToCartDeniedEvent = EventManager.RegisterRoutedEvent(
        "AddToCartDenied",
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(MainWindow));

    private Border? _highlightedCard;

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

        AddHandler(PreviewProductOpenEvent, new RoutedEventHandler(OnPreviewProductOpen));
        AddHandler(AddToCartDeniedEvent, new RoutedEventHandler(OnAddToCartDenied));
    }

    private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e) => Close();

    private void OnLanguageChanged()
    {
    }

    private void OnRoutingInfoExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "текст вызван с помощью routed ui command.",
            "routed UI",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnProductCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border cardBorder || cardBorder.DataContext is not GuitarProduct product)
            return;

        var previewArgs = new ProductRoutedEventArgs(PreviewProductOpenEvent, cardBorder, product);
        cardBorder.RaiseEvent(previewArgs);
        if (previewArgs.Handled)
            return;

        cardBorder.RaiseEvent(new RoutedEventArgs(ProductCardHighlightEvent, cardBorder));

        var text =
            $"{product.FullName}\n" +
            $"{product.Description}\n\n" +
            $"{(Application.Current.TryFindResource("CategoryLabel") as string ?? "Category")}: {product.Category}\n" +
            $"{(Application.Current.TryFindResource("ColPrice") as string ?? "Price")}: {product.PriceWithDiscount:N2}\n" +
            $"{(Application.Current.TryFindResource("ColQty") as string ?? "Qty")}: {product.Quantity}\n" +
            $"{(Application.Current.TryFindResource("LblMaker") as string ?? "Manufacturer")}: {product.Manufacturer}\n" +
            $"{(Application.Current.TryFindResource("LblCountry") as string ?? "Delivery country")}: {product.DeliveryCountry}";

        MessageBox.Show(this, text, product.ShortName, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnAddImageToProductClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: GuitarProduct product })
            return;

        var dlg = new OpenFileDialog
        {
            Title = Application.Current.TryFindResource("PickImageTitle") as string ?? "Select image",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|All files (*.*)|*.*",
            Multiselect = false
        };

        if (dlg.ShowDialog(this) != true || string.IsNullOrWhiteSpace(dlg.FileName))
            return;

        var paths = product.ImagePaths.ToList();
        paths.Add(dlg.FileName);
        product.ImagePaths = paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void OnProductCardLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Border cardBorder)
            return;

        cardBorder.AddHandler(ProductCardHighlightEvent, new RoutedEventHandler(OnProductCardHighlight));
    }

    private void OnProductCardHighlight(object sender, RoutedEventArgs e)
    {
        if (sender is not Border cardBorder)
            return;

        _highlightedCard?.ClearValue(BackgroundProperty);
        cardBorder.Background = new SolidColorBrush(Color.FromRgb(198, 239, 206));
        _highlightedCard = cardBorder;
    }   

    private void OnAddToCartClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: GuitarProduct product } button || DataContext is not MainViewModel vm)
            return;

        var alreadyInCart = vm.CartItems
            .Where(x => x.Product.Id == product.Id)
            .Select(x => x.Quantity)
            .FirstOrDefault();

        if (alreadyInCart >= product.Quantity)
        {
            button.RaiseEvent(new ProductRoutedEventArgs(AddToCartDeniedEvent, button, product));
            return;
        }

        vm.AddToCartCommand.Execute(product);
    }

    private void OnAddToCartDenied(object sender, RoutedEventArgs e)
    {
        if (e is not ProductRoutedEventArgs args || args.Product is null)
            return;

        MessageBox.Show(
            this,
            $"Нельзя добавить в корзину больше, чем есть на складе.\nТовар: {args.Product.ShortName}",
            "Bubbling event",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void OnPreviewProductOpen(object sender, RoutedEventArgs e)
    {
        if (e is not ProductRoutedEventArgs args || args.Product is null)
            return;

        if (args.Product.Quantity > 0)
            return;

        MessageBox.Show(
            this,
            $"Tunneling event: карточка не открывается, потому что товара нет в наличии.\nТовар: {args.Product.ShortName}",
            "Tunneling event",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        args.Handled = true;
    }

    private sealed class ProductRoutedEventArgs : RoutedEventArgs
    {
        public GuitarProduct? Product { get; }

        public ProductRoutedEventArgs(RoutedEvent routedEvent, object source, GuitarProduct? product)
            : base(routedEvent, source)
        {
            Product = product;
        }
    }
}
