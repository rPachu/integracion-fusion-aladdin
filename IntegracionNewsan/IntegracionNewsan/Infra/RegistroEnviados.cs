using System.Text.Json;
using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Infra;

/// <summary>
/// Registro de las motos ya enviadas (estado\enviados.json), para no declarar dos veces la misma.
/// Para repetir una prueba desde cero, borrar ese archivo.
/// </summary>
public sealed class RegistroEnviados
{
    private static readonly JsonSerializerOptions OpcionesJson = new() { WriteIndented = true };

    private readonly string _archivo;
    private readonly Dictionary<string, EntradaRegistro> _entradas;

    public RegistroEnviados(string carpeta)
    {
        Directory.CreateDirectory(carpeta);
        _archivo = Path.Combine(carpeta, "enviados.json");

        var existentes = File.Exists(_archivo)
            ? JsonSerializer.Deserialize<List<EntradaRegistro>>(File.ReadAllText(_archivo)) ?? []
            : [];

        _entradas = new Dictionary<string, EntradaRegistro>();
        foreach (var entrada in existentes) _entradas[entrada.Clave] = entrada;
    }

    public bool YaEnviado(string clave) => _entradas.ContainsKey(clave);

    public void Registrar(MotoDeclarada moto, string detalle)
    {
        _entradas[moto.Clave] = new EntradaRegistro
        {
            Clave = moto.Clave,
            OtCapataz = moto.NroOt,
            OtFusion = moto.OtFusion,
            NroSerie = moto.NroSerie,
            Vin = moto.Vin,
            NroMotor = moto.NroMotor,
            FechaEnvio = DateTime.Now,
            Detalle = detalle
        };
        Guardar();
    }

    // Se escribe en un archivo temporal y después se reemplaza, para no dejar el registro a medio escribir.
    private void Guardar()
    {
        var temporal = _archivo + ".tmp";
        File.WriteAllText(temporal, JsonSerializer.Serialize(_entradas.Values.ToList(), OpcionesJson));
        File.Move(temporal, _archivo, overwrite: true);
    }
}

public sealed class EntradaRegistro
{
    public string Clave { get; set; } = "";
    public string OtCapataz { get; set; } = "";
    public string OtFusion { get; set; } = "";
    public string NroSerie { get; set; } = "";
    public string Vin { get; set; } = "";
    public string NroMotor { get; set; } = "";
    public DateTime FechaEnvio { get; set; }
    public string Detalle { get; set; } = "";
}
