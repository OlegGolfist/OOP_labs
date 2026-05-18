using System.Windows;
using System.Windows.Controls;

namespace lab4_5.Controls;

public partial class StockLevelBarControl : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(StockLevelBarControl),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVisualPropertyChanged, CoerceValue),
        ValidateFiniteDouble);

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(StockLevelBarControl),
        new PropertyMetadata(50d, OnMaximumChanged, CoerceMaximum),
        ValidatePositiveFiniteDouble);

    public static readonly DependencyProperty BarHeightProperty = DependencyProperty.Register(
        nameof(BarHeight),
        typeof(double),
        typeof(StockLevelBarControl),
        new PropertyMetadata(14d, OnVisualPropertyChanged, CoerceBarHeight),
        ValidateFiniteDouble);

    public StockLevelBarControl()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            BarHost.SizeChanged += (_, _) => UpdateBar();
            UpdateBar();
        };
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double BarHeight
    {
        get => (double)GetValue(BarHeightProperty);
        set => SetValue(BarHeightProperty, value);
    }

    private static bool ValidateFiniteDouble(object value) =>
        value is double d && !double.IsNaN(d) && !double.IsInfinity(d);

    private static bool ValidatePositiveFiniteDouble(object value) =>
        value is double d && d > 0 && !double.IsNaN(d) && !double.IsInfinity(d);

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        if (d is not StockLevelBarControl c || baseValue is not double value)
            return 0d;
        if (value < 0d)
            value = 0d;
        if (value > c.Maximum)
            value = c.Maximum;

        return value;
    }

    private static object CoerceMaximum(DependencyObject d, object baseValue)
    {
        if (baseValue is not double value)
            return 25d;
        if (value < 1d)
            return 1d;
        if (value > 50d)
            return 50d;

        return value;
    }

    private static object CoerceBarHeight(DependencyObject d, object baseValue)
    {
        if (baseValue is not double value)
            return 14d;
        if (value < 8d)
            return 8d;
        if (value > 40d)
            return 40d;

        return value;
    }

    private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        d.CoerceValue(ValueProperty);
        OnVisualPropertyChanged(d, e);
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StockLevelBarControl control)
            control.UpdateBar();
    }

    private void UpdateBar()
    {
        if (!IsLoaded)
            return;

        var width = BarHost.ActualWidth;
        if (width <= 0d)
            return;

        var ratio = Maximum <= 0d ? 0d : Value / Maximum;
        ratio = Math.Clamp(ratio, 0d, 1d);

        BarFill.Width = width * ratio;
        ValueText.Text = $"{Value:0.##} / {Maximum:0.##}";
    }
}
