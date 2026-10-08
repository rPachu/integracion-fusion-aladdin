using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Envio;

/// <summary>
/// Modo simulación: en lugar de enviar a Fusion, guarda cada declaración como un archivo JSON
/// en la carpeta de salida. Es el modo a usar hasta tener las credenciales de Fusion,
/// y sirve para que Newsan valide las declaraciones en las pruebas.
/// </summary>
public sealed class EnvioSimulacion : IEnvioDeclaracion
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // para que "/" y las tildes se vean tal cual
    };

    private readonly string _carpeta;

    public EnvioSimulacion(string carpeta)
    {
        _carpeta = carpeta;
        Directory.CreateDirectory(carpeta);
    }

    // En simulación no se consulta Fusion: no hay límite de cantidad.
    public Task<int?> ObtenerDisponibleAsync(string otFusion, CancellationToken ct) => Task.FromResult<int?>(null);

    public async Task<ResultadoEnvio> EnviarAsync(MotoDeclarada moto, DeclaracionPT declaracion, CancellationToken ct)
    {
        var nombre = $"{DateTime.Now:yyyyMMdd_HHmmss}_{ParaNombre(moto.OtFusion)}_{ParaNombre(moto.NroSerie)}.json";
        var ruta = Path.Combine(_carpeta, nombre);
        await File.WriteAllTextAsync(ruta, JsonSerializer.Serialize(declaracion, OpcionesJson), ct);
        return new ResultadoEnvio(true, $"Simulación: JSON guardado en {ruta}");
    }

    // Reemplaza los caracteres que no pueden ir en un nombre de archivo (ej. "/" de la serie 073-…/2026).
    private static string ParaNombre(string texto)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        return new string(texto.Select(c => invalidos.Contains(c) ? '-' : c).ToArray());
    }
}
