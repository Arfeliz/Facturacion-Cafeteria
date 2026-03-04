using CommunityToolkit.Mvvm.ComponentModel;

namespace MiniCafeteria.Models;

public partial class DetalleVenta : ObservableObject
{
    public string Nombre { get; set; } = string.Empty;

    public decimal PrecioUnitario { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private int _cantidad;

    public decimal Subtotal => PrecioUnitario * Cantidad;
}
