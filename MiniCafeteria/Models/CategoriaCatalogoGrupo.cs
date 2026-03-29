using System.Collections.Generic;

namespace MiniCafeteria.Models;

public class CategoriaCatalogoGrupo
{
    public string Nombre { get; set; } = string.Empty;

    public IReadOnlyList<ProductoMenu> Productos { get; set; } = [];
}
