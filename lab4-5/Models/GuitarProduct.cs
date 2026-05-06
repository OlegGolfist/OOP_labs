using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace lab4_5.Models;

public class GuitarProduct : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();

    private string _shortName = "";
    private string _fullName = "";
    private string _description = "";
    private List<string> _imagePaths = new();
    private string _category = "Электро";
    private double _rating;
    private decimal _price;
    private int _quantity;
    private string _color = "";
    private string _size = "";
    private string _deliveryCountry = "Россия";
    private decimal _discountPercent;
    private bool _isOutOfStock;
    private int _purchasedCount;
    private string _manufacturer = "";

    public string ShortName
    {
        get => _shortName;
        set => SetField(ref _shortName, value);
    }

    public string FullName
    {
        get => _fullName;
        set => SetField(ref _fullName, value);
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    public List<string> ImagePaths
    {
        get => _imagePaths;
        set
        {
            if (!SetField(ref _imagePaths, value))
                return;
            OnPropertyChanged(nameof(ImagesText));
        }
    }

    [JsonIgnore]
    public string ImagesText
    {
        get => string.Join(";", ImagePaths);
        set
        {
            ImagePaths = value
                .Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(path => path.Trim().Trim('"'))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList();
            OnPropertyChanged();
        }
    }

    public string Category
    {
        get => _category;
        set => SetField(ref _category, value);
    }

    public double Rating
    {
        get => _rating;
        set => SetField(ref _rating, value);
    }

    public decimal Price
    {
        get => _price;
        set
        {
            if (!SetField(ref _price, value))
                return;
            OnPropertyChanged(nameof(PriceWithDiscount));
        }
    }

    public int Quantity
    {
        get => _quantity;
        set => SetField(ref _quantity, value);
    }

    public string Color
    {
        get => _color;
        set => SetField(ref _color, value);
    }

    public string Size
    {
        get => _size;
        set => SetField(ref _size, value);
    }

    public string DeliveryCountry
    {
        get => _deliveryCountry;
        set => SetField(ref _deliveryCountry, value);
    }

    public decimal DiscountPercent
    {
        get => _discountPercent;
        set
        {
            if (!SetField(ref _discountPercent, value))
                return;
            OnPropertyChanged(nameof(PriceWithDiscount));
        }
    }

    [JsonIgnore]
    public decimal PriceWithDiscount
    {
        get
        {
            var discount = Math.Clamp(DiscountPercent, 0m, 100m);
            var factor = 1m - discount / 100m;
            return decimal.Round(Price * factor, 2);
        }
    }

    public bool IsOutOfStock
    {
        get => _isOutOfStock;
        set => SetField(ref _isOutOfStock, value);
    }

    public int PurchasedCount
    {
        get => _purchasedCount;
        set => SetField(ref _purchasedCount, value);
    }

    public string Manufacturer
    {
        get => _manufacturer;
        set => SetField(ref _manufacturer, value);
    }

    public GuitarProduct Clone()
    {
        return new GuitarProduct
        {
            Id = Id,
            ShortName = ShortName,
            FullName = FullName,
            Description = Description,
            ImagePaths = new List<string>(ImagePaths),
            Category = Category,
            Rating = Rating,
            Price = Price,
            Quantity = Quantity,
            Color = Color,
            Size = Size,
            DeliveryCountry = DeliveryCountry,
            DiscountPercent = DiscountPercent,
            IsOutOfStock = IsOutOfStock,
            PurchasedCount = PurchasedCount,
            Manufacturer = Manufacturer
        };
    }

    public static ObservableCollection<string> Categories { get; } = new()
    {
        "Электро",
        "Акустика",
        "Классика",
        "Бас",
        "Усилители",
        "Аксессуары"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
