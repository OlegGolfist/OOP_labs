using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using lab4_5.Models;
using lab4_5.Services;
using lab4_5.Views;

namespace lab4_5.ViewModels;

public enum UserRole
{
    Client,
    Admin
}

public class MainViewModel : INotifyPropertyChanged
{
    private const int MaxTrackedQuantity = 100;
    private readonly ProductRepository _repo = new();
    private ICollectionView? _itemsView;
    private GuitarProduct? _selectedProduct;
    private GuitarProduct? _selectedSnapshot;
    private string _searchText = "";
    private string _filterCategory = "";
    private decimal? _priceMin;
    private decimal? _priceMax;
    private string _priceMinStr = "";
    private string _priceMaxStr = "";
    private bool _onlyInStock;
    private UserRole _role = UserRole.Client;
    private string _userName = "Student";
    private string _email = "student@example.com";
    private string _preferredLanguage = "ru";
    private string _selectedTheme = "optimistic";
    private int _activePageIndex;
    private string _deliveryAddress = "";
    private string _selectedPaymentMethod = "Картой";
    private CartItem? _selectedCartItem;
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();

    public MainViewModel()
    {
        Products = new ObservableCollection<GuitarProduct>(_repo.Load());
        foreach (var p in Products)
            p.PropertyChanged += OnProductPropertyChanged;

        _itemsView = CollectionViewSource.GetDefaultView(Products);
        _itemsView.Filter = FilterProduct;

        CartItems = new ObservableCollection<CartItem>();
        CartItems.CollectionChanged += OnCartCollectionChanged;

        RebuildCategoryOptions();
        _filterCategory = CategoryOptions.FirstOrDefault() ?? "";
        ThemeOptions = new ObservableCollection<string> { "optimistic", "pink", "gray" };
        PaymentMethods = new ObservableCollection<string> { "Картой", "Наличкой", };

        AddCommand = new RelayCommand(AddProduct, () => IsAdmin);
        EditSaveCommand = new RelayCommand(SaveCurrent, () => IsAdmin && SelectedProduct != null);
        DeleteCommand = new RelayCommand(DeleteCurrent, () => IsAdmin && SelectedProduct != null);
        ApplyFilterCommand = new RelayCommand(ApplyFilters);
        ClearFilterCommand = new RelayCommand(ClearFilters);
        SetLangRuCommand = new RelayCommand(() => SetLanguage("ru"));
        SetLangEnCommand = new RelayCommand(() => SetLanguage("en"));
        SetThemeCommand = new RelayCommand(p => SetThemeByKey(p as string));
        OpenProfileCommand = new RelayCommand(OpenProfile);
        UndoCommand = new RelayCommand(Undo, () => _undoStack.Count > 0);
        RedoCommand = new RelayCommand(Redo, () => _redoStack.Count > 0);
        AddToCartCommand = new RelayCommand(AddToCart);
        RemoveFromCartCommand = new RelayCommand(_ => RemoveSelectedCartItem(), _ => SelectedCartItem != null);
        OpenCartCommand = new RelayCommand(_ => ActivePageIndex = 1);
        OpenCatalogCommand = new RelayCommand(_ => ActivePageIndex = 0);
        CheckoutCommand = new RelayCommand(_ => Checkout());

        UpdateVisibleCount();
        UpdateCartSummary();
        ApplyTheme(_selectedTheme);
    }

    public ObservableCollection<string> CategoryOptions { get; } = new();
    public ObservableCollection<string> ThemeOptions { get; }
    public ObservableCollection<string> PaymentMethods { get; }
    public ObservableCollection<GuitarProduct> Products { get; }
    public ObservableCollection<CartItem> CartItems { get; }
    public ICollectionView? FilteredProducts => _itemsView;

