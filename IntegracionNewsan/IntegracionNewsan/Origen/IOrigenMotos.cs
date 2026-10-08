using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Origen;

/// <summary>De dónde se leen las motos declaradas.</summary>
public interface IOrigenMotos
{
    string Descripcion { get; }
    Task<List<MotoDeclarada>> ObtenerAsync(CancellationToken ct);
}
