using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace lab4_5.Converters;

public class UriToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
            return null!;

        try
        {
            var source = s.Trim().Trim('"');
            Uri uri;

            if (Path.IsPathRooted(source))
            {
                if (!File.Exists(source))
                    return null!;
                uri = new Uri(source, UriKind.Absolute);
            }
            else if (source.StartsWith("pack:", StringComparison.OrdinalIgnoreCase))
            {
                uri = new Uri(source, UriKind.Absolute);
            }
            else
            {
                uri = new Uri(source, UriKind.RelativeOrAbsolute);
            }

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null!;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
