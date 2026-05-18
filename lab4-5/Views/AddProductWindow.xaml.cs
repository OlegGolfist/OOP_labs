using System.Windows;
using System.Windows.Input;
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
            Category = "Электро"
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
}
