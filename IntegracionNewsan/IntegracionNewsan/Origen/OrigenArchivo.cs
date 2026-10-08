using System.Text.Json;
using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Origen;

/// <summary>
/// Lee las motos de un archivo JSON con el mismo formato que devuelve la API.
/// Sirve para probar el programa sin acceso a la API de CAPATAZ.
/// </summary>
public sealed class OrigenArchivo(string ruta) : IOrigenMotos
{
    private static readonly JsonSerializerOptions OpcionesJson = new() { PropertyNameCaseInsensitive = true };

    public string Descripcion => $"archivo de prueba {ruta}";

    public async Task<List<MotoDeclarada>> ObtenerAsync(CancellationToken ct)
    {
        if (!File.Exists(ruta))
            throw new FileNotFoundException("No se encontró el archivo de prueba.", ruta);

        await using var archivo = File.OpenRead(ruta);
        return await JsonSerializer.DeserializeAsync<List<MotoDeclarada>>(archivo, OpcionesJson, ct) ?? [];
    }
}
