using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IntegracionNewsan.Configuracion;
using IntegracionNewsan.Infra;
using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Capataz;

/// <summary>
/// Cliente de la API de CAPATAZ: inicia sesión y ejecuta la consulta de grilla "Integracion Fusion".
/// Solo LEE datos; no modifica nada en CAPATAZ.
/// </summary>
public sealed class CapatazClient : IDisposable
{
    private static readonly JsonSerializerOptions OpcionesJson = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly CapatazOpciones _opciones;
    private readonly Log _log;
    private string? _token;

    public CapatazClient(CapatazOpciones opciones, Log log)
    {
        _opciones = opciones;
        _log = log;
        _http = new HttpClient
        {
            BaseAddress = new Uri(opciones.UrlApi.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos)
        };
    }

    /// <summary>POST /Login. La API devuelve el token JWT como texto plano.</summary>
    public async Task LoginAsync(CancellationToken ct)
    {
        var cuerpo = new
        {
            loginName = _opciones.Usuario,
            password = _opciones.Password,
            esDeRed = false,
            tipo = "Working",
            userData = (object?)null
        };

        using var respuesta = await _http.PostAsJsonAsync("Login", cuerpo, ct);
        var texto = await respuesta.Content.ReadAsStringAsync(ct);

        if (!respuesta.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"No se pudo iniciar sesión en CAPATAZ (HTTP {(int)respuesta.StatusCode}). " +
                $"Revisar usuario y contraseña en appsettings.Local.json. Respuesta: {texto}");

        _token = texto.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(_token))
            throw new InvalidOperationException("CAPATAZ respondió el login sin token.");

        _log.Info("Sesión iniciada en la API de CAPATAZ.");
    }

    /// <summary>GET /ConsultasDeGrilla/Ejecutar con la consulta configurada (IdConsulta).</summary>
    public async Task<List<MotoDeclarada>> EjecutarConsultaAsync(CancellationToken ct)
    {
        if (_token is null) await LoginAsync(ct);

        var respuesta = await PedirConsultaAsync(ct);
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            // El token dura ~8 h. Si venció, se vuelve a iniciar sesión y se reintenta una vez.
            respuesta.Dispose();
            _log.Info("El token de CAPATAZ venció; se vuelve a iniciar sesión.");
            await LoginAsync(ct);
            respuesta = await PedirConsultaAsync(ct);
        }

        using (respuesta)
        {
            var texto = await respuesta.Content.ReadAsStringAsync(ct);
            if (!respuesta.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"La consulta {_opciones.IdConsulta} de CAPATAZ devolvió HTTP {(int)respuesta.StatusCode}. " +
                    "Si es 409, la consulta probablemente usa filtros con variables @@...@@ (no se pueden usar por API). " +
                    $"Respuesta: {texto}");

            return JsonSerializer.Deserialize<List<MotoDeclarada>>(texto, OpcionesJson) ?? [];
        }
    }

    private async Task<HttpResponseMessage> PedirConsultaAsync(CancellationToken ct)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, "ConsultasDeGrilla/Ejecutar");
        pedido.Headers.Add("idEmp", _opciones.IdEmpresa.ToString());
        pedido.Headers.Add("idConsulta", _opciones.IdConsulta.ToString());
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return await _http.SendAsync(pedido, ct);
    }

    public void Dispose() => _http.Dispose();
}
