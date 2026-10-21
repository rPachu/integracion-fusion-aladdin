using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Envio;

public sealed record ResultadoEnvio(bool Ok, string Detalle);

/// <summary>
/// Datos de la orden en Fusion antes de declarar.
/// Disponible = ReadyQuantity (null = sin límite, ej. simulación).
/// SecuenciaOperacion = secuencia de la operación donde se declara (null = usar la configurada).
/// </summary>
public sealed record InfoOrden(int? Disponible, int? SecuenciaOperacion, string Detalle);

/// <summary>A dónde se manda la declaración: simulación (archivo) o Fusion (API real).</summary>
public interface IEnvioDeclaracion
{
    /// <summary>Consulta la orden en Fusion. Lanza excepción si la orden no existe o no se pudo consultar.</summary>
    Task<InfoOrden> ConsultarOrdenAsync(string otFusion, CancellationToken ct);

    Task<ResultadoEnvio> EnviarAsync(MotoDeclarada moto, DeclaracionPT declaracion, CancellationToken ct);
}
