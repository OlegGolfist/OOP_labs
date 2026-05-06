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
            var uri = ResolveUri(source);
            if (uri == null)
                return null!;

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

    private static Uri? ResolveUri(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        if (source.StartsWith("pack:", StringComparison.OrdinalIgnoreCase))
            return Uri.TryCreate(source, UriKind.Absolute, out var packUri) ? packUri : null;

        // Absolute file path (supports spaces and non-latin chars).
        if (Path.IsPathRooted(source))
        {
            var full = Path.GetFullPath(source);
            if (!File.Exists(full))
                return null;
            return new Uri(full, UriKind.Absolute);
        }

        // Relative path from application folder (works after restart from bin output).
        var appRelative = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, source));
        if (File.Exists(appRelative))
            return new Uri(appRelative, UriKind.Absolute);

        // Fallback for URI-like relative values.
        return Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out var uri) ? uri : null;
    }
}
