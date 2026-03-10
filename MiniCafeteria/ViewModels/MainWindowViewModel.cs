using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using MiniCafeteria.Models;

namespace MiniCafeteria.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _rutaMenuJson;

    public ObservableCollection<ItemVenta> ListaFactura { get; } = new();

    public ObservableCollection<ProductoMenu> CatalogoMenu { get; } = new();

    public ObservableCollection<ProductoMenu> MenuEditableLista { get; } = new();

    public IReadOnlyList<string> TemasDisponibles { get; } = new[] { "Azul", "Verde", "Naranja", "Gris" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Cambio))]
    private decimal _montoRecibido;

    [ObservableProperty]
    private string _buscarTexto = string.Empty;

    [ObservableProperty]
    private string _estadoMenuEdicion = "Menu cargado";

    [ObservableProperty]
    private bool _mostrarEditorMenu;

    [ObservableProperty]
    private bool _mostrarOpcionesBarra;

    [ObservableProperty]
    private string _temaActual = "Azul";

    [ObservableProperty]
    private int _pestanaDerechaSeleccionada;

    [ObservableProperty]
    private string _nuevoItemNombre = string.Empty;

    [ObservableProperty]
    private string _nuevoItemPrecioTexto = "0";

    [ObservableProperty]
    private string _nuevoItemCantidadTexto = "1";

    [ObservableProperty]
    private string _estadoAgregarItem = "Completa los datos y confirma";

    public MainWindowViewModel()
    {
        _rutaMenuJson = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniCafeteria",
            "menu-productos.json");

        ListaFactura.CollectionChanged += OnListaFacturaCollectionChanged;

        AsegurarArchivoMenuEditable();
        RecargarMenuDesdeArchivoInterno();
    }

    public string RutaMenuJson => _rutaMenuJson;

    public string FondoVentana => TemaActual switch
    {
        "Verde" => "#1f2824",
        "Naranja" => "#2a2320",
        "Gris" => "#222222",
        _ => "#1f2630"
    };

    public string FondoBarra => TemaActual switch
    {
        "Verde" => "#2b3933",
        "Naranja" => "#3a2f29",
        "Gris" => "#2d2d2d",
        _ => "#2a3240"
    };

    public string FondoPanel => TemaActual switch
    {
        "Verde" => "#25312d",
        "Naranja" => "#332a25",
        "Gris" => "#2a2a2a",
        _ => "#263042"
    };

    public string FondoPanelSecundario => TemaActual switch
    {
        "Verde" => "#202925",
        "Naranja" => "#2b241f",
        "Gris" => "#242424",
        _ => "#222b3c"
    };

    public string ColorBorde => TemaActual switch
    {
        "Verde" => "#40574f",
        "Naranja" => "#5c4b40",
        "Gris" => "#474747",
        _ => "#41506a"
    };

    public string ColorTextoPrincipal => "#e8edf3";

    public string ColorTextoSecundario => "#a9b5c5";

    public string ColorAcento => TemaActual switch
    {
        "Verde" => "#2fa36b",
        "Naranja" => "#d08a3d",
        "Gris" => "#5a8dbb",
        _ => "#3e7bd8"
    };

    public string ColorAcentoBorde => TemaActual switch
    {
        "Verde" => "#267f54",
        "Naranja" => "#a86f31",
        "Gris" => "#4a7397",
        _ => "#335fab"
    };

    public string ColorCambio => TemaActual switch
    {
        "Verde" => "#4fd39a",
        "Naranja" => "#f0b46a",
        "Gris" => "#8ec3f0",
        _ => "#78c8ff"
    };

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

        EstadoAgregarItem = $"Agregado desde menu: {producto.Nombre}";
    }

    [RelayCommand]
    private void AbrirNuevoItemTab()
    {
        if (string.IsNullOrWhiteSpace(NuevoItemNombre) && !string.IsNullOrWhiteSpace(BuscarTexto))
        {
            NuevoItemNombre = BuscarTexto.Trim();
        }

        PestanaDerechaSeleccionada = 1;
    }

    [RelayCommand]
    private void AgregarItemDesdeFormulario()
    {
        var nombre = string.IsNullOrWhiteSpace(NuevoItemNombre) ? "Item libre" : NuevoItemNombre.Trim();

        if (!TryParseDecimal(NuevoItemPrecioTexto, out var precio))
        {
            EstadoAgregarItem = "Precio invalido";
            return;
        }

        if (!int.TryParse(NuevoItemCantidadTexto, out var cantidad) || cantidad <= 0)
        {
            EstadoAgregarItem = "Cantidad invalida";
            return;
        }

        ListaFactura.Add(new ItemVenta
        {
            Nombre = nombre,
            PrecioUnitario = precio < 0 ? 0 : precio,
            Cantidad = cantidad
        });

        EstadoAgregarItem = $"Agregado: {nombre}";
        NuevoItemNombre = string.Empty;
        NuevoItemPrecioTexto = "0";
        NuevoItemCantidadTexto = "1";
        PestanaDerechaSeleccionada = 0;
    }

    [RelayCommand]
    private void CancelarNuevoItem()
    {
        PestanaDerechaSeleccionada = 0;
    }

    [RelayCommand]
    private void ToggleOpcionesBarra()
    {
        MostrarOpcionesBarra = !MostrarOpcionesBarra;
    }

    [RelayCommand]
    private void ToggleEditorMenu()
    {
        MostrarEditorMenu = !MostrarEditorMenu;
    }

    [RelayCommand]
    private void AgregarProductoMenuLista()
    {
        MenuEditableLista.Add(new ProductoMenu
        {
            Nombre = "Nuevo Producto",
            Precio = 0,
            Categoria = "Comidas",
            ColorFondo = "#3f6ad8",
            ColorBorde = "#3458b3"
        });

        EstadoMenuEdicion = "Nueva fila agregada al editor de menu";
    }

    [RelayCommand]
    private void EliminarProductoMenuLista(ProductoMenu? producto)
    {
        if (producto is null)
        {
            return;
        }

        MenuEditableLista.Remove(producto);
        EstadoMenuEdicion = "Fila eliminada";
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
    private void GuardarMenuJson()
    {
        try
        {
            var productos = NormalizarProductos(MenuEditableLista);
            File.WriteAllText(_rutaMenuJson, SerializarProductos(productos));
            AplicarCatalogo(productos);
            EstadoMenuEdicion = $"Guardado OK ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            EstadoMenuEdicion = $"Error al guardar: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RecargarMenuJson()
    {
        RecargarMenuDesdeArchivoInterno();
    }

    [RelayCommand]
    private void RestaurarMenuBase()
    {
        try
        {
            var baseJson = LeerMenuBaseEmbebido();
            File.WriteAllText(_rutaMenuJson, baseJson);
            RecargarMenuDesdeArchivoInterno();
            EstadoMenuEdicion = "Menu restaurado a la plantilla base";
        }
        catch (Exception ex)
        {
            EstadoMenuEdicion = $"Error al restaurar: {ex.Message}";
        }
    }

    partial void OnTemaActualChanged(string value)
    {
        OnPropertyChanged(nameof(FondoVentana));
        OnPropertyChanged(nameof(FondoBarra));
        OnPropertyChanged(nameof(FondoPanel));
        OnPropertyChanged(nameof(FondoPanelSecundario));
        OnPropertyChanged(nameof(ColorBorde));
        OnPropertyChanged(nameof(ColorTextoPrincipal));
        OnPropertyChanged(nameof(ColorTextoSecundario));
        OnPropertyChanged(nameof(ColorAcento));
        OnPropertyChanged(nameof(ColorAcentoBorde));
        OnPropertyChanged(nameof(ColorCambio));
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

    private void AsegurarArchivoMenuEditable()
    {
        var directorio = Path.GetDirectoryName(_rutaMenuJson);
        if (!string.IsNullOrWhiteSpace(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        if (!File.Exists(_rutaMenuJson))
        {
            File.WriteAllText(_rutaMenuJson, LeerMenuBaseEmbebido());
        }
    }

    private string LeerMenuBaseEmbebido()
    {
        var uri = new Uri("avares://MiniCafeteria/Assets/Data/menu-productos.json");
        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private void RecargarMenuDesdeArchivoInterno()
    {
        try
        {
            var json = File.ReadAllText(_rutaMenuJson);
            var productos = ParsearProductosDesdeJson(json);
            AplicarCatalogo(productos);
            EstadoMenuEdicion = $"Menu cargado ({productos.Count} productos)";
        }
        catch (Exception ex)
        {
            EstadoMenuEdicion = $"Error al cargar menu: {ex.Message}";
        }
    }

    private List<ProductoMenu> ParsearProductosDesdeJson(string json)
    {
        var productos = JsonSerializer.Deserialize<List<ProductoMenu>>(json, _jsonOptions);
        if (productos is null)
        {
            throw new InvalidOperationException("El JSON no contiene productos.");
        }

        return NormalizarProductos(productos);
    }

    private List<ProductoMenu> NormalizarProductos(IEnumerable<ProductoMenu> productos)
    {
        var normalizados = new List<ProductoMenu>();

        foreach (var p in productos)
        {
            var nombre = (p.Nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                continue;
            }

            var categoria = string.IsNullOrWhiteSpace(p.Categoria) ? "Comidas" : p.Categoria.Trim();
            var colorFondo = string.IsNullOrWhiteSpace(p.ColorFondo) ? "#3498db" : p.ColorFondo.Trim();
            var colorBorde = string.IsNullOrWhiteSpace(p.ColorBorde) ? "#2c7bb5" : p.ColorBorde.Trim();
            var precio = p.Precio < 0 ? 0 : p.Precio;

            normalizados.Add(new ProductoMenu
            {
                Nombre = nombre,
                Precio = precio,
                Categoria = categoria,
                ColorFondo = colorFondo,
                ColorBorde = colorBorde
            });
        }

        if (normalizados.Count == 0)
        {
            throw new InvalidOperationException("No hay productos validos para mostrar.");
        }

        return normalizados;
    }

    private string SerializarProductos(List<ProductoMenu> productos)
    {
        return JsonSerializer.Serialize(productos, _jsonOptions);
    }

    private void AplicarCatalogo(List<ProductoMenu> productos)
    {
        CatalogoMenu.Clear();
        MenuEditableLista.Clear();

        foreach (var producto in productos)
        {
            var catalogoItem = new ProductoMenu
            {
                Nombre = producto.Nombre,
                Precio = producto.Precio,
                Categoria = producto.Categoria,
                ColorFondo = producto.ColorFondo,
                ColorBorde = producto.ColorBorde
            };

            CatalogoMenu.Add(catalogoItem);

            MenuEditableLista.Add(new ProductoMenu
            {
                Nombre = producto.Nombre,
                Precio = producto.Precio,
                Categoria = producto.Categoria,
                ColorFondo = producto.ColorFondo,
                ColorBorde = producto.ColorBorde
            });
        }

        OnPropertyChanged(nameof(BebidasFiltradas));
        OnPropertyChanged(nameof(ComidasFiltradas));
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

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) ||
               decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
}
