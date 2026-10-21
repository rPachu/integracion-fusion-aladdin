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
        ? new EnvioFusion(opciones.Fusion, opciones.Declaracion, Ruta(ej.CarpetaSalida), log)
        : new EnvioSimulacion(Ruta(ej.CarpetaSalida));
    using var liberarEnvio = envio as IDisposable;

    var registrar = envio is not EnvioSimulacion || ej.RegistrarEnviadosEnSimulacion;
    var registro = new RegistroEnviados(Ruta(ej.CarpetaEstado));
    var builder = new DeclaracionBuilder(opciones.Declaracion);

    var motos = await origen.ObtenerAsync(ct);
    foreach (var moto in motos) moto.Normalizar();
    log.Info($"Motos leídas: {motos.Count} ({origen.Descripcion}).");

    if (!string.IsNullOrWhiteSpace(ej.FiltroOtFusion))
    {
        motos = motos.Where(m => EsIgual(m.OtFusion, ej.FiltroOtFusion.Trim())).ToList();
        log.Info($"Filtro de prueba: solo la OT de Fusion {ej.FiltroOtFusion} ({motos.Count} motos).");
    }

    int yaEnviadas = 0, incompletas = 0, duplicadas = 0, enviadas = 0, sinDisponible = 0, erroresEnvio = 0;
    var candidatas = new List<MotoDeclarada>();

    // Series repetidas en motos distintas (otro artículo): puede pasar si dos puestos declaran
    // al mismo tiempo y la función de numeración les da el mismo número. Ninguna se envía.
    var seriesRepetidas = motos
        .Where(m => m.NroSerie != "")
        .GroupBy(m => m.NroSerie, StringComparer.OrdinalIgnoreCase)
        .Where(g => g.Select(m => m.Clave).Distinct().Count() > 1)
        .Select(g => g.Key)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var moto in motos)
    {
        if (registro.YaEnviado(moto.Clave)) { yaEnviadas++; continue; }

        if (seriesRepetidas.Contains(moto.NroSerie))
        {
            duplicadas++;
            log.Error($"Serie '{moto.NroSerie}' (artículo {moto.CodArticulo}, OT CAPATAZ {moto.NroOt}): NO se envía. " +
                      "La misma serie aparece en más de una moto. Revisar en CAPATAZ y corregir.");
            continue;
        }

        var previa = registro.SerieEnviadaConOtraClave(moto.NroSerie, moto.Clave);
        if (previa is not null)
        {
            duplicadas++;
            log.Error($"Serie '{moto.NroSerie}' (artículo {moto.CodArticulo}, OT CAPATAZ {moto.NroOt}): NO se envía. " +
                      $"Esa serie ya se envió el {previa.FechaEnvio:dd/MM/yyyy HH:mm} con otro artículo ({previa.Clave}). Revisar en CAPATAZ.");
            continue;
        }

        var faltantes = Validar(moto);
        if (faltantes.Count > 0)
        {
            // No se manda: alguien tiene que completar el dato (en CAPATAZ o en Tango) y sale en la próxima corrida.
            incompletas++;
            log.Advertencia($"Serie '{moto.NroSerie}' (OT CAPATAZ {moto.NroOt}): no se envía. {string.Join(" · ", faltantes)}.");
            continue;
        }

        candidatas.Add(moto);
    }

    foreach (var grupo in candidatas.GroupBy(x => x.OtFusion))
    {
        InfoOrden orden;
        try
        {
            orden = await envio.ConsultarOrdenAsync(grupo.Key, ct);
            log.Info(orden.Detalle);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Sin poder consultar la orden no se declara nada de ella; se reintenta en la próxima corrida.
            erroresEnvio += grupo.Count();
            log.Error($"OT de Fusion {grupo.Key}: no se envían sus {grupo.Count()} motos. {ex.Message}");
            continue;
        }

        var disponible = opciones.Fusion.ControlarDisponible || envio is EnvioSimulacion ? orden.Disponible : null;
        if (!opciones.Fusion.ControlarDisponible && envio is not EnvioSimulacion)
            log.Advertencia("ControlarDisponible = false: se envía sin controlar ReadyQuantity (solo para pruebas).");
        var usadas = 0;

        foreach (var moto in grupo.OrderBy(x => x.FechaDeclaracion).ThenBy(x => x.HoraDeclaracion).ThenBy(x => x.NroSerie))
        {
            if (ej.MaximoEnvios > 0 && enviadas + erroresEnvio >= ej.MaximoEnvios)
            {
                log.Info($"Serie '{moto.NroSerie}': queda para la próxima corrida (MaximoEnvios = {ej.MaximoEnvios}).");
                continue;
            }

            // Regla de Newsan: no se puede declarar más de lo que figura como ReadyQuantity en la OT.
            if (disponible.HasValue && usadas >= disponible.Value)
            {
                sinDisponible++;
                log.Info($"Serie '{moto.NroSerie}': queda pendiente; la OT {grupo.Key} no tiene más cantidad disponible (ReadyQuantity = {disponible}).");
                continue;
            }

            var declaracion = builder.Armar(moto, secuenciaOperacion: orden.SecuenciaOperacion);
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
             $"series duplicadas {duplicadas} · enviadas {enviadas} · sin disponible {sinDisponible} · errores de envío {erroresEnvio}.");

    // Código 1 = hay algo que alguien tiene que revisar (errores de envío o series duplicadas).
    return erroresEnvio > 0 || duplicadas > 0 ? 1 : 0;
}

// Devuelve los problemas de la moto (vacío = se puede enviar).
List<string> Validar(MotoDeclarada moto)
{
    var op = opciones.Declaracion;
    var problemas = new List<string>();

    var faltan = new List<string>();
    if (moto.NroSerie == "") faltan.Add("número de serie");
    if (moto.Vin == "") faltan.Add("VIN");
    if (moto.NroMotor == "") faltan.Add("número de motor");
    if (moto.OtFusion == "") faltan.Add("OT de Fusion (Leyenda 1 de la OT)");
    if (faltan.Count > 0) problemas.Add("Falta: " + string.Join(", ", faltan));

    if (moto.NroSerie != "" && !ReglasMoto.SerieValida(moto.NroSerie, op.FormatoSerie))
        problemas.Add("Número de serie con formato inválido (se espera 073-NNNNNNNNN/AAAA)");

    if (moto.Vin != "")
    {
        var anoDeclaracion = ReglasMoto.AnoDeclaracion(moto.FechaDeclaracion);
        var anoModelo = ReglasMoto.AnoModeloDesdeVin(moto.Vin, anoDeclaracion, out var motivo);
        if (anoModelo is null)
            problemas.Add($"No se puede obtener el año modelo: {motivo}");
        else if (Math.Abs(anoModelo.Value - anoDeclaracion) > op.ToleranciaAnoModelo)
            problemas.Add($"Año modelo {anoModelo} (según el VIN) no coherente con la declaración de {anoDeclaracion}. Revisar el VIN");
    }

    return problemas;
}

static bool EsIgual(string? a, string b) => string.Equals(a?.Trim(), b, StringComparison.OrdinalIgnoreCase);
