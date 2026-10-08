using System.Text.Json.Serialization;

namespace IntegracionNewsan.Modelos;

/// <summary>
/// Una fila de la consulta personalizada "Integracion Fusion" de CAPATAZ: una moto declarada.
/// Los nombres de las columnas son los del SQL de la consulta (ver docs/registro-pruebas.md).
/// </summary>
public sealed class MotoDeclarada
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("t_comp")] public string TipoOt { get; set; } = "";
    [JsonPropertyName("nro_ot")] public string NroOt { get; set; } = "";
    /// <summary>OT de Fusion: se carga en la Leyenda 1 de la OT de CAPATAZ.</summary>
    [JsonPropertyName("ot_fusion")] public string OtFusion { get; set; } = "";
    [JsonPropertyName("cod_articu")] public string CodArticulo { get; set; } = "";
    [JsonPropertyName("descripcio")] public string Descripcion { get; set; } = "";
    [JsonPropertyName("n_serie")] public string NroSerie { get; set; } = "";
    /// <summary>Descripción adicional 1 de la serie.</summary>
    [JsonPropertyName("vin")] public string Vin { get; set; } = "";
    /// <summary>Descripción adicional 2 de la serie.</summary>
    [JsonPropertyName("nro_motor")] public string NroMotor { get; set; } = "";
    [JsonPropertyName("comentario")] public string Comentario { get; set; } = "";
    [JsonPropertyName("n_partida")] public string NroPartida { get; set; } = "";
    [JsonPropertyName("cod_deposi")] public string CodDeposito { get; set; } = "";
    [JsonPropertyName("fecha_declaracion")] public string FechaDeclaracion { get; set; } = "";
    [JsonPropertyName("tipo_comprob")] public string TipoComprobante { get; set; } = "";
    [JsonPropertyName("nro_comprob")] public string NroComprobante { get; set; } = "";

    /// <summary>Clave única de la moto en CAPATAZ (artículo + serie). Se usa para no enviarla dos veces.</summary>
    [JsonIgnore]
    public string Clave => $"{CodArticulo}|{NroSerie}";

    /// <summary>
    /// La API devuelve algunos campos con espacios (ej. nro_ot = " 000000000075")
    /// y puede devolver null en columnas vacías: se recortan y se pasan a texto vacío.
    /// </summary>
    public void Normalizar()
    {
        TipoOt = Limpio(TipoOt);
        NroOt = Limpio(NroOt);
        OtFusion = Limpio(OtFusion);
        CodArticulo = Limpio(CodArticulo);
        Descripcion = Limpio(Descripcion);
        NroSerie = Limpio(NroSerie);
        Vin = Limpio(Vin);
        NroMotor = Limpio(NroMotor);
        Comentario = Limpio(Comentario);
        NroPartida = Limpio(NroPartida);
        CodDeposito = Limpio(CodDeposito);
        FechaDeclaracion = Limpio(FechaDeclaracion);
        TipoComprobante = Limpio(TipoComprobante);
        NroComprobante = Limpio(NroComprobante);
    }

    private static string Limpio(string? texto) => (texto ?? "").Trim();
}
