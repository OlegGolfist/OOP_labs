using System.Windows;

namespace lab4_5.Controls;

public sealed class ShopDoubleRoutedEventArgs : RoutedEventArgs
{
    public double OldValue { get; }
    public double NewValue { get; }

    public ShopDoubleRoutedEventArgs(RoutedEvent routedEvent, object source, double oldValue, double newValue)
        : base(routedEvent, source)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}

public sealed class ShopDecimalRoutedEventArgs : RoutedEventArgs
{
    public decimal OldValue { get; }
    public decimal NewValue { get; }

    public ShopDecimalRoutedEventArgs(RoutedEvent routedEvent, object source, decimal oldValue, decimal newValue)
        : base(routedEvent, source)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
