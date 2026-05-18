using System.Windows;
using System.Windows.Controls;

namespace lab4_5.Controls;

public partial class QuantityStepperControl : UserControl
{
    public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
        nameof(Count),
        typeof(int),
        typeof(QuantityStepperControl),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCounterPropertyChanged, CoerceCount),
        ValidateInt);

    public static readonly DependencyProperty MinCountProperty = DependencyProperty.Register(
        nameof(MinCount),
        typeof(int),
        typeof(QuantityStepperControl),
        new PropertyMetadata(0, OnMinChanged),  
        ValidateInt);

    public static readonly DependencyProperty MaxCountProperty = DependencyProperty.Register(
        nameof(MaxCount),
        typeof(int),
        typeof(QuantityStepperControl),
        new PropertyMetadata(100, OnMaxChanged),  
        ValidateInt);

    public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
        nameof(Step),
        typeof(int),
        typeof(QuantityStepperControl),
        new PropertyMetadata(1),  
        ValidatePositiveInt);

    public QuantityStepperControl()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateText();
    }

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public int MinCount
    {
        get => (int)GetValue(MinCountProperty);
        set => SetValue(MinCountProperty, value);
    }

    public int MaxCount
    {
        get => (int)GetValue(MaxCountProperty);
        set => SetValue(MaxCountProperty, value);
    }

    public int Step
    {
        get => (int)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    private static bool ValidateInt(object value) => value is int;
    private static bool ValidatePositiveInt(object value) => value is int i && i > 0;

    private static object CoerceCount(DependencyObject d, object baseValue)
    {
        if (d is not QuantityStepperControl c || baseValue is not int value)
            return 0;
        if (value < c.MinCount)
            value = c.MinCount;
        if (value > c.MaxCount)
            value = c.MaxCount;
        return value;
    }

    private static void OnMinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Только корректируем Count, без взаимной коррекции Min/Max
        d.CoerceValue(CountProperty);
        OnCounterPropertyChanged(d, e);
    }

    private static void OnMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Только корректируем Count, без взаимной коррекции Min/Max
        d.CoerceValue(CountProperty);
        OnCounterPropertyChanged(d, e);
    }

    private static void OnCounterPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is QuantityStepperControl c)
            c.UpdateText();
    }

    private void OnMinusClick(object sender, RoutedEventArgs e) => Count -= Step;
    private void OnPlusClick(object sender, RoutedEventArgs e) => Count += Step;

    private void UpdateText()
    {
        CountText.Text = Count.ToString();
    }
}