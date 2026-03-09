using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace MiniCafeteria.Models;

public partial class ItemVenta : ObservableObject
{
    private static readonly CultureInfo ParseCulture = CultureInfo.CurrentCulture;

    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal _precioUnitario;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private int _cantidad;

    [ObservableProperty]
    private string _precioUnitarioTexto = "0";

    [ObservableProperty]
    private string _cantidadTexto = "1";

    public decimal Subtotal => PrecioUnitario * Cantidad;

    partial void OnPrecioUnitarioChanged(decimal value)
    {
        PrecioUnitarioTexto = value.ToString("0.##", ParseCulture);
    }

    partial void OnCantidadChanged(int value)
    {
        CantidadTexto = value.ToString(ParseCulture);
    }

    partial void OnPrecioUnitarioTextoChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (decimal.TryParse(value, NumberStyles.Number, ParseCulture, out var parsed) ||
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            PrecioUnitario = parsed < 0 ? 0 : parsed;
        }
    }

    partial void OnCantidadTextoChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (int.TryParse(value, NumberStyles.Integer, ParseCulture, out var parsed))
        {
            Cantidad = parsed < 0 ? 0 : parsed;
        }
    }
}
