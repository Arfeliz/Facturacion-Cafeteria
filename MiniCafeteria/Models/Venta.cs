using System;
using System.Collections.Generic;

namespace MiniCafeteria.Models;

public class Venta
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    public decimal Total { get; set; }

    public decimal MontoRecibido { get; set; }

    public decimal Cambio { get; set; }

    public string MetodoPago { get; set; } = "Efectivo";

    public List<DetalleVenta> Detalles { get; set; } = new();
}
