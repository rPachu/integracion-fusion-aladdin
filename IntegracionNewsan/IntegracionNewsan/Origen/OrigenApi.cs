using IntegracionNewsan.Capataz;
using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Origen;

/// <summary>Lee las motos de la API de CAPATAZ (uso normal).</summary>
public sealed class OrigenApi(CapatazClient capataz) : IOrigenMotos
{
    public string Descripcion => "API de CAPATAZ";

    public Task<List<MotoDeclarada>> ObtenerAsync(CancellationToken ct) => capataz.EjecutarConsultaAsync(ct);
}
