using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace lab4_5.Models;

public class GuitarProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ShortName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Description { get; set; } = "";

    public List<string> ImagePaths { get; set; } = new();

    [JsonIgnore]
    public string ImagesText
    {
        get => string.Join(";", ImagePaths);
        set => ImagePaths = value
            .Split(new[] { ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(path => path.Trim().Trim('"'))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList();
    }

    public string Category { get; set; } = "Электро";
 
    public double Rating { get; set; }

    public decimal Price { get; set; }

    public int Quantity { get; set; }

    public string Color { get; set; } = "";
    public string Size { get; set; } = "";
    public string DeliveryCountry { get; set; } = "Россия";
    public decimal DiscountPercent { get; set; }
    public bool IsOutOfStock { get; set; }
    public int PurchasedCount { get; set; }
    public string Manufacturer { get; set; } = "";

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
}
