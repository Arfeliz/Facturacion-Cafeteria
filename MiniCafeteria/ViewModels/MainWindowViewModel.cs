using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace MiniCafeteria.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Cambio))]
    private decimal _totalVentaActual;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Cambio))]
    private decimal _montoRecibido;

    // Propiedad calculada: se actualiza automáticamente
    public decimal Cambio => MontoRecibido > TotalVentaActual ? MontoRecibido - TotalVentaActual : 0;

    [RelayCommand]
    private void AgregarPrecio(string precio)
    {
        if (decimal.TryParse(precio, out decimal valor))
        {
            TotalVentaActual += valor;
        }
    }

    [RelayCommand]
    private void FinalizarVenta()
    {
        // Aquí podrías agregar la lógica para guardar en Base de Datos antes de limpiar
        TotalVentaActual = 0;
        MontoRecibido = 0;
    }

    [RelayCommand]
    private void GenerarReporte()
    {
        // Lógica para el reporte diario
        Console.WriteLine($"Reporte generado. Total: {TotalVentaActual}");
    }
}