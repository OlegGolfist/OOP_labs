using System.Windows;
using System.Windows.Controls;

namespace lab4_5.Controls;

public partial class DiscountBarControl : UserControl
{
    private bool _syncing;

    public static readonly DependencyProperty DiscountPercentProperty = DependencyProperty.Register(
        nameof(DiscountPercent),
        typeof(decimal),
        typeof(DiscountBarControl),
        new FrameworkPropertyMetadata(0m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDiscountChanged, CoerceDiscount),
        ValidateDiscount);

    public static readonly DependencyProperty SnapStepProperty = DependencyProperty.Register(
        nameof(SnapStep),
        typeof(decimal),
        typeof(DiscountBarControl),
        new PropertyMetadata(1m, OnSnapStepChanged, CoerceSnapStep),
        ValidateSnapStep);

    public static readonly DependencyProperty BarHeightProperty = DependencyProperty.Register(
        nameof(BarHeight),
        typeof(double),
        typeof(DiscountBarControl),
        new PropertyMetadata(16d, null, CoerceBarHeight),
        ValidateBarHeight);

    public static readonly RoutedEvent PreviewDiscountChangingEvent = EventManager.RegisterRoutedEvent(
        nameof(PreviewDiscountChanging), RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(DiscountBarControl));
    public static readonly RoutedEvent DiscountChangingEvent = EventManager.RegisterRoutedEvent(
        nameof(DiscountChanging), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DiscountBarControl));
    public static readonly RoutedEvent DiscountChangedDirectEvent = EventManager.RegisterRoutedEvent(
        nameof(DiscountChangedDirect), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(DiscountBarControl));

    public DiscountBarControl()
    {
        InitializeComponent();
        AddHandler(DiscountChangedDirectEvent, new RoutedEventHandler(OnDirect));
        Loaded += (_, _) =>
        {
            BarGrid.SizeChanged += (_, _) => PaintBar();
            SyncSlider();
            PaintBar();
            PercentText.Text = $"{DiscountPercent:0.#}%";
        };
    }

    public event RoutedEventHandler PreviewDiscountChanging { add => AddHandler(PreviewDiscountChangingEvent, value); remove => RemoveHandler(PreviewDiscountChangingEvent, value); }
    public event RoutedEventHandler DiscountChanging { add => AddHandler(DiscountChangingEvent, value); remove => RemoveHandler(DiscountChangingEvent, value); }
    public event RoutedEventHandler DiscountChangedDirect { add => AddHandler(DiscountChangedDirectEvent, value); remove => RemoveHandler(DiscountChangedDirectEvent, value); }

    public decimal DiscountPercent
    {
        get => (decimal)GetValue(DiscountPercentProperty);
        set => SetValue(DiscountPercentProperty, value);
    }

    public decimal SnapStep
    {
        get => (decimal)GetValue(SnapStepProperty);
        set => SetValue(SnapStepProperty, value);
    }

    public double BarHeight
    {
        get => (double)GetValue(BarHeightProperty);
        set => SetValue(BarHeightProperty, value);
    }

    private static bool ValidateDiscount(object value) => value is decimal;
    private static bool ValidateSnapStep(object value) => value is decimal d && d > 0m && d <= 25m;
    private static bool ValidateBarHeight(object value) => value is double d && !double.IsNaN(d) && !double.IsInfinity(d);

    private static object CoerceDiscount(DependencyObject d, object baseValue)
    {
        if (d is not DiscountBarControl c || baseValue is not decimal v) return 0m;
        if (v < 0m) v = 0m;
        if (v > 100m) v = 100m;
        var step = c.SnapStep <= 0m ? 1m : c.SnapStep;
        var snapped = Math.Round(v / step, MidpointRounding.AwayFromZero) * step;
        if (snapped < 0m) snapped = 0m;
        if (snapped > 100m) snapped = 100m;
        return decimal.Round(snapped, 2);
    }

    private static object CoerceSnapStep(DependencyObject d, object baseValue)
    {
        if (baseValue is not decimal v) return 1m;
        if (v < 0.5m) return 0.5m;
        if (v > 25m) return 25m;
        return v;
    }

    private static object CoerceBarHeight(DependencyObject d, object baseValue)
    {
        if (baseValue is not double v) return 16d;
        if (v < 8d) return 8d;
        if (v > 40d) return 40d;
        return v;
    }

    private static void OnSnapStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => d.CoerceValue(DiscountPercentProperty);
    private static void OnDiscountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DiscountBarControl c) return;
        c.SyncSlider();
        c.PaintBar();
        c.PercentText.Text = $"{c.DiscountPercent:0.#}%";
    }

    private void SyncSlider()
    {
        _syncing = true;
        DiscountSlider.Value = (double)DiscountPercent;
        _syncing = false;
    }

    private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || !IsLoaded) return;
        var oldVal = DiscountPercent;
        var raw = (decimal)e.NewValue;

        var preview = new ShopDecimalRoutedEventArgs(PreviewDiscountChangingEvent, this, oldVal, raw);
        RaiseEvent(preview);
        if (preview.Handled)
        {
            SyncSlider();
            return;
        }

        DiscountPercent = raw;
        RaiseEvent(new ShopDecimalRoutedEventArgs(DiscountChangingEvent, this, oldVal, DiscountPercent));
        RaiseEvent(new ShopDecimalRoutedEventArgs(DiscountChangedDirectEvent, this, oldVal, DiscountPercent));
    }

    private void PaintBar()
    {
        var w = BarGrid.ActualWidth;
        if (w <= 0) return;
        FillRect.Width = w * (double)(DiscountPercent / 100m);
    }

    private void OnDirect(object sender, RoutedEventArgs e)
    {
        if (e is not ShopDecimalRoutedEventArgs a) return;
        DirectText.Text = $"Direct: {a.OldValue:0.#}% -> {a.NewValue:0.#}%";
        DirectText.Visibility = Visibility.Visible;
    }
}
