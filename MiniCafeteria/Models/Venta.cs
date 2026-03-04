using System;

namespace MiniCafeteria.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public string? MetodoPago { get; set; } // Efectivo o Tarjeta
    }

    
}