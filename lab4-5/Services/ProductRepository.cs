using System.IO;
using System.Text.Json;
using lab4_5.Models;

namespace lab4_5.Services;

public class ProductRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _filePath;

    public ProductRepository()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "products.json");
    }

    public List<GuitarProduct> Load()
    {
        if (!File.Exists(_filePath))
        {
            var sample = CreateSample();
            Save(sample);
            return sample;
        }

        var json = File.ReadAllText(_filePath);
        var list = JsonSerializer.Deserialize<List<GuitarProduct>>(json, JsonOptions);
        return list ?? new List<GuitarProduct>();
    }

    public void Save(IEnumerable<GuitarProduct> products)
    {
        var json = JsonSerializer.Serialize(products.ToList(), JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private static List<GuitarProduct> CreateSample()
    {
        var packImg = "pack://application:,,,/Assets/app.ico";
        return new List<GuitarProduct>
        {
            new()
            {
                ShortName = "Strat Lite",
                FullName = "Электрогитара Strat Lite SSS",
                Description = "Три сингла, удобный гриф.",
                Category = "Электро",
                Rating = 4.5,
                Price = 18900,
                Quantity = 5,
                Color = "Sunburst",
                Size = "полный",
                DeliveryCountry = "Россия",
                DiscountPercent = 10,
                IsOutOfStock = false,
                PurchasedCount = 12,
                Manufacturer = "GuitarLab",
                ImagePaths = new List<string> { packImg }
            },
            new()
            {
                ShortName = "D-28 Style",
                FullName = "Акустика Dreadnought",
                Description = "Яркий верх из ели.",
                Category = "Акустика",
                Rating = 4.8,
                Price = 24500,
                Quantity = 2,
                Color = "Natural",
                Size = "полный",
                DeliveryCountry = "Россия",
                DiscountPercent = 0,
                IsOutOfStock = false,
                PurchasedCount = 7,
                Manufacturer = "WoodSong",
                ImagePaths = new List<string> { packImg }
            },
            new()
            {
                ShortName = "Combo 15W",
                FullName = "Комбоусилитель 15 Вт",
                Description = "Для дома и репетиций.",
                Category = "Усилители",
                Rating = 4.2,
                Price = 8900,
                Quantity = 0,
                Color = "Чёрный",
                Size = "компакт",
                DeliveryCountry = "Китай",
                DiscountPercent = 15,
                IsOutOfStock = true,
                PurchasedCount = 30,
                Manufacturer = "AmpMini",
                ImagePaths = new List<string> { packImg }
            }
        };
    }
}
