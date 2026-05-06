using System.Windows.Input;

namespace lab4_5.Commands;

public static class ShopCommands
{
    public static readonly RoutedUICommand ShowRoutingInfo = new(
        "RoutingInfo",
        nameof(ShowRoutingInfo),
        typeof(ShopCommands),
        new InputGestureCollection
        {
            new KeyGesture(Key.F1)
        });
}
