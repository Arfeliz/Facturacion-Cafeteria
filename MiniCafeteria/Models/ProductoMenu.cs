using CommunityToolkit.Mvvm.ComponentModel;

namespace MiniCafeteria.Models;

public partial class ProductoMenu : ObservableObject
{
    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private decimal _precio;

    [ObservableProperty]
    private string _categoria = string.Empty;

    [ObservableProperty]
    private string _colorFondo = "#3498db";

    [ObservableProperty]
    private string _colorBorde = "#2c7bb5";
}
