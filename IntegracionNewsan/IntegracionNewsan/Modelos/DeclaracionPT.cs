using System.Text.Json.Serialization;

namespace IntegracionNewsan.Modelos;

// Estructura del payload "Declaración de Producto Terminado (PEX)" que pasó Newsan
// (Oracle Fusion · Work Order Operation Transactions · POST /operationTransactions).

public sealed class DeclaracionPT
{
    public string SourceSystemCode { get; set; } = "";
    public string SourceSystemType { get; set; } = "";
    public List<DetalleOperacion> OperationTransactionDetail { get; set; } = [];
}

public sealed class DetalleOperacion
{
    public string SourceSystemCode { get; set; } = "";
    public string OrganizationCode { get; set; } = "";
    public string WorkOrderNumber { get; set; } = "";
    public int WoOperationSequenceNumber { get; set; }
    public string TransactionDate { get; set; } = "";
    public string TransactionNote { get; set; } = "";
    public decimal TransactionQuantity { get; set; }
    public string TransactionUnitOfMeasure { get; set; } = "";
    public string FromDispatchState { get; set; } = "";
    public string ToDispatchState { get; set; } = "";
    /// <summary>PENDIENTE (Newsan). Si es null no se incluye en el JSON.</summary>
    public string? ExternalSystemPackingUnit { get; set; }
    public List<SerieTransaccion> TransactionSerial { get; set; } = [];
}

public sealed class SerieTransaccion
{
    public string SerialNumber { get; set; } = "";
    public List<DffMotos> TransactionSerialDFF { get; set; } = [];
}

/// <summary>Flexfield "MOTOS" de la serie en Fusion.</summary>
public sealed class DffMotos
{
    [JsonPropertyName("__FLEX_Context")] public string FlexContext { get; set; } = "";
    [JsonPropertyName("vin")] public string Vin { get; set; } = "";
    [JsonPropertyName("numeroDeMotor")] public string NumeroDeMotor { get; set; } = "";
    [JsonPropertyName("anoModelo")] public string AnoModelo { get; set; } = "";
    [JsonPropertyName("numeroDnrpa")] public string NumeroDnrpa { get; set; } = "";
    [JsonPropertyName("bloqueadoDnrpa")] public string BloqueadoDnrpa { get; set; } = "";
}
