namespace MiniCafeteria.Models;

public class ProductoMenu
{
    public string Nombre { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public string Categoria { get; init; } = string.Empty;
    public string ColorFondo { get; init; } = "#3498db";
    public string ColorBorde { get; init; } = "#2c7bb5";
}
