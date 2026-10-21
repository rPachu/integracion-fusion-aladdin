using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntegracionNewsan.Configuracion;
using IntegracionNewsan.Infra;
using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Envio;

/// <summary>
/// Envío real a Oracle Fusion (Newsan), con autenticación básica (usuario y contraseña del usuario técnico).
///
/// 1. ConsultarOrdenAsync:
///    GET {UrlBase}/fscmRestApi/resources/{Version}/workOrders
///        ?q=OrganizationCode='{org}';WorkOrderNumber='{ot}'&amp;expand=WorkOrderOperation&amp;onlyData=true
///    De la operación se toma ReadyQuantity (lo que se puede declarar) y la secuencia de la operación.
///
/// 2. EnviarAsync:
///    POST {UrlBase}/fscmRestApi/resources/{Version}/operationTransactions con el JSON de la declaración.
///
/// Cada pedido y cada respuesta se guardan en la carpeta salida\fusion\ para auditoría y diagnóstico.
/// </summary>
public sealed class EnvioFusion : IEnvioDeclaracion, IDisposable
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _http;
    private readonly FusionOpciones _op;
    private readonly string _organizacion;
    private readonly int _secuenciaConfigurada;
    private readonly string _carpeta;
    private readonly Log _log;

    public EnvioFusion(FusionOpciones op, DeclaracionOpciones declaracion, string carpetaSalida, Log log)
    {
        if (string.IsNullOrWhiteSpace(op.UrlBase) || string.IsNullOrWhiteSpace(op.Usuario) || string.IsNullOrWhiteSpace(op.Password))
            throw new InvalidOperationException(
                "Faltan datos de Fusion: UrlBase y Usuario en appsettings.json, Password en appsettings.Local.json.");

        _op = op;
        _organizacion = declaracion.OrganizationCode;
        _secuenciaConfigurada = declaracion.WoOperationSequenceNumber;
        _log = log;
        _carpeta = Path.Combine(carpetaSalida, "fusion");
        Directory.CreateDirectory(_carpeta);

        _http = new HttpClient
        {
            BaseAddress = new Uri(op.UrlBase.TrimEnd('/') + $"/fscmRestApi/resources/{op.VersionApi}/"),
            Timeout = TimeSpan.FromSeconds(op.TimeoutSegundos)
        };
        var credenciales = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{op.Usuario}:{op.Password}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credenciales);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<InfoOrden> ConsultarOrdenAsync(string otFusion, CancellationToken ct)
    {
        // Las condiciones se separan con ";" (probado en TEST el 21/10: con "and" Fusion devuelve vacío).
        var q = Uri.EscapeDataString($"OrganizationCode='{_organizacion}';WorkOrderNumber='{otFusion}'");
        var url = $"workOrders?q={q}&expand=WorkOrderOperation&onlyData=true";

        using var respuesta = await _http.GetAsync(url, ct);
        var texto = await respuesta.Content.ReadAsStringAsync(ct);
        Guardar($"orden_{otFusion}", "respuesta", texto);

        if (!respuesta.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Fusion respondió HTTP {(int)respuesta.StatusCode} al consultar la orden {otFusion}. {Recortar(texto)}");

        using var doc = JsonDocument.Parse(texto);
        if (!doc.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
            throw new InvalidOperationException(
                $"La orden {otFusion} no existe en Fusion para la organización {_organizacion}. Revisar la Leyenda 1 de la OT.");

        var orden = items[0];
        if (!orden.TryGetProperty("WorkOrderOperation", out var operaciones) || operaciones.GetArrayLength() == 0)
            throw new InvalidOperationException($"La orden {otFusion} no trae operaciones en la respuesta de Fusion.");

        // Se usa la operación con la secuencia configurada; si no existe, la última (la de mayor secuencia).
        JsonElement? elegida = null;
        foreach (var o in operaciones.EnumerateArray())
            if (Numero(o, "OperationSequenceNumber") == _secuenciaConfigurada) elegida = o;
        elegida ??= operaciones.EnumerateArray().OrderBy(o => Numero(o, "OperationSequenceNumber") ?? 0).Last();

        var operacion = elegida.Value;
        var secuencia = Numero(operacion, "OperationSequenceNumber");
        var lista = Numero(operacion, "ReadyQuantity");
        var completa = Numero(operacion, "CompletedQuantity");
        var recibida = Numero(operacion, "ReceivedQuantity");

        var detalle = $"Orden {otFusion}: operación {secuencia?.ToString() ?? "?"} · ReadyQuantity {lista?.ToString() ?? "?"} · " +
                      $"CompletedQuantity {completa?.ToString() ?? "?"} · ReceivedQuantity {recibida?.ToString() ?? "?"}";

        if (lista is null)
            _log.Advertencia($"{detalle}. No vino ReadyQuantity: no se puede controlar el disponible (ver salida\\fusion).");

        return new InfoOrden(lista, secuencia, detalle);
    }

    public async Task<ResultadoEnvio> EnviarAsync(MotoDeclarada moto, DeclaracionPT declaracion, CancellationToken ct)
    {
        var cuerpo = JsonSerializer.Serialize(declaracion, OpcionesJson);
        var nombre = $"{moto.OtFusion}_{moto.NroSerie}";
        Guardar(nombre, "envio", cuerpo);

        using var contenido = new StringContent(cuerpo, Encoding.UTF8, "application/json");
        using var respuesta = await _http.PostAsync("operationTransactions", contenido, ct);
        var texto = await respuesta.Content.ReadAsStringAsync(ct);
        var archivo = Guardar(nombre, "respuesta", texto);

        return respuesta.IsSuccessStatusCode
            ? new ResultadoEnvio(true, $"Fusion respondió HTTP {(int)respuesta.StatusCode}. Respuesta en {archivo}")
            : new ResultadoEnvio(false, $"Fusion respondió HTTP {(int)respuesta.StatusCode}: {Recortar(texto)} (completo en {archivo})");
    }

    private string Guardar(string nombre, string tipo, string contenido)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var limpio = new string(nombre.Select(c => invalidos.Contains(c) ? '-' : c).ToArray());
        var ruta = Path.Combine(_carpeta, $"{DateTime.Now:yyyyMMdd_HHmmss}_{limpio}_{tipo}.json");
        File.WriteAllText(ruta, contenido);
        return ruta;
    }

    private static int? Numero(JsonElement e, string propiedad)
    {
        foreach (var p in e.EnumerateObject())
        {
            if (!string.Equals(p.Name, propiedad, StringComparison.OrdinalIgnoreCase)) continue;
            if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetDecimal(out var d)) return (int)d;
            if (p.Value.ValueKind == JsonValueKind.String && decimal.TryParse(p.Value.GetString(),
                    System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var s)) return (int)s;
        }
        return null;
    }

    private static string Recortar(string texto) => texto.Length <= 500 ? texto : texto[..500] + "…";

    public void Dispose() => _http.Dispose();
}
