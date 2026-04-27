using System.Collections.ObjectModel;
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
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();

    public MainViewModel()
    {
        Products = new ObservableCollection<GuitarProduct>(_repo.Load());
        foreach (var p in Products)
            p.PropertyChanged += OnProductPropertyChanged;
        _itemsView = CollectionViewSource.GetDefaultView(Products);
        _itemsView.Filter = FilterProduct;

        RebuildCategoryOptions();
        _filterCategory = CategoryOptions.FirstOrDefault() ?? "";
        ThemeOptions = new ObservableCollection<string> { "optimistic", "pink", "gray" };

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

        UpdateVisibleCount();
        ApplyTheme(_selectedTheme);
    }

    public ObservableCollection<string> CategoryOptions { get; } = new();
    public ObservableCollection<string> ThemeOptions { get; }

    public ObservableCollection<GuitarProduct> Products { get; }

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

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); }
    }

    public string FilterCategory
    {
        get => _filterCategory;
        set { _filterCategory = value; OnPropertyChanged(); }
    }

    public string PriceMinStr
    {
        get => _priceMinStr;
        set { _priceMinStr = value; OnPropertyChanged(); }
    }

    public string PriceMaxStr
    {
        get => _priceMaxStr;
        set { _priceMaxStr = value; OnPropertyChanged(); }
    }

    public bool OnlyInStock
    {
        get => _onlyInStock;
        set { _onlyInStock = value; OnPropertyChanged(); }
    }

    public int RoleIndex
    {
        get => Role == UserRole.Admin ? 1 : 0;
        set
        {
            var r = value == 1 ? UserRole.Admin : UserRole.Client;
            Role = r;
        }
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
    public string UserName
    {
        get => _userName;
        set { _userName = value; OnPropertyChanged(); }
    }

    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }

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
        IsAdmin
            ? Application.Current.TryFindResource("RoleAdmin") as string ?? "Admin"
            : Application.Current.TryFindResource("RoleClient") as string ?? "Client";

    public string StatusSummary =>
        string.Format(
            Application.Current.TryFindResource("StatusTpl") as string ?? "{0} | {1}",
            VisibleCount,
            RoleTitle);

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
    public event Action? LanguageChanged;

    private void RebuildCategoryOptions()
    {
        CategoryOptions.Clear();
        var all = Application.Current.TryFindResource("CatAll") as string ?? "Все";
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

        var all = Application.Current.TryFindResource("CatAll") as string ?? "Все";
        if (!string.IsNullOrEmpty(FilterCategory) && FilterCategory != all && p.Category != FilterCategory)
            return false;

        if (_priceMin is decimal min && p.Price < min) return false;
        if (_priceMax is decimal max && p.Price > max) return false;

        if (OnlyInStock && (p.IsOutOfStock || p.Quantity <= 0)) return false;

        return true;
    }

    private void ApplyFilters()
    {
        _priceMin = decimal.TryParse(
            PriceMinStr.Replace(',', '.'),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var a)
            ? a
            : null;
        _priceMax = decimal.TryParse(
            PriceMaxStr.Replace(',', '.'),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var b)
            ? b
            : null;
        RefreshView();
    }

    private void ClearFilters()
    {
        SearchText = "";
        FilterCategory = Application.Current.TryFindResource("CatAll") as string ?? "Все";
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
    }

    private void UpdateVisibleCount()
    {
        if (_itemsView == null)
        {
            VisibleCount = Products.Count;
        }
        else
        {
            VisibleCount = _itemsView.Cast<object>().Count();
        }

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
        MessageBox.Show(
            Application.Current.TryFindResource("MsgSaved") as string ?? "OK",
            "",
            MessageBoxButton.OK);
        RefreshView();
    }

    private void DeleteCurrent()
    {
        if (SelectedProduct == null) return;
        var toDelete = SelectedProduct;
        var msg = Application.Current.TryFindResource("MsgDeleteConfirm") as string ?? "Удалить?";
        if (MessageBox.Show(msg, "", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Products.Remove(toDelete);
        toDelete.PropertyChanged -= OnProductPropertyChanged;
        RegisterAction(new DeleteAction(toDelete.Clone(), Products));
        SelectedProduct = null;
        _repo.Save(Products);
        RefreshView();
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

        var path = culture.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? "Localization/Strings.en.xaml"
            : "Localization/Strings.ru.xaml";

        dicts.Add(new ResourceDictionary { Source = new Uri(path, UriKind.Relative) });
        _preferredLanguage = culture;
        OnPropertyChanged(nameof(PreferredLanguage));

        RebuildCategoryOptions();
        FilterCategory = Application.Current.TryFindResource("CatAll") as string ?? "Все";
        OnPropertyChanged(nameof(RoleTitle));
        RefreshView();
        LanguageChanged?.Invoke();
    }

    private void SetThemeByKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;
        SelectedTheme = key;
    }

    private void OpenProfile()
    {
        var profile = new Views.ProfileWindow { Owner = Application.Current.MainWindow, DataContext = this };
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
                Application.Current.TryFindResource("MsgQuantityState") as string ?? "Количество товара \"{0}\": {1}",
                p.ShortName,
                p.Quantity);
            MessageBox.Show(msg, "", MessageBoxButton.OK);
            LogEvent(msg);
        }
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

    private interface IUndoableAction
    {
        void Undo();
        void Redo();
    }

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
