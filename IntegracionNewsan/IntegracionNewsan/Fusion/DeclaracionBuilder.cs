using System.Globalization;
using IntegracionNewsan.Configuracion;
using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Fusion;

/// <summary>
/// Arma la declaración de producto terminado de una moto, con la estructura del payload
/// "Declaración de Producto Terminado (PEX)" que pasó Newsan. Una moto por declaración.
/// </summary>
public sealed class DeclaracionBuilder(DeclaracionOpciones op)
{
    /// <param name="organizacion">Si se conoce por la consulta a la OT en Fusion, reemplaza al valor configurado.</param>
    /// <param name="secuenciaOperacion">Ídem para la secuencia de la operación.</param>
    public DeclaracionPT Armar(MotoDeclarada moto, string? organizacion = null, int? secuenciaOperacion = null)
    {
        return new DeclaracionPT
        {
            SourceSystemCode = op.SourceSystemCode,
            SourceSystemType = op.SourceSystemType,
            OperationTransactionDetail =
            [
                new DetalleOperacion
                {
                    SourceSystemCode = op.SourceSystemCode,
                    OrganizationCode = organizacion ?? op.OrganizationCode,
                    WorkOrderNumber = moto.OtFusion,
                    WoOperationSequenceNumber = secuenciaOperacion ?? op.WoOperationSequenceNumber,
                    TransactionDate = FechaTransaccion(moto.FechaDeclaracion, moto.HoraDeclaracion),
                    TransactionNote = op.TransactionNote,
                    TransactionQuantity = 1,
                    TransactionUnitOfMeasure = op.UnidadMedida,
                    FromDispatchState = op.FromDispatchState,
                    ToDispatchState = op.ToDispatchState,
                    // LPN: uno por moto, armado desde la serie (definido con Newsan).
                    ExternalSystemPackingUnit = ReglasMoto.Lpn(moto.NroSerie, op.PrefijoLpn),
                    TransactionSerial =
                    [
                        new SerieTransaccion
                        {
                            // Número de serie de Newsan (073-NNNNNNNNN/AAAA), generado en CAPATAZ (función NSAN).
                            SerialNumber = moto.NroSerie,
                            TransactionSerialDFF =
                            [
                                new DffMotos
                                {
                                    FlexContext = op.FlexContext,
                                    Vin = moto.Vin,
                                    NumeroDeMotor = moto.NroMotor,
                                    AnoModelo = AnoModelo(moto),
                                    NumeroDnrpa = "",
                                    BloqueadoDnrpa = op.BloqueadoDnrpa
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    // Formato del ejemplo de Newsan: "2024-10-10T15:20".
    // La fecha sale de fecha_mov y la hora de hora_comp del comprobante (texto "HHmmss", ej. "120256").
    // Si la hora no viene o no es válida, se informa 00:00.
    private static string FechaTransaccion(string fecha, string hora)
    {
        var dia = DateTime.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f)
            ? f.Date
            : DateTime.Today;

        var digitos = new string(hora.Where(char.IsDigit).ToArray());
        if (digitos.Length is 3 or 5) digitos = "0" + digitos; // por si la hora viene sin el cero inicial

        if (digitos.Length >= 4
            && int.TryParse(digitos[..2], out var hh)
            && int.TryParse(digitos[2..4], out var mm)
            && hh < 24 && mm < 60)
        {
            dia = dia.AddHours(hh).AddMinutes(mm);
        }

        return dia.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture);
    }

    // Año modelo desde el carácter 10 del VIN. La moto ya pasó la validación (Program.Validar),
    // así que acá siempre se puede calcular.
    private static string AnoModelo(MotoDeclarada moto) =>
        ReglasMoto.AnoModeloDesdeVin(moto.Vin, ReglasMoto.AnoDeclaracion(moto.FechaDeclaracion), out var motivo)
            ?.ToString(CultureInfo.InvariantCulture)
        ?? throw new InvalidOperationException($"No se pudo obtener el año modelo: {motivo}");
}
