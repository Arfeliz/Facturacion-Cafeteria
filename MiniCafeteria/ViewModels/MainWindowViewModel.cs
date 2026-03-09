using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using MiniCafeteria.Models;

namespace MiniCafeteria.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // Lista editable del ticket
    public ObservableCollection<ItemVenta> ListaFactura { get; } = new();

    // Catalogo base del menu (cargado desde JSON)
    public ObservableCollection<ProductoMenu> CatalogoMenu { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Cambio))]
    private decimal _montoRecibido;

    [ObservableProperty]
    private string _buscarTexto = string.Empty;

    public MainWindowViewModel()
    {
        ListaFactura.CollectionChanged += OnListaFacturaCollectionChanged;
        CargarCatalogo();
    }

    // El total se calcula sumando los subtotales de la lista
    public decimal TotalVentaActual => ListaFactura.Sum(x => x.Subtotal);

    public decimal Cambio => MontoRecibido > TotalVentaActual ? MontoRecibido - TotalVentaActual : 0;

    public IEnumerable<ProductoMenu> BebidasFiltradas => FiltrarPorCategoria("Bebidas");

    public IEnumerable<ProductoMenu> ComidasFiltradas => FiltrarPorCategoria("Comidas");

    [RelayCommand]
    private void AgregarProducto(ProductoMenu? producto)
    {
        if (producto is null)
        {
            return;
        }

        ListaFactura.Add(new ItemVenta
        {
            Nombre = producto.Nombre,
            PrecioUnitario = producto.Precio,
            Cantidad = 1
        });
    }

    [RelayCommand]
    private void EliminarItem(ItemVenta? item)
    {
        if (item is null)
        {
            return;
        }

        ListaFactura.Remove(item);
    }

    [RelayCommand]
    private void FinalizarVenta()
    {
        foreach (var item in ListaFactura)
        {
            item.PropertyChanged -= OnItemVentaPropertyChanged;
        }

        ListaFactura.Clear();
        MontoRecibido = 0;
        NotificarTotales();
    }

    [RelayCommand]
    private void GenerarReporte()
    {
        Console.WriteLine($"Reporte generado. Total: {TotalVentaActual}");
    }

    partial void OnBuscarTextoChanged(string value)
    {
        OnPropertyChanged(nameof(BebidasFiltradas));
        OnPropertyChanged(nameof(ComidasFiltradas));
    }

    private IEnumerable<ProductoMenu> FiltrarPorCategoria(string categoria)
    {
        var query = CatalogoMenu.Where(p => p.Categoria == categoria);

        if (string.IsNullOrWhiteSpace(BuscarTexto))
        {
            return query;
        }

        return query.Where(p => p.Nombre.Contains(BuscarTexto, StringComparison.OrdinalIgnoreCase));
    }

    private void CargarCatalogo()
    {
        try
        {
            var uri = new Uri("avares://MiniCafeteria/Assets/Data/menu-productos.json");
            using var stream = AssetLoader.Open(uri);
            var productos = JsonSerializer.Deserialize<List<ProductoMenu>>(stream);

            if (productos is null)
            {
                return;
            }

            foreach (var producto in productos)
            {
                CatalogoMenu.Add(producto);
            }
        }
        catch
        {
            // Si el archivo de menu falla, se evita romper la pantalla.
        }
    }

    private void OnListaFacturaCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ItemVenta item in e.OldItems)
            {
                item.PropertyChanged -= OnItemVentaPropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (ItemVenta item in e.NewItems)
            {
                item.PropertyChanged += OnItemVentaPropertyChanged;
            }
        }

        NotificarTotales();
    }

    private void OnItemVentaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ItemVenta.Cantidad) ||
            e.PropertyName == nameof(ItemVenta.PrecioUnitario) ||
            e.PropertyName == nameof(ItemVenta.Subtotal))
        {
            NotificarTotales();
        }
    }

    private void NotificarTotales()
    {
        OnPropertyChanged(nameof(TotalVentaActual));
        OnPropertyChanged(nameof(Cambio));
    }
}
