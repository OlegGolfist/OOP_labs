using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
    private readonly ProductRepository _repo = new();
    private ICollectionView? _itemsView;
    private GuitarProduct? _selectedProduct;
    private string _searchText = "";
    private string _filterCategory = "";
    private decimal? _priceMin;
    private decimal? _priceMax;
    private string _priceMinStr = "";
    private string _priceMaxStr = "";
    private bool _onlyInStock;
    private UserRole _role = UserRole.Client;

    public MainViewModel()
    {
        Products = new ObservableCollection<GuitarProduct>(_repo.Load());
        _itemsView = CollectionViewSource.GetDefaultView(Products);
        _itemsView.Filter = FilterProduct;

        RebuildCategoryOptions();
        _filterCategory = CategoryOptions.FirstOrDefault() ?? "";

        AddCommand = new RelayCommand(AddProduct, () => IsAdmin);
        EditSaveCommand = new RelayCommand(SaveCurrent, () => IsAdmin && SelectedProduct != null);
        DeleteCommand = new RelayCommand(DeleteCurrent, () => IsAdmin && SelectedProduct != null);
        ApplyFilterCommand = new RelayCommand(ApplyFilters);
        ClearFilterCommand = new RelayCommand(ClearFilters);
        SetLangRuCommand = new RelayCommand(() => SetLanguage("ru"));
        SetLangEnCommand = new RelayCommand(() => SetLanguage("en"));

        UpdateVisibleCount();
    }

    public ObservableCollection<string> CategoryOptions { get; } = new();

    public ObservableCollection<GuitarProduct> Products { get; }

    public GuitarProduct? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (ReferenceEquals(_selectedProduct, value)) return;
            _selectedProduct = value;
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
            _repo.Save(Products);
            SelectedProduct = np;
            RefreshView();
        }
    }

    private void SaveCurrent()
    {
        if (SelectedProduct == null) return;
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
        var msg = Application.Current.TryFindResource("MsgDeleteConfirm") as string ?? "Удалить?";
        if (MessageBox.Show(msg, "", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Products.Remove(SelectedProduct);
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
            if (d.Source != null && d.Source.OriginalString.Contains("/Localization/", StringComparison.OrdinalIgnoreCase))
            {
                toRemove = d;
                break;
            }
        }

        if (toRemove != null)
            dicts.Remove(toRemove);

        var path = culture.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? "pack://application:,,,/Localization/Strings.en.xaml"
            : "pack://application:,,,/Localization/Strings.ru.xaml";

        dicts.Add(new ResourceDictionary { Source = new Uri(path, UriKind.Absolute) });

        RebuildCategoryOptions();
        FilterCategory = Application.Current.TryFindResource("CatAll") as string ?? "Все";
        OnPropertyChanged(nameof(RoleTitle));
        RefreshView();
        LanguageChanged?.Invoke();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