    public GuitarProduct? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (ReferenceEquals(_selectedProduct, value)) return;
            _selectedProduct = value;
            _selectedSnapshot = value?.Clone();
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetailIsReadOnly));
            OnPropertyChanged(nameof(CanEditDetail));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }
    public string FilterCategory { get => _filterCategory; set { _filterCategory = value; OnPropertyChanged(); } }
    public string PriceMinStr { get => _priceMinStr; set { _priceMinStr = value; OnPropertyChanged(); } }
    public string PriceMaxStr { get => _priceMaxStr; set { _priceMaxStr = value; OnPropertyChanged(); } }
    public bool OnlyInStock { get => _onlyInStock; set { _onlyInStock = value; OnPropertyChanged(); } }

    public int RoleIndex
    {
        get => Role == UserRole.Admin ? 1 : 0;
        set => Role = value == 1 ? UserRole.Admin : UserRole.Client;
    }

    public UserRole Role
    {
        get => _role;
        set
        {
            if (_role == value) return;
            _role = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RoleIndex));
            OnPropertyChanged(nameof(IsAdmin));
            OnPropertyChanged(nameof(RoleTitle));
            OnPropertyChanged(nameof(StatusSummary));
            OnPropertyChanged(nameof(DetailIsReadOnly));
            OnPropertyChanged(nameof(CanEditDetail));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsAdmin => Role == UserRole.Admin;
    public bool DetailIsReadOnly => !IsAdmin || SelectedProduct == null;
    public bool CanEditDetail => IsAdmin && SelectedProduct != null;
    public int VisibleCount { get; private set; }
    public int ActivePageIndex { get => _activePageIndex; set { _activePageIndex = value; OnPropertyChanged(); } }
    public string DeliveryAddress { get => _deliveryAddress; set { _deliveryAddress = value; OnPropertyChanged(); } }
    public string SelectedPaymentMethod { get => _selectedPaymentMethod; set { _selectedPaymentMethod = value; OnPropertyChanged(); } }

    public CartItem? SelectedCartItem
    {
        get => _selectedCartItem;
        set
        {
            _selectedCartItem = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public int CartItemsCount => CartItems.Sum(x => x.Quantity);
    public decimal CartTotal => CartItems.Sum(x => x.LineTotal);
    public string UserName { get => _userName; set { _userName = value; OnPropertyChanged(); } }
    public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }

    public string PreferredLanguage
    {
        get => _preferredLanguage;
        set
        {
            _preferredLanguage = value;
            OnPropertyChanged();
            if (!string.IsNullOrWhiteSpace(value))
                SetLanguage(value);
        }
    }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme == value) return;
            _selectedTheme = value;
            ApplyTheme(value);
            OnPropertyChanged();
        }
    }

    public string RoleTitle =>
        IsAdmin ? Application.Current.TryFindResource("RoleAdmin") as string ?? "Admin"
                : Application.Current.TryFindResource("RoleClient") as string ?? "Client";

    public string StatusSummary =>
        string.Format(
            Application.Current.TryFindResource("StatusTplCart") as string ?? "{0} | {1} | Cart: {2}",
            VisibleCount,
            RoleTitle,
            CartItemsCount);

    public ICommand AddCommand { get; }
    public ICommand EditSaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ApplyFilterCommand { get; }
    public ICommand ClearFilterCommand { get; }
    public ICommand SetLangRuCommand { get; }
    public ICommand SetLangEnCommand { get; }
    public ICommand SetThemeCommand { get; }
    public ICommand OpenProfileCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand AddToCartCommand { get; }
    public ICommand RemoveFromCartCommand { get; }
    public ICommand OpenCartCommand { get; }
    public ICommand OpenCatalogCommand { get; }
    public ICommand CheckoutCommand { get; }
    public event Action? LanguageChanged;

    private void RebuildCategoryOptions()
    {
        CategoryOptions.Clear();
        var all = Application.Current.TryFindResource("CatAll") as string ?? "All";
        CategoryOptions.Add(all);
        foreach (var c in GuitarProduct.Categories)
            CategoryOptions.Add(c);
        OnPropertyChanged(nameof(CategoryOptions));
    }

    private bool FilterProduct(object obj)
    {
        if (obj is not GuitarProduct p) return false;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var q = SearchText.Trim();
            if (!p.ShortName.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !p.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !p.Description.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !p.Manufacturer.Contains(q, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        var all = Application.Current.TryFindResource("CatAll") as string ?? "All";
        if (!string.IsNullOrEmpty(FilterCategory) && FilterCategory != all && p.Category != FilterCategory)
            return false;
        if (_priceMin is decimal min && p.Price < min) return false;
        if (_priceMax is decimal max && p.Price > max) return false;
        if (OnlyInStock && (p.IsOutOfStock || p.Quantity <= 0)) return false;
        return true;
    }

    private void ApplyFilters()
    {
        _priceMin = decimal.TryParse(PriceMinStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var a) ? a : null;
        _priceMax = decimal.TryParse(PriceMaxStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var b) ? b : null;
        RefreshView();
    }

    private void ClearFilters()
    {
        SearchText = "";
        FilterCategory = Application.Current.TryFindResource("CatAll") as string ?? "All";
        PriceMinStr = "";
        PriceMaxStr = "";
        _priceMin = null;
        _priceMax = null;
        OnlyInStock = false;
        RefreshView();
    }

    private void RefreshView()
    {
        _itemsView?.Refresh();
        UpdateVisibleCount();
        OnPropertyChanged(nameof(StatusSummary));
        OnPropertyChanged(nameof(FilteredProducts));
    }

    private void UpdateVisibleCount()
    {
        VisibleCount = _itemsView?.Cast<object>().Count() ?? Products.Count;
        OnPropertyChanged(nameof(VisibleCount));
        OnPropertyChanged(nameof(StatusSummary));
    }

    private void AddProduct()
    {
        var dlg = new AddProductWindow { Owner = Application.Current.MainWindow };
        if (dlg.ShowDialog() == true && dlg.Result is { } np)
        {
            Products.Add(np);
            np.PropertyChanged += OnProductPropertyChanged;
            RegisterAction(new AddAction(np, Products));
            _repo.Save(Products);
            SelectedProduct = np;
            RefreshView();
        }
    }

    private void SaveCurrent()
    {
        if (SelectedProduct == null) return;
        if (_selectedSnapshot != null)
            RegisterAction(new EditAction(_selectedSnapshot.Clone(), SelectedProduct.Clone(), Products));
        _selectedSnapshot = SelectedProduct.Clone();
        _repo.Save(Products);
        MessageBox.Show(Application.Current.TryFindResource("MsgSaved") as string ?? "OK", "", MessageBoxButton.OK);
        RefreshView();
    }

    private void DeleteCurrent()
    {
        if (SelectedProduct == null) return;
        var toDelete = SelectedProduct;
        var msg = Application.Current.TryFindResource("MsgDeleteConfirm") as string ?? "Delete?";
        if (MessageBox.Show(msg, "", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Products.Remove(toDelete);
        toDelete.PropertyChanged -= OnProductPropertyChanged;
        RegisterAction(new DeleteAction(toDelete.Clone(), Products));
        SelectedProduct = null;
        _repo.Save(Products);
        RefreshView();
    }

    private void AddToCart(object? parameter)
    {
        var product = parameter as GuitarProduct ?? SelectedProduct;
        if (product == null)
            return;
        if (product.Quantity <= 0)
        {
            MessageBox.Show(Application.Current.TryFindResource("MsgOutOfStock") as string ?? "Out of stock.", "", MessageBoxButton.OK);
            return;
        }
        var existing = CartItems.FirstOrDefault(x => x.Product.Id == product.Id);
        if (existing != null)
        {
            if (existing.Quantity >= product.Quantity)
            {
                MessageBox.Show(Application.Current.TryFindResource("MsgCannotAddMoreStock") as string ?? "Cannot add more than available stock.", "", MessageBoxButton.OK);
                return;
            }
            existing.Quantity++;
            return;
        }
        var item = new CartItem { Product = product, Quantity = 1 };
        item.PropertyChanged += OnCartItemChanged;
        CartItems.Add(item);
    }

    private void RemoveSelectedCartItem()
    {
        if (SelectedCartItem == null)
            return;
        SelectedCartItem.PropertyChanged -= OnCartItemChanged;
        CartItems.Remove(SelectedCartItem);
    }

    private void Checkout()
    {
        if (CartItems.Count == 0)
        {
            MessageBox.Show(Application.Current.TryFindResource("MsgCartEmpty") as string ?? "Cart is empty.", "", MessageBoxButton.OK);
            return;
        }
        if (string.IsNullOrWhiteSpace(DeliveryAddress))
        {
            MessageBox.Show(Application.Current.TryFindResource("MsgEnterDeliveryAddress") as string ?? "Enter delivery address.", "", MessageBoxButton.OK);
            return;
        }
        foreach (var item in CartItems)
        {
            if (item.Product.Quantity < item.Quantity)
            {
                MessageBox.Show(
                    string.Format(
                        Application.Current.TryFindResource("MsgInsufficientStock") as string ?? "Insufficient stock: {0}",
                        item.Product.ShortName),
                    "",
                    MessageBoxButton.OK);
                return;
            }
        }
        foreach (var item in CartItems.ToList())
        {
            item.Product.Quantity -= item.Quantity;
            item.Product.PurchasedCount += item.Quantity;
            item.Product.IsOutOfStock = item.Product.Quantity <= 0;
            item.PropertyChanged -= OnCartItemChanged;
        }
        CartItems.Clear();
        DeliveryAddress = "";
        _repo.Save(Products);
        RefreshView();
        UpdateCartSummary();
        ActivePageIndex = 0;
        MessageBox.Show(Application.Current.TryFindResource("MsgOrderPlaced") as string ?? "Order placed.", "", MessageBoxButton.OK);
    }

    private void SetLanguage(string culture)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        ResourceDictionary? toRemove = null;
        foreach (var d in dicts)
        {
            if (d.Source != null && d.Source.OriginalString.Contains("Localization/", StringComparison.OrdinalIgnoreCase))
            {
                toRemove = d;
                break;
            }
        }
        if (toRemove != null)
            dicts.Remove(toRemove);

        var path = culture.Equals("en", StringComparison.OrdinalIgnoreCase) ? "Localization/Strings.en.xaml" : "Localization/Strings.ru.xaml";
        dicts.Add(new ResourceDictionary { Source = new Uri(path, UriKind.Relative) });
        _preferredLanguage = culture;
        OnPropertyChanged(nameof(PreferredLanguage));
        RebuildCategoryOptions();
        FilterCategory = Application.Current.TryFindResource("CatAll") as string ?? "All";
        OnPropertyChanged(nameof(RoleTitle));
        RefreshView();
        LanguageChanged?.Invoke();
    }

    private void SetThemeByKey(string? key)
    {
        if (!string.IsNullOrWhiteSpace(key))
            SelectedTheme = key;
    }

    private void OpenProfile()
    {
        var profile = new ProfileWindow { Owner = Application.Current.MainWindow, DataContext = this };
        profile.ShowDialog();
    }

    private void ApplyTheme(string key)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        ResourceDictionary? toRemove = null;
        foreach (var d in dicts)
        {
            if (d.Source != null && d.Source.OriginalString.Contains("Themes/Theme", StringComparison.OrdinalIgnoreCase))
            {
                toRemove = d;
                break;
            }
        }
        if (toRemove != null)
            dicts.Remove(toRemove);
        var path = key switch
        {
            "pink" => "Themes/ThemePink.xaml",
            "gray" => "Themes/ThemeGray.xaml",
            _ => "Themes/ThemeOptimistic.xaml"
        };
        dicts.Insert(0, new ResourceDictionary { Source = new Uri(path, UriKind.Relative) });
    }

    private void OnProductPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not GuitarProduct p || e.PropertyName != nameof(GuitarProduct.Quantity))
            return;

        if (p.Quantity == 0 || p.Quantity == MaxTrackedQuantity)
        {
            var msg = string.Format(
                Application.Current.TryFindResource("MsgQuantityState") as string ?? "Product quantity \"{0}\": {1}",
                p.ShortName,
                p.Quantity);
            MessageBox.Show(msg, "", MessageBoxButton.OK);
            LogEvent(msg);
        }

        p.IsOutOfStock = p.Quantity <= 0;
        foreach (var item in CartItems.Where(x => x.Product.Id == p.Id))
        {
            if (item.Quantity > p.Quantity)
                item.Quantity = Math.Max(1, p.Quantity);
        }
        UpdateCartSummary();
    }

    private void OnCartCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateCartSummary();

    private void OnCartItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.LineTotal))
            UpdateCartSummary();
    }

    private void UpdateCartSummary()
    {
        OnPropertyChanged(nameof(CartItemsCount));
        OnPropertyChanged(nameof(CartTotal));
        OnPropertyChanged(nameof(StatusSummary));
        CommandManager.InvalidateRequerySuggested();
    }

    private static void LogEvent(string line)
    {
        var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(logsDir);
        var file = Path.Combine(logsDir, "events.log");
        File.AppendAllText(file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}{Environment.NewLine}");
    }

    private void RegisterAction(IUndoableAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();
        CommandManager.InvalidateRequerySuggested();
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);
        _repo.Save(Products);
        RefreshView();
        CommandManager.InvalidateRequerySuggested();
    }

    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        var action = _redoStack.Pop();
        action.Redo();
        _undoStack.Push(action);
        _repo.Save(Products);
        RefreshView();
        CommandManager.InvalidateRequerySuggested();
    }

    private interface IUndoableAction { void Undo(); void Redo(); }

    private sealed class AddAction(GuitarProduct product, ObservableCollection<GuitarProduct> target) : IUndoableAction
    {
        public void Undo() => target.Remove(product);
        public void Redo() => target.Add(product);
    }

    private sealed class DeleteAction(GuitarProduct product, ObservableCollection<GuitarProduct> target) : IUndoableAction
    {
        public void Undo() => target.Add(product.Clone());
        public void Redo()
        {
            var existing = target.FirstOrDefault(x => x.Id == product.Id);
            if (existing != null)
                target.Remove(existing);
        }
    }

    private sealed class EditAction(GuitarProduct before, GuitarProduct after, ObservableCollection<GuitarProduct> target) : IUndoableAction
    {
        public void Undo() => Apply(before);
        public void Redo() => Apply(after);

        private void Apply(GuitarProduct source)
        {
            var existing = target.FirstOrDefault(x => x.Id == source.Id);
            if (existing == null) return;
            existing.ShortName = source.ShortName;
            existing.FullName = source.FullName;
            existing.Description = source.Description;
            existing.ImagePaths = new List<string>(source.ImagePaths);
            existing.Category = source.Category;
            existing.Rating = source.Rating;
            existing.Price = source.Price;
            existing.Quantity = source.Quantity;
            existing.Color = source.Color;
            existing.Size = source.Size;
            existing.DeliveryCountry = source.DeliveryCountry;
            existing.DiscountPercent = source.DiscountPercent;
            existing.IsOutOfStock = source.IsOutOfStock;
            existing.PurchasedCount = source.PurchasedCount;
            existing.Manufacturer = source.Manufacturer;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
