using System.Windows;
using System.Windows.Controls;

namespace lab4_5.Controls;

public partial class StockBadge : UserControl
{
    public static readonly DependencyProperty QuantityProperty =
        DependencyProperty.Register(nameof(Quantity), typeof(int), typeof(StockBadge),
            new PropertyMetadata(0, OnQuantityChanged));

    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(StockBadge),
            new PropertyMetadata(""));

    public int Quantity
    {
        get => (int)GetValue(QuantityProperty);
        set => SetValue(QuantityProperty, value);
    }

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    public StockBadge()
    {
        InitializeComponent();
    }

    private static void OnQuantityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not StockBadge badge)
            return;
        badge.LabelText = $"{badge.FindResource("ColQty") ?? "Qty"}: {badge.Quantity}";
    }
}
