using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Envio;

public sealed record ResultadoEnvio(bool Ok, string Detalle);

/// <summary>A dónde se manda la declaración: simulación (archivo) o Fusion (API real).</summary>
public interface IEnvioDeclaracion
{
    /// <summary>
    /// Cantidad que todavía se puede declarar en la OT de Fusion (ReadyQuantity).
    /// null = sin límite (simulación).
    /// </summary>
    Task<int?> ObtenerDisponibleAsync(string otFusion, CancellationToken ct);

    Task<ResultadoEnvio> EnviarAsync(MotoDeclarada moto, DeclaracionPT declaracion, CancellationToken ct);
}
