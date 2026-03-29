using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _rutaMenuJson;
    private readonly string _rutaVentasJson;
    private readonly string _rutaNegocioJson;
    private int _ultimoVentaId;

    public ObservableCollection<ItemVenta> ListaFactura { get; } = new();

    public ObservableCollection<ProductoMenu> CatalogoMenu { get; } = new();

    public ObservableCollection<ProductoMenu> MenuEditableLista { get; } = new();

    public ObservableCollection<Venta> HistorialVentasDia { get; } = new();

    public IReadOnlyList<string> TemasDisponibles { get; } = new[] { "Azul", "Verde", "Naranja", "Gris" };

    public IReadOnlyList<string> MetodosPagoDisponibles { get; } = new[] { "Efectivo", "Tarjeta" };

    [ObservableProperty]
    private decimal _montoRecibido;

    [ObservableProperty]
    private string _buscarTexto = string.Empty;

    [ObservableProperty]
    private string _estadoMenuEdicion = "Menu cargado";

    [ObservableProperty]
    private string _estadoCobro = "Agrega productos para comenzar una venta";

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

    [ObservableProperty]
    private string _metodoPagoSeleccionado = "Efectivo";

    [ObservableProperty]
    private bool _confirmandoCobro;

    [ObservableProperty]
    private bool _mostrarEditorNegocio;

    [ObservableProperty]
    private string _estadoNegocio = string.Empty;

    [ObservableProperty]
    private string _nombreNegocio = "Mi Cafeteria";

    [ObservableProperty]
    private string _esloganNegocio = string.Empty;

    [ObservableProperty]
    private string _direccionNegocio = string.Empty;

    [ObservableProperty]
    private string _telefonoNegocio = string.Empty;

    [ObservableProperty]
    private string _emailNegocio = string.Empty;

    [ObservableProperty]
    private string _rifNegocio = string.Empty;

    [ObservableProperty]
    private string _piePaginaNegocio = "¡Gracias por su visita!";

    public MainWindowViewModel()
    {
        var directorioDatos = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniCafeteria");

        _rutaMenuJson = Path.Combine(directorioDatos, "menu-productos.json");
        _rutaVentasJson = Path.Combine(directorioDatos, "ventas.json");
        _rutaNegocioJson = Path.Combine(directorioDatos, "negocio.json");

        ListaFactura.CollectionChanged += OnListaFacturaCollectionChanged;

        AsegurarArchivosDatos();
        RecargarMenuDesdeArchivoInterno();
        CargarHistorialVentas();
        CargarDatosNegocio();
        ActualizarEstadoCobro();
    }

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

    public decimal Cambio =>
        RequiereMontoRecibido && MontoRecibido > TotalVentaActual
            ? MontoRecibido - TotalVentaActual
            : 0;

    public bool RequiereMontoRecibido => MetodoPagoSeleccionado == "Efectivo";

    public bool MostrarAvisoTarjeta => !RequiereMontoRecibido;

    public string TextoBotonFinalizar => ConfirmandoCobro ? "CONFIRMAR COBRO" : "FINALIZAR Y COBRAR";

    public int CantidadVentasHoy => HistorialVentasDia.Count;

    public decimal TotalVentasHoy => HistorialVentasDia.Sum(x => x.Total);

    public IEnumerable<CategoriaCatalogoGrupo> CategoriasCatalogo =>
        FiltrarCatalogo()
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Categoria) ? "Sin categoria" : p.Categoria.Trim())
            .Select(g => new CategoriaCatalogoGrupo
            {
                Nombre = g.Key.ToUpperInvariant(),
                Productos = g.ToList()
            });

    [RelayCommand]
    private void AgregarProducto(ProductoMenu? producto)
    {
        if (producto is null)
        {
            return;
        }

        AgregarOIncrementarItem(producto.Nombre, producto.Precio, 1);
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

        if (!int.TryParse(NuevoItemCantidadTexto, NumberStyles.Integer, CultureInfo.CurrentCulture, out var cantidad) || cantidad <= 0)
        {
            EstadoAgregarItem = "Cantidad invalida";
            return;
        }

        AgregarOIncrementarItem(nombre, precio < 0 ? 0 : precio, cantidad);
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
    private void CancelarConfirmacionCobro()
    {
        ConfirmandoCobro = false;
        ActualizarEstadoCobro();
    }

    [RelayCommand]
    private void FinalizarVenta()
    {
        if (ListaFactura.Count == 0)
        {
            EstadoCobro = "No hay items en la venta actual";
            ConfirmandoCobro = false;
            return;
        }

        if (RequiereMontoRecibido && MontoRecibido < TotalVentaActual)
        {
            EstadoCobro = "El monto recibido no puede ser menor al total para ventas en efectivo";
            ConfirmandoCobro = false;
            return;
        }

        if (!ConfirmandoCobro)
        {
            ConfirmandoCobro = true;
            EstadoCobro = $"Confirma el cobro por {TotalVentaActual:C2} con {MetodoPagoSeleccionado.ToLowerInvariant()}";
            OnPropertyChanged(nameof(TextoBotonFinalizar));
            return;
        }

        var totalCobrado = TotalVentaActual;
        var htmlFactura = GenerarHtmlFactura();
        RegistrarVentaActual();
        LimpiarVentaActual();
        ConfirmandoCobro = false;
        EstadoCobro = $"Venta guardada. Total cobrado: {totalCobrado:C2}";
        OnPropertyChanged(nameof(TextoBotonFinalizar));
        AbrirHtmlFactura(htmlFactura);
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

    [RelayCommand]
    private void ToggleEditorNegocio()
    {
        MostrarEditorNegocio = !MostrarEditorNegocio;
    }

    [RelayCommand]
    private void GuardarDatosNegocio()
    {
        try
        {
            var datos = new DatosNegocio
            {
                NombreNegocio = NombreNegocio.Trim(),
                Eslogan = EsloganNegocio.Trim(),
                Direccion = DireccionNegocio.Trim(),
                Telefono = TelefonoNegocio.Trim(),
                Email = EmailNegocio.Trim(),
                Rif = RifNegocio.Trim(),
                PiePagina = PiePaginaNegocio.Trim()
            };

            File.WriteAllText(_rutaNegocioJson, JsonSerializer.Serialize(datos, JsonOptions), Encoding.UTF8);
            EstadoNegocio = $"Guardado ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception ex)
        {
            EstadoNegocio = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ImprimirFactura()
    {
        if (ListaFactura.Count == 0)
        {
            EstadoCobro = "No hay items para imprimir";
            return;
        }

        AbrirHtmlFactura(GenerarHtmlFactura());
    }

    private void AbrirHtmlFactura(string html)
    {
        try
        {
            var archivo = Path.Combine(Path.GetTempPath(), $"factura_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            File.WriteAllText(archivo, html, Encoding.UTF8);
            Process.Start(new ProcessStartInfo(archivo) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            EstadoCobro = $"Error al generar factura: {ex.Message}";
        }
    }

    partial void OnTemaActualChanged(string value)
    {
        OnPropertyChanged(nameof(FondoVentana));
        OnPropertyChanged(nameof(FondoBarra));
        OnPropertyChanged(nameof(FondoPanel));
        OnPropertyChanged(nameof(FondoPanelSecundario));
        OnPropertyChanged(nameof(ColorBorde));
        OnPropertyChanged(nameof(ColorAcento));
        OnPropertyChanged(nameof(ColorAcentoBorde));
        OnPropertyChanged(nameof(ColorCambio));
    }

    partial void OnBuscarTextoChanged(string value)
    {
        OnPropertyChanged(nameof(CategoriasCatalogo));
    }

    partial void OnMontoRecibidoChanged(decimal value)
    {
        InvalidarConfirmacionCobro();
        OnPropertyChanged(nameof(Cambio));
        ActualizarEstadoCobro();
    }

    partial void OnMetodoPagoSeleccionadoChanged(string value)
    {
        InvalidarConfirmacionCobro();
        OnPropertyChanged(nameof(RequiereMontoRecibido));
        OnPropertyChanged(nameof(MostrarAvisoTarjeta));
        OnPropertyChanged(nameof(Cambio));
        ActualizarEstadoCobro();
    }

    private IEnumerable<ProductoMenu> FiltrarCatalogo()
    {
        if (string.IsNullOrWhiteSpace(BuscarTexto))
        {
            return CatalogoMenu;
        }

        return CatalogoMenu.Where(p => p.Nombre.Contains(BuscarTexto, StringComparison.OrdinalIgnoreCase));
    }

    private void AsegurarArchivosDatos()
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

        if (!File.Exists(_rutaVentasJson))
        {
            File.WriteAllText(_rutaVentasJson, "[]");
        }
    }

    private void CargarDatosNegocio()
    {
        try
        {
            if (!File.Exists(_rutaNegocioJson))
            {
                return;
            }

            var json = File.ReadAllText(_rutaNegocioJson);
            var datos = JsonSerializer.Deserialize<DatosNegocio>(json, JsonOptions);
            if (datos is null)
            {
                return;
            }

            NombreNegocio = datos.NombreNegocio;
            EsloganNegocio = datos.Eslogan;
            DireccionNegocio = datos.Direccion;
            TelefonoNegocio = datos.Telefono;
            EmailNegocio = datos.Email;
            RifNegocio = datos.Rif;
            PiePaginaNegocio = datos.PiePagina;
        }
        catch
        {
            // Usar valores por defecto si el archivo está corrupto
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

    private void CargarHistorialVentas()
    {
        try
        {
            var ventas = LeerVentasDesdeArchivo();
            _ultimoVentaId = ventas.Count == 0 ? 0 : ventas.Max(v => v.Id);

            HistorialVentasDia.Clear();
            foreach (var venta in ventas.Where(v => v.Fecha.Date == DateTime.Today))
            {
                HistorialVentasDia.Add(venta);
            }

            OnPropertyChanged(nameof(CantidadVentasHoy));
            OnPropertyChanged(nameof(TotalVentasHoy));
        }
        catch
        {
            HistorialVentasDia.Clear();
            _ultimoVentaId = 0;
            OnPropertyChanged(nameof(CantidadVentasHoy));
            OnPropertyChanged(nameof(TotalVentasHoy));
        }
    }

    private List<ProductoMenu> ParsearProductosDesdeJson(string json)
    {
        var productos = JsonSerializer.Deserialize<List<ProductoMenu>>(json, JsonOptions) ?? [];
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

            var categoria = string.IsNullOrWhiteSpace(p.Categoria) ? "Sin categoria" : p.Categoria.Trim();
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

        return normalizados;
    }

    private string SerializarProductos(List<ProductoMenu> productos)
    {
        return JsonSerializer.Serialize(productos, JsonOptions);
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

        OnPropertyChanged(nameof(CategoriasCatalogo));
    }

    private void AgregarOIncrementarItem(string nombre, decimal precioUnitario, int cantidad)
    {
        var existente = ListaFactura.FirstOrDefault(item =>
            string.Equals(item.Nombre, nombre, StringComparison.OrdinalIgnoreCase) &&
            item.PrecioUnitario == precioUnitario);

        if (existente is not null)
        {
            existente.Cantidad += cantidad;
            return;
        }

        ListaFactura.Add(new ItemVenta
        {
            Nombre = nombre,
            PrecioUnitario = precioUnitario,
            Cantidad = cantidad
        });
    }

    private void RegistrarVentaActual()
    {
        var totalCobrado = TotalVentaActual;
        var montoRecibido = RequiereMontoRecibido ? MontoRecibido : totalCobrado;
        var venta = new Venta
        {
            Id = ++_ultimoVentaId,
            Fecha = DateTime.Now,
            Total = totalCobrado,
            MontoRecibido = montoRecibido,
            Cambio = RequiereMontoRecibido ? Cambio : 0,
            MetodoPago = MetodoPagoSeleccionado,
            Detalles = ListaFactura.Select(item => new DetalleVenta
            {
                Nombre = item.Nombre,
                PrecioUnitario = item.PrecioUnitario,
                Cantidad = item.Cantidad
            }).ToList()
        };

        var ventas = LeerVentasDesdeArchivo();
        ventas.Add(venta);
        File.WriteAllText(_rutaVentasJson, JsonSerializer.Serialize(ventas, JsonOptions));

        if (venta.Fecha.Date == DateTime.Today)
        {
            HistorialVentasDia.Add(venta);
            OnPropertyChanged(nameof(CantidadVentasHoy));
            OnPropertyChanged(nameof(TotalVentasHoy));
        }
    }

    private List<Venta> LeerVentasDesdeArchivo()
    {
        if (!File.Exists(_rutaVentasJson))
        {
            return [];
        }

        var json = File.ReadAllText(_rutaVentasJson);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<Venta>>(json, JsonOptions) ?? [];
    }

    private void LimpiarVentaActual()
    {
        foreach (var item in ListaFactura)
        {
            item.PropertyChanged -= OnItemVentaPropertyChanged;
        }

        ListaFactura.Clear();
        MontoRecibido = 0;
        MetodoPagoSeleccionado = "Efectivo";
        ConfirmandoCobro = false;
        OnPropertyChanged(nameof(TextoBotonFinalizar));
        ActualizarEstadoCobro();
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

        InvalidarConfirmacionCobro();
        NotificarTotales();
        ActualizarEstadoCobro();
    }

    private void OnItemVentaPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ItemVenta.Cantidad) ||
            e.PropertyName == nameof(ItemVenta.PrecioUnitario) ||
            e.PropertyName == nameof(ItemVenta.Subtotal) ||
            e.PropertyName == nameof(ItemVenta.Nombre))
        {
            InvalidarConfirmacionCobro();
            NotificarTotales();
            ActualizarEstadoCobro();
        }
    }

    private void NotificarTotales()
    {
        OnPropertyChanged(nameof(TotalVentaActual));
        OnPropertyChanged(nameof(Cambio));
    }

    private void InvalidarConfirmacionCobro()
    {
        if (!ConfirmandoCobro)
        {
            return;
        }

        ConfirmandoCobro = false;
        OnPropertyChanged(nameof(TextoBotonFinalizar));
    }

    private void ActualizarEstadoCobro()
    {
        if (ListaFactura.Count == 0)
        {
            EstadoCobro = "Agrega productos para comenzar una venta";
            return;
        }

        if (RequiereMontoRecibido && MontoRecibido < TotalVentaActual)
        {
            EstadoCobro = $"Faltan {(TotalVentaActual - MontoRecibido):C2} para completar el cobro en efectivo";
            return;
        }

        EstadoCobro = RequiereMontoRecibido
            ? $"Cobro listo. Cambio estimado: {Cambio:C2}"
            : "Cobro listo para tarjeta";
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) ||
               decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private string GenerarHtmlFactura()
    {
        var sb = new StringBuilder();
        var ahora = DateTime.Now;

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='es'><head><meta charset='utf-8'/>");
        sb.AppendLine("<title>Factura</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("* { box-sizing: border-box; margin: 0; padding: 0; }");
        sb.AppendLine("body { font-family: 'Courier New', Courier, monospace; width: 80mm; margin: 0 auto; padding: 8mm; font-size: 12px; }");
        sb.AppendLine(".center { text-align: center; }");
        sb.AppendLine(".negocio { font-size: 16px; font-weight: bold; margin-bottom: 2px; }");
        sb.AppendLine(".eslogan { font-size: 11px; color: #555; margin-bottom: 4px; }");
        sb.AppendLine(".info { font-size: 11px; margin-bottom: 2px; }");
        sb.AppendLine(".sep-dash { border: none; border-top: 1px dashed #000; margin: 6px 0; }");
        sb.AppendLine(".sep-solid { border: none; border-top: 2px solid #000; margin: 6px 0; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 4px 0; }");
        sb.AppendLine("th { border-bottom: 1px solid #000; text-align: left; padding: 2px 0; font-size: 11px; }");
        sb.AppendLine("td { padding: 2px 0; vertical-align: top; }");
        sb.AppendLine(".right { text-align: right; }");
        sb.AppendLine(".item-nombre { width: 40%; }");
        sb.AppendLine(".total-bloque { margin-top: 4px; }");
        sb.AppendLine(".total-row { display: flex; justify-content: space-between; font-size: 14px; font-weight: bold; margin: 4px 0; }");
        sb.AppendLine(".subtotal-row { display: flex; justify-content: space-between; font-size: 12px; margin: 2px 0; }");
        sb.AppendLine(".footer { margin-top: 10px; font-size: 11px; color: #444; }");
        sb.AppendLine("@media print { @page { margin: 0; size: 80mm auto; } body { padding: 5mm; } }");
        sb.AppendLine("</style></head><body>");

        // Encabezado
        sb.AppendLine("<div class='center'>");
        sb.AppendLine($"<div class='negocio'>{EscaparHtml(NombreNegocio)}</div>");
        if (!string.IsNullOrWhiteSpace(EsloganNegocio))
            sb.AppendLine($"<div class='eslogan'>{EscaparHtml(EsloganNegocio)}</div>");
        if (!string.IsNullOrWhiteSpace(RifNegocio))
            sb.AppendLine($"<div class='info'>RIF: {EscaparHtml(RifNegocio)}</div>");
        if (!string.IsNullOrWhiteSpace(DireccionNegocio))
            sb.AppendLine($"<div class='info'>{EscaparHtml(DireccionNegocio)}</div>");
        if (!string.IsNullOrWhiteSpace(TelefonoNegocio))
            sb.AppendLine($"<div class='info'>Tel: {EscaparHtml(TelefonoNegocio)}</div>");
        if (!string.IsNullOrWhiteSpace(EmailNegocio))
            sb.AppendLine($"<div class='info'>{EscaparHtml(EmailNegocio)}</div>");
        sb.AppendLine("</div>");

        sb.AppendLine("<hr class='sep-solid'/>");
        sb.AppendLine($"<div>Fecha: {ahora:dd/MM/yyyy HH:mm}</div>");
        sb.AppendLine($"<div>Metodo de pago: {EscaparHtml(MetodoPagoSeleccionado)}</div>");
        sb.AppendLine("<hr class='sep-dash'/>");

        // Items
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th class='item-nombre'>Descripcion</th><th class='right'>Cant</th><th class='right'>P.Unit</th><th class='right'>Subtotal</th></tr>");
        foreach (var item in ListaFactura)
        {
            sb.AppendLine($"<tr><td class='item-nombre'>{EscaparHtml(item.Nombre)}</td><td class='right'>{item.Cantidad}</td><td class='right'>{item.PrecioUnitario:N2}</td><td class='right'>{item.Subtotal:N2}</td></tr>");
        }
        sb.AppendLine("</table>");

        sb.AppendLine("<hr class='sep-solid'/>");

        // Totales
        sb.AppendLine("<div class='total-bloque'>");
        sb.AppendLine($"<div class='total-row'><span>TOTAL:</span><span>{TotalVentaActual:N2}</span></div>");
        if (RequiereMontoRecibido)
        {
            sb.AppendLine($"<div class='subtotal-row'><span>Efectivo recibido:</span><span>{MontoRecibido:N2}</span></div>");
            sb.AppendLine($"<div class='subtotal-row'><span>Cambio:</span><span>{Cambio:N2}</span></div>");
        }
        sb.AppendLine("</div>");

        // Pie
        if (!string.IsNullOrWhiteSpace(PiePaginaNegocio))
        {
            sb.AppendLine("<hr class='sep-dash'/>");
            sb.AppendLine($"<div class='center footer'>{EscaparHtml(PiePaginaNegocio)}</div>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string EscaparHtml(string texto)
    {
        return texto
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
