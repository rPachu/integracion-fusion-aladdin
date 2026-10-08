using IntegracionNewsan.Capataz;
using IntegracionNewsan.Configuracion;
using IntegracionNewsan.Envio;
using IntegracionNewsan.Fusion;
using IntegracionNewsan.Infra;
using IntegracionNewsan.Modelos;
using IntegracionNewsan.Origen;
using Microsoft.Extensions.Configuration;

// -----------------------------------------------------------------------------
// IntegracionNewsan
// Lee de CAPATAZ las motos declaradas (consulta personalizada "Integracion Fusion"),
// arma la declaración de producto terminado de cada una y la envía a Oracle Fusion
// (Newsan). Solo LEE de CAPATAZ. Ver README.md en la raíz del repositorio.
// -----------------------------------------------------------------------------

var baseDir = AppContext.BaseDirectory;

var configuracion = new ConfigurationBuilder()
    .SetBasePath(baseDir)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Local.json", optional: true)   // contraseñas: este archivo NO se sube al repo
    .Build();

var opciones = configuracion.Get<Opciones>() ?? new Opciones();

string Ruta(string ruta) => Path.IsPathRooted(ruta) ? ruta : Path.Combine(baseDir, ruta);

var log = new Log(Ruta(opciones.Ejecucion.CarpetaLogs));

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

int codigoSalida;
try
{
    codigoSalida = await CorrerAsync(cts.Token);
}
catch (Exception ex)
{
    log.Error("La corrida terminó con un error no controlado.", ex);
    codigoSalida = 1;
}

log.Info($"=== Fin de la corrida (código de salida {codigoSalida}) ===");
return codigoSalida;

// -----------------------------------------------------------------------------

async Task<int> CorrerAsync(CancellationToken ct)
{
    var ej = opciones.Ejecucion;
    log.Info($"=== Inicio de la corrida · Origen: {ej.Origen} · Modo de envío: {ej.ModoEnvio} ===");

    // Origen de las motos: la API de CAPATAZ o un archivo de prueba (para probar sin la API).
    CapatazClient? capataz = EsIgual(ej.Origen, "ArchivoPrueba") ? null : new CapatazClient(opciones.Capataz, log);
    using var liberarCapataz = capataz;
    IOrigenMotos origen = capataz is null
        ? new OrigenArchivo(Ruta(ej.ArchivoPrueba))
        : new OrigenApi(capataz);

    // Envío: simulación (guarda el JSON en una carpeta) o Fusion (pendiente de credenciales).
    IEnvioDeclaracion envio = EsIgual(ej.ModoEnvio, "Fusion")
        ? new EnvioFusion(opciones.Fusion)
        : new EnvioSimulacion(Ruta(ej.CarpetaSalida));

    var registrar = envio is not EnvioSimulacion || ej.RegistrarEnviadosEnSimulacion;
    var registro = new RegistroEnviados(Ruta(ej.CarpetaEstado));
    var builder = new DeclaracionBuilder(opciones.Declaracion);

    var motos = await origen.ObtenerAsync(ct);
    foreach (var moto in motos) moto.Normalizar();
    log.Info($"Motos leídas: {motos.Count} ({origen.Descripcion}).");

    int yaEnviadas = 0, incompletas = 0, enviadas = 0, sinDisponible = 0, erroresEnvio = 0;
    var candidatas = new List<MotoDeclarada>();

    foreach (var moto in motos)
    {
        if (registro.YaEnviado(moto.Clave)) { yaEnviadas++; continue; }

        var faltantes = Validar(moto);
        if (faltantes.Count > 0)
        {
            // No se manda: alguien tiene que completar el dato (en CAPATAZ o en Tango) y sale en la próxima corrida.
            incompletas++;
            log.Advertencia($"Serie '{moto.NroSerie}' (OT CAPATAZ {moto.NroOt}): no se envía. Falta: {string.Join(", ", faltantes)}.");
            continue;
        }

        candidatas.Add(moto);
    }

    foreach (var grupo in candidatas.GroupBy(x => x.OtFusion))
    {
        var disponible = await envio.ObtenerDisponibleAsync(grupo.Key, ct);
        var usadas = 0;

        foreach (var moto in grupo.OrderBy(x => x.FechaDeclaracion).ThenBy(x => x.HoraDeclaracion).ThenBy(x => x.NroSerie))
        {
            // Regla de Newsan: no se puede declarar más de lo que figura como ReadyQuantity en la OT.
            if (disponible.HasValue && usadas >= disponible.Value)
            {
                sinDisponible++;
                log.Info($"Serie '{moto.NroSerie}': queda pendiente; la OT {grupo.Key} no tiene más cantidad disponible (ReadyQuantity = {disponible}).");
                continue;
            }

            var declaracion = builder.Armar(moto);
            var resultado = await envio.EnviarAsync(moto, declaracion, ct);

            if (resultado.Ok)
            {
                enviadas++;
                usadas++;
                if (registrar) registro.Registrar(moto, resultado.Detalle);
                log.Info($"Serie '{moto.NroSerie}' (OT Fusion {moto.OtFusion}): enviada. {resultado.Detalle}");
            }
            else
            {
                erroresEnvio++;
                log.Error($"Serie '{moto.NroSerie}' (OT Fusion {moto.OtFusion}): falló el envío. {resultado.Detalle}");
            }
        }
    }

    log.Info($"Resumen: leídas {motos.Count} · ya enviadas {yaEnviadas} · incompletas {incompletas} · " +
             $"enviadas {enviadas} · sin disponible {sinDisponible} · errores de envío {erroresEnvio}.");

    return erroresEnvio > 0 ? 1 : 0;
}

static List<string> Validar(MotoDeclarada moto)
{
    var faltantes = new List<string>();
    if (string.IsNullOrWhiteSpace(moto.NroSerie)) faltantes.Add("número de serie");
    if (string.IsNullOrWhiteSpace(moto.Vin)) faltantes.Add("VIN");
    if (string.IsNullOrWhiteSpace(moto.NroMotor)) faltantes.Add("número de motor");
    if (string.IsNullOrWhiteSpace(moto.OtFusion)) faltantes.Add("OT de Fusion (Leyenda 1 de la OT)");
    return faltantes;
}

static bool EsIgual(string? a, string b) => string.Equals(a?.Trim(), b, StringComparison.OrdinalIgnoreCase);
