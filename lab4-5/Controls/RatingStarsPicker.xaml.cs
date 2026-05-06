using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace lab4_5.Controls;

public partial class RatingStarsPicker : UserControl
{
    public static readonly DependencyProperty RatingProperty = DependencyProperty.Register(
        nameof(Rating),
        typeof(double),
        typeof(RatingStarsPicker),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRatingChanged, CoerceRating),
        ValidateRating);

    public static readonly DependencyProperty MaxRatingProperty = DependencyProperty.Register(
        nameof(MaxRating),
        typeof(double),
        typeof(RatingStarsPicker),
        new PropertyMetadata(5d, OnMaxRatingChanged, CoerceMaxRating),
        ValidateMaxRating);

    public static readonly RoutedEvent PreviewRatingChangingEvent = EventManager.RegisterRoutedEvent(
        nameof(PreviewRatingChanging), RoutingStrategy.Tunnel, typeof(RoutedEventHandler), typeof(RatingStarsPicker));
    public static readonly RoutedEvent RatingChangingEvent = EventManager.RegisterRoutedEvent(
        nameof(RatingChanging), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RatingStarsPicker));
    public static readonly RoutedEvent RatingChangedDirectEvent = EventManager.RegisterRoutedEvent(
        nameof(RatingChangedDirect), RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(RatingStarsPicker));

    public RatingStarsPicker()
    {
        InitializeComponent();
        AddHandler(RatingChangedDirectEvent, new RoutedEventHandler(OnDirect));
        Loaded += (_, _) => PaintStars();
    }

    public event RoutedEventHandler PreviewRatingChanging { add => AddHandler(PreviewRatingChangingEvent, value); remove => RemoveHandler(PreviewRatingChangingEvent, value); }
    public event RoutedEventHandler RatingChanging { add => AddHandler(RatingChangingEvent, value); remove => RemoveHandler(RatingChangingEvent, value); }
    public event RoutedEventHandler RatingChangedDirect { add => AddHandler(RatingChangedDirectEvent, value); remove => RemoveHandler(RatingChangedDirectEvent, value); }

    public double Rating
    {
        get => (double)GetValue(RatingProperty);
        set => SetValue(RatingProperty, value);
    }

    public double MaxRating
    {
        get => (double)GetValue(MaxRatingProperty);
        set => SetValue(MaxRatingProperty, value);
    }

    private static bool ValidateRating(object value) => value is double d && !double.IsNaN(d) && !double.IsInfinity(d);
    private static bool ValidateMaxRating(object value) => value is double d && d > 0 && !double.IsNaN(d) && !double.IsInfinity(d);

    private static object CoerceRating(DependencyObject d, object baseValue)
    {
        if (d is not RatingStarsPicker c || baseValue is not double v) return 0d;
        if (v < 0) v = 0;
        if (v > c.MaxRating) v = c.MaxRating;
        return Math.Round(v * 2, MidpointRounding.AwayFromZero) / 2;
    }

    private static object CoerceMaxRating(DependencyObject d, object baseValue)
    {
        if (baseValue is not double v) return 5d;
        if (v < 1) return 1d;
        if (v > 10) return 10d;
        return v;
    }

    private static void OnMaxRatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => d.CoerceValue(RatingProperty);
    private static void OnRatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RatingStarsPicker c) c.PaintStars();
    }

    private void OnStarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.Tag is not string s || !int.TryParse(s, out var star)) return;
        var oldVal = Rating;
        var newVal = Math.Min(star, MaxRating);

        var preview = new ShopDoubleRoutedEventArgs(PreviewRatingChangingEvent, this, oldVal, newVal);
        RaiseEvent(preview);
        if (preview.Handled) return;

        Rating = newVal;
        RaiseEvent(new ShopDoubleRoutedEventArgs(RatingChangingEvent, this, oldVal, Rating));
        RaiseEvent(new ShopDoubleRoutedEventArgs(RatingChangedDirectEvent, this, oldVal, Rating));
    }

    private void PaintStars()
    {
        var on = TryFindResource("BrushWarning") as Brush ?? Brushes.Goldenrod;
        var off = TryFindResource("BrushGridBorder") as Brush ?? Brushes.Gray;
        var threshold = Math.Ceiling(Rating - 0.01);
        var stars = new[] { Star0, Star1, Star2, Star3, Star4 };
        for (var i = 0; i < stars.Length; i++)
            stars[i].Foreground = (i + 1) <= threshold ? on : off;
    }

    private void OnDirect(object sender, RoutedEventArgs e)
    {
        if (e is not ShopDoubleRoutedEventArgs a) return;
        DirectText.Text = $"Direct: {a.OldValue:0.#} -> {a.NewValue:0.#}";
        DirectText.Visibility = Visibility.Visible;
    }
}
