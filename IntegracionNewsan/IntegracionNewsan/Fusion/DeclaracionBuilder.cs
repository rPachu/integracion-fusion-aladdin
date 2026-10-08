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
                    TransactionDate = FechaTransaccion(moto.FechaDeclaracion),
                    TransactionNote = op.TransactionNote,
                    TransactionQuantity = 1,
                    TransactionUnitOfMeasure = op.UnidadMedida,
                    FromDispatchState = op.FromDispatchState,
                    ToDispatchState = op.ToDispatchState,
                    ExternalSystemPackingUnit = string.IsNullOrWhiteSpace(op.ExternalSystemPackingUnit)
                        ? null
                        : op.ExternalSystemPackingUnit,
                    TransactionSerial =
                    [
                        new SerieTransaccion
                        {
                            // PENDIENTE (Newsan): confirmar que SerialNumber es el número de serie
                            // de Newsan (073-…/2026) y de dónde lo obtiene Aladdin.
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
    private static string FechaTransaccion(string fecha)
    {
        var valor = DateTime.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f)
            ? f
            : DateTime.Now;
        return valor.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture);
    }

    // PROVISORIO: el año modelo sale de la columna MODELO del PPL. Mientras no se defina dónde
    // queda en CAPATAZ, se toma del comentario de la serie si es un año de 4 dígitos;
    // si no, se usa el valor por defecto de la configuración.
    private string AnoModelo(MotoDeclarada moto) =>
        moto.Comentario.Length == 4 && moto.Comentario.All(char.IsDigit)
            ? moto.Comentario
            : op.AnoModeloPorDefecto;
}
