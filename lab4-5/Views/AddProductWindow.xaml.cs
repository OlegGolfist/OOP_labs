using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using lab4_5.Models;
using lab4_5.ViewModels;

namespace lab4_5.Views;

public partial class AddProductWindow : Window
{
    private readonly GuitarProduct _model;

    public GuitarProduct? Result { get; private set; }

    public ICommand OkCmd { get; }

    public AddProductWindow()
    {
        _model = new GuitarProduct
        {
            Category = "Электро",
            ImagePaths = new List<string> { "pack://application:,,,/Assets/app.ico" }
        };
        OkCmd = new RelayCommand(_ => TryOk());
        InitializeComponent();
        FieldsPanel.DataContext = _model;
    }

    private void TryOk()
    {
        if (string.IsNullOrWhiteSpace(_model.ShortName))
        {
            MessageBox.Show(
                Application.Current.TryFindResource("MsgNameRequired") as string ?? "Name?",
                "",
                MessageBoxButton.OK);
            return;
        }

        Result = _model;
        DialogResult = true;
    }

    private void OnPickImageClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = Application.Current.TryFindResource("PickImageTitle") as string ?? "Select image",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|All files (*.*)|*.*",
            Multiselect = true
        };

        if (dlg.ShowDialog(this) != true || dlg.FileNames.Length == 0)
            return;

        TbImg.Text = AppendPaths(TbImg.Text, dlg.FileNames);
    }

    private static string AppendPaths(string current, IEnumerable<string> filePaths)
    {
        var merged = current
            .Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Concat(filePaths)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(";", merged);
    }
}
