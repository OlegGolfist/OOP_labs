using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace lab4_5.Models;

public class CartItem : INotifyPropertyChanged
{
    private int _quantity;

    public required GuitarProduct Product { get; init; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            var bounded = Math.Clamp(value, 1, Product.Quantity <= 0 ? 1 : Product.Quantity);
            if (_quantity == bounded)
                return;
            _quantity = bounded;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
        }
    }

    public int MaxQuantity => Math.Max(Product.Quantity, 1);

    public decimal LineTotal => Product.PriceWithDiscount * Quantity;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
